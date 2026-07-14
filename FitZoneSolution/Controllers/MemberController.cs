using System.Security.Claims;
using FitZone.Data;
using FitZone.Models.Entities;
using FitZone.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitZone.Controllers;

[Authorize(Roles = "Member")]
public class MemberController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public MemberController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Member?> GetCurrentMemberAsync() =>
        await _db.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == CurrentUserId);

    private async Task ExpireSubscriptionsIfNeeded(int memberId)
    {
        var expiredSubs = await _db.Subscriptions
            .Where(s => s.MemberId == memberId && s.Status == SubscriptionStatus.Active && s.EndDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var sub in expiredSubs)
            sub.Status = SubscriptionStatus.Expired;

        if (expiredSubs.Any())
            await _db.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        await ExpireSubscriptionsIfNeeded(member.Id);

        var activeSub = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.MemberId == member.Id && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        var upcomingBookings = await _db.Bookings
            .Include(bk => bk.Class).ThenInclude(c => c.Trainer).ThenInclude(t => t.User)
            .Include(bk => bk.Class).ThenInclude(c => c.Schedules)
            .Where(bk => bk.MemberId == member.Id && bk.Status == BookingStatus.Confirmed)
            .ToListAsync();

        var assignment = await _db.TrainerMemberAssignments
            .Include(a => a.Trainer).ThenInclude(t => t.User)
            .Where(a => a.MemberId == member.Id)
            .FirstOrDefaultAsync();

        var vm = new MemberDashboardVM
        {
            MemberName = member.User.Name,
            ActivePlanName = activeSub?.Plan.PlanName,
            SubscriptionEndDate = activeSub?.EndDate,
            SubscriptionStatus = activeSub != null ? "Active" : "None",
            UpcomingBookingCount = upcomingBookings.Count,
            UpcomingClasses = upcomingBookings.SelectMany(bk => bk.Class.Schedules.Select(sc => new UpcomingClassVM
            {
                BookingId = bk.Id,
                ClassId = bk.ClassId,
                ClassName = bk.Class.ClassName,
                TrainerName = bk.Class.Trainer.User.Name,
                DayOfWeek = sc.DayOfWeek,
                StartTime = sc.StartTime,
                EndTime = sc.EndTime
            })).OrderBy(c => c.DayOfWeek).ToList(),
            RecentProgress = (await _db.ProgressRecords
                .Where(p => p.MemberId == member.Id)
                .OrderByDescending(p => p.Date)
                .Take(10)
                .ToListAsync())
                .Select(p => new ProgressPointVM { Date = p.Date, Weight = p.Weight, BodyFat = p.BodyFat, Notes = p.Notes })
                .ToList(),
            TrainerName = assignment?.Trainer?.User?.Name,
            TrainerSpecialization = assignment?.Trainer?.Specialization
        };

        return View(vm);
    }

    // GET /Member/Subscribe
    [HttpGet]
    public async Task<IActionResult> Subscribe()
    {
        var plans = await _db.SubscriptionPlans
            .Select(p => new PlanOptionVM { Id = p.Id, PlanName = p.PlanName, DurationMonths = p.DurationMonths, Price = p.Price })
            .ToListAsync();

        return View(new SubscribeVM { AvailablePlans = plans });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(int planId, PaymentMethod method)
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        var plan = await _db.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
        {
            TempData["Error"] = "Selected plan no longer exists.";
            return RedirectToAction(nameof(Subscribe));
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var subscription = new Subscription
        {
            MemberId = member.Id,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(plan.DurationMonths),
            Status = SubscriptionStatus.PendingPayment
        };
        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        var payment = new Payment
        {
            SubscriptionId = subscription.Id,
            Amount = plan.Price,
            Method = method,
            PaymentDate = DateTime.UtcNow
        };
        _db.Payments.Add(payment);

        // Simulated payment confirmation -> activate subscription.
        subscription.Status = SubscriptionStatus.Active;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        TempData["Success"] = $"Subscribed to {plan.PlanName} successfully.";
        return RedirectToAction(nameof(Dashboard));
    }

    // GET /Member/BookClass
    [HttpGet]
    public async Task<IActionResult> BookClass()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        await ExpireSubscriptionsIfNeeded(member.Id);

        var hasActiveSub = await _db.Subscriptions
            .AnyAsync(s => s.MemberId == member.Id && s.Status == SubscriptionStatus.Active && s.EndDate >= DateTime.UtcNow);

        var classes = await _db.Classes
            .Include(c => c.Trainer).ThenInclude(t => t.User)
            .Include(c => c.Schedules)
            .Include(c => c.Bookings)
            .ToListAsync();

        var vm = new BookClassVM
        {
            HasActiveSubscription = hasActiveSub,
            AvailableClasses = classes.Select(c => new ClassOptionVM
            {
                ClassId = c.Id,
                ClassName = c.ClassName,
                TrainerName = c.Trainer.User.Name,
                Capacity = c.Capacity,
                Duration = c.Duration,
                BookedCount = c.Bookings.Count(bk => bk.Status == BookingStatus.Confirmed),
                Schedule = c.Schedules.Select(s => (s.DayOfWeek, s.StartTime, s.EndTime)).ToList(),
                AlreadyBooked = c.Bookings.Any(bk => bk.MemberId == member.Id && bk.Status == BookingStatus.Confirmed),
                ActiveBookingId = c.Bookings.FirstOrDefault(bk => bk.MemberId == member.Id && bk.Status == BookingStatus.Confirmed)?.Id
            }).ToList()
        };

        return View(vm);
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookClass(int classId)
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        var hasActiveSub = await _db.Subscriptions
            .AnyAsync(s => s.MemberId == member.Id && s.Status == SubscriptionStatus.Active && s.EndDate >= DateTime.UtcNow);
        if (!hasActiveSub)
        {
            TempData["Error"] = "You need an active subscription to book a class.";
            return RedirectToAction(nameof(BookClass));
        }

        var gymClass = await _db.Classes.Include(c => c.Bookings).FirstOrDefaultAsync(c => c.Id == classId);
        if (gymClass == null)
        {
            TempData["Error"] = "Class not found.";
            return RedirectToAction(nameof(BookClass));
        }

        var confirmedCount = gymClass.Bookings.Count(bk => bk.Status == BookingStatus.Confirmed);
        if (confirmedCount >= gymClass.Capacity)
        {
            TempData["Error"] = "This class has reached maximum capacity.";
            return RedirectToAction(nameof(BookClass));
        }

        var alreadyBooked = gymClass.Bookings.Any(bk => bk.MemberId == member.Id && bk.Status == BookingStatus.Confirmed);
        if (alreadyBooked)
        {
            TempData["Error"] = "You have already booked this class.";
            return RedirectToAction(nameof(BookClass));
        }

        _db.Bookings.Add(new Booking
        {
            MemberId = member.Id,
            ClassId = classId,
            BookingDate = DateTime.UtcNow,
            Status = BookingStatus.Confirmed
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Booked {gymClass.ClassName} successfully.";
        return RedirectToAction(nameof(BookClass));
    }

    // POST /Member/CancelBooking
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int bookingId)
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.MemberId == member.Id);
        if (booking == null)
        {
            TempData["Error"] = "Booking not found.";
            return RedirectToAction(nameof(BookClass));
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            TempData["Error"] = "Only confirmed bookings can be cancelled.";
            return RedirectToAction(nameof(BookClass));
        }

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Booking cancelled successfully.";
        return RedirectToAction(nameof(BookClass));
    }

    // GET /Member/MyProgress
    [HttpGet]
    public async Task<IActionResult> MyProgress()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        var records = await _db.ProgressRecords
            .Where(p => p.MemberId == member.Id)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        var history = records.Select(p => new ProgressPointVM
        {
            Date = p.Date,
            Weight = p.Weight,
            BodyFat = p.BodyFat,
            Notes = p.Notes
        }).ToList();

        return View(new MyProgressVM { History = history });
    }

    // POST /Member/AddProgress
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProgress(MyProgressVM model)
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        var entry = model.NewEntry;
        if (!ModelState.IsValid)
        {
            var records = await _db.ProgressRecords
                .Where(p => p.MemberId == member.Id)
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            var history = records.Select(p => new ProgressPointVM
            {
                Date = p.Date,
                Weight = p.Weight,
                BodyFat = p.BodyFat,
                Notes = p.Notes
            }).ToList();

            model.History = history;
            return View("MyProgress", model);
        }

        _db.ProgressRecords.Add(new ProgressTracking
        {
            MemberId = member.Id,
            Weight = entry!.Weight,
            BodyFat = entry.BodyFat,
            Notes = entry.Notes,
            Date = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyProgress));
    }

    // GET /Member/MyWorkoutPlans
    [HttpGet]
    public async Task<IActionResult> MyWorkoutPlans()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        var plans = await _db.WorkoutPlans
            .Include(wp => wp.Trainer).ThenInclude(t => t.User)
            .Where(wp => wp.MemberId == member.Id)
            .OrderByDescending(wp => wp.AssignedDate)
            .Select(wp => new WorkoutPlanVM
            {
                Id = wp.Id,
                TrainerName = wp.Trainer.User.Name,
                PlanDetails = wp.PlanDetails,
                AssignedDate = wp.AssignedDate
            })
            .ToListAsync();

        return View(new MyWorkoutPlansVM { Plans = plans });
    }

    // GET /Member/MySubscription
    [HttpGet]
    public async Task<IActionResult> MySubscription()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        var activeSub = await _db.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Payments)
            .Where(s => s.MemberId == member.Id && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        MySubscriptionVM? vm = null;
        if (activeSub != null)
        {
            var daysRemaining = (activeSub.EndDate - DateTime.UtcNow).Days;
            vm = new MySubscriptionVM
            {
                PlanName = activeSub.Plan.PlanName,
                DurationMonths = activeSub.Plan.DurationMonths,
                Price = activeSub.Plan.Price,
                StartDate = activeSub.StartDate,
                EndDate = activeSub.EndDate,
                Status = "Active",
                DaysRemaining = daysRemaining > 0 ? daysRemaining : 0,
                Payments = activeSub.Payments.Select(p => new PaymentHistoryVM
                {
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Method = p.Method.ToString()
                }).ToList()
            };
        }

        var activeSubId = activeSub?.Id ?? 0;
        var historyEntities = await _db.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Payments)
            .Where(s => s.MemberId == member.Id && s.Id != activeSubId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        var history = historyEntities.Select(s => new PastSubscriptionVM
        {
            PlanName = s.Plan.PlanName,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status.ToString(),
            AmountPaid = s.Payments.Sum(p => p.Amount)
        }).ToList();

        ViewBag.ActiveSubscription = vm;
        return View(new SubscriptionHistoryVM { Subscriptions = history });
    }

    // GET /Member/EditProfile
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        return View(new EditProfileVM
        {
            Name = member.User.Name,
            Email = member.User.Email,
            Phone = member.Phone
        });
    }

    // POST /Member/EditProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileVM model)
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        if (!ModelState.IsValid) return View(model);

        if (model.Email != member.User.Email)
        {
            var emailTaken = await _db.Users.AnyAsync(u => u.Email == model.Email && u.Id != member.UserId);
            if (emailTaken)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }
        }

        var nameChanged = member.User.Name != model.Name;

        member.User.Name = model.Name;
        member.User.Email = model.Email;
        member.Phone = model.Phone;

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
            member.User.PasswordHash = _hasher.HashPassword(member.User, model.NewPassword);

        await _db.SaveChangesAsync();

        if (nameChanged)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, member.UserId.ToString()),
                new(ClaimTypes.Name, member.User.Name),
                new(ClaimTypes.Email, member.User.Email),
                new(ClaimTypes.Role, "Member")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Dashboard));
    }

    // GET /Member/BookingHistory
    [HttpGet]
    public async Task<IActionResult> BookingHistory()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null) return NotFound();

        var bookings = await _db.Bookings
            .Include(bk => bk.Class).ThenInclude(c => c.Trainer).ThenInclude(t => t.User)
            .Include(bk => bk.Class).ThenInclude(c => c.Schedules)
            .Where(bk => bk.MemberId == member.Id)
            .OrderByDescending(bk => bk.BookingDate)
            .ToListAsync();

        var vm = new BookingHistoryVM
        {
            Bookings = bookings.Select(bk => new BookingHistoryItemVM
            {
                ClassName = bk.Class.ClassName,
                TrainerName = bk.Class.Trainer.User.Name,
                BookingDate = bk.BookingDate,
                Status = bk.Status.ToString(),
                Schedule = bk.Class.Schedules.Select(s => $"{s.DayOfWeek} {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}").ToList()
            }).ToList()
        };

        return View(vm);
    }
}
