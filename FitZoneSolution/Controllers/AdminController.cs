using FitZone.Data;
using FitZone.Models.Entities;
using FitZone.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitZone.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private const int PageSize = 10;

    public AdminController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // ================= DASHBOARD =================

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var totalMembers = await _db.Members.CountAsync();
        var totalTrainers = await _db.Trainers.CountAsync();
        var activeSubs = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active);
        var totalRevenue = (await _db.Payments.Select(p => p.Amount).ToListAsync()).Sum();
        var classesCount = await _db.Classes.CountAsync();
        var now = DateTime.UtcNow;
        var bookingsThisMonth = await _db.Bookings.CountAsync(b => b.BookingDate.Month == now.Month && b.BookingDate.Year == now.Year);

        return View(new AdminDashboardVM
        {
            TotalMembers = totalMembers,
            TotalTrainers = totalTrainers,
            ActiveSubscriptions = activeSubs,
            TotalRevenue = totalRevenue,
            ClassesCount = classesCount,
            BookingsThisMonth = bookingsThisMonth
        });
    }

    //  MEMBERS

    [HttpGet]
    public async Task<IActionResult> ManageMembers(string? search, int page = 1)
    {
        var query = _db.Members.Include(m => m.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(m => m.User.Name.ToLower().Contains(search) || m.User.Email.ToLower().Contains(search));
        }

        var paged = await PaginatedList<Member>.CreateAsync(query, page, PageSize);
        var memberIds = paged.Items.Select(m => m.Id).ToList();
        var activeSubs = await _db.Subscriptions.Where(s => memberIds.Contains(s.MemberId) && s.Status == SubscriptionStatus.Active).ToListAsync();

        var vm = paged.Items.Select(m => new MemberListItemVM
        {
            MemberId = m.Id,
            UserId = m.UserId,
            Name = m.User.Name,
            Email = m.User.Email,
            Phone = m.Phone,
            MembershipDate = m.MembershipDate,
            SubscriptionStatus = activeSubs.Any(s => s.MemberId == m.Id) ? "Active" : "Inactive"
        }).ToList();

        ViewBag.Search = search;
        ViewBag.PageIndex = paged.PageIndex;
        ViewBag.TotalPages = paged.TotalPages;
        ViewBag.TotalCount = paged.TotalCount;
        ViewBag.HasPrevious = paged.HasPreviousPage;
        ViewBag.HasNext = paged.HasNextPage;
        ViewBag.Action = nameof(ManageMembers);

        return View(vm);
    }

    [HttpGet]
    public IActionResult CreateMember() => View("MemberForm", new MemberFormVM());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMember(MemberFormVM model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Password is required for new members.");

        if (!ModelState.IsValid) return View("MemberForm", model);

        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Email already registered.");
            return View("MemberForm", model);
        }

        var user = new User { Name = model.Name, Email = model.Email, Role = UserRole.Member };
        user.PasswordHash = _hasher.HashPassword(user, model.Password!);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.Members.Add(new Member { UserId = user.Id, Phone = model.Phone, MembershipDate = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Member created.";
        return RedirectToAction(nameof(ManageMembers));
    }

    [HttpGet]
    public async Task<IActionResult> EditMember(int id)
    {
        var member = await _db.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return NotFound();

        return View("MemberForm", new MemberFormVM
        {
            MemberId = member.Id,
            UserId = member.UserId,
            Name = member.User.Name,
            Email = member.User.Email,
            Phone = member.Phone
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMember(int id, MemberFormVM model)
    {
        var member = await _db.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return NotFound();

        if (!ModelState.IsValid) return View("MemberForm", model);

        member.User.Name = model.Name;
        member.User.Email = model.Email;
        member.Phone = model.Phone;
        if (!string.IsNullOrWhiteSpace(model.Password))
            member.User.PasswordHash = _hasher.HashPassword(member.User, model.Password);

        await _db.SaveChangesAsync();
        TempData["Success"] = "Member updated.";
        return RedirectToAction(nameof(ManageMembers));
    }

    // Business rule: only Admin can delete; soft-delete via IsDeleted flag.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMember(int id)
    {
        var member = await _db.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return NotFound();

        member.IsDeleted = true;
        member.User.IsDeleted = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Member removed.";
        return RedirectToAction(nameof(ManageMembers));
    }

    // ================= TRAINERS =================

    [HttpGet]
    public async Task<IActionResult> ManageTrainers(string? search, int page = 1)
    {
        var query = _db.Trainers.Include(t => t.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(t => t.User.Name.ToLower().Contains(search) || t.User.Email.ToLower().Contains(search));
        }

        var paged = await PaginatedList<Trainer>.CreateAsync(query, page, PageSize);
        var trainerIds = paged.Items.Select(t => t.Id).ToList();
        var assignmentCounts = await _db.TrainerMemberAssignments
            .Where(a => trainerIds.Contains(a.TrainerId))
            .GroupBy(a => a.TrainerId)
            .Select(g => new { TrainerId = g.Key, Count = g.Count() })
            .ToListAsync();

        var vm = paged.Items.Select(t => new TrainerListItemVM
        {
            TrainerId = t.Id,
            UserId = t.UserId,
            Name = t.User.Name,
            Email = t.User.Email,
            Specialization = t.Specialization,
            HireDate = t.HireDate,
            AssignedMemberCount = assignmentCounts.FirstOrDefault(a => a.TrainerId == t.Id)?.Count ?? 0
        }).ToList();

        ViewBag.Search = search;
        ViewBag.PageIndex = paged.PageIndex;
        ViewBag.TotalPages = paged.TotalPages;
        ViewBag.TotalCount = paged.TotalCount;
        ViewBag.HasPrevious = paged.HasPreviousPage;
        ViewBag.HasNext = paged.HasNextPage;
        ViewBag.Action = nameof(ManageTrainers);

        return View(vm);
    }

    [HttpGet]
    public IActionResult CreateTrainer() => View("TrainerForm", new TrainerFormVM());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTrainer(TrainerFormVM model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Password is required for new trainers.");

        if (!ModelState.IsValid) return View("TrainerForm", model);

        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Email already registered.");
            return View("TrainerForm", model);
        }

        var user = new User { Name = model.Name, Email = model.Email, Role = UserRole.Trainer };
        user.PasswordHash = _hasher.HashPassword(user, model.Password!);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.Trainers.Add(new Trainer { UserId = user.Id, Specialization = model.Specialization, HireDate = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Trainer created.";
        return RedirectToAction(nameof(ManageTrainers));
    }

    [HttpGet]
    public async Task<IActionResult> EditTrainer(int id)
    {
        var trainer = await _db.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
        if (trainer == null) return NotFound();

        return View("TrainerForm", new TrainerFormVM
        {
            TrainerId = trainer.Id,
            UserId = trainer.UserId,
            Name = trainer.User.Name,
            Email = trainer.User.Email,
            Specialization = trainer.Specialization
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTrainer(int id, TrainerFormVM model)
    {
        var trainer = await _db.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
        if (trainer == null) return NotFound();

        if (!ModelState.IsValid) return View("TrainerForm", model);

        trainer.User.Name = model.Name;
        trainer.User.Email = model.Email;
        trainer.Specialization = model.Specialization;
        if (!string.IsNullOrWhiteSpace(model.Password))
            trainer.User.PasswordHash = _hasher.HashPassword(trainer.User, model.Password);

        await _db.SaveChangesAsync();
        TempData["Success"] = "Trainer updated.";
        return RedirectToAction(nameof(ManageTrainers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTrainer(int id)
    {
        var trainer = await _db.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
        if (trainer == null) return NotFound();

        trainer.IsDeleted = true;
        trainer.User.IsDeleted = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Trainer removed.";
        return RedirectToAction(nameof(ManageTrainers));
    }

    // Assign members to a trainer (supports the "trainer sees only assigned members" rule).
    [HttpGet]
    public async Task<IActionResult> AssignMembers(int trainerId)
    {
        var trainer = await _db.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == trainerId);
        if (trainer == null) return NotFound();

        var allMembers = await _db.Members.Include(m => m.User).ToListAsync();
        var currentlyAssigned = await _db.TrainerMemberAssignments
            .Where(a => a.TrainerId == trainerId)
            .Select(a => a.MemberId)
            .ToListAsync();

        var vm = new AssignMemberVM
        {
            TrainerId = trainerId,
            TrainerName = trainer.User.Name,
            SelectedMemberIds = currentlyAssigned,
            AllMembers = allMembers.Select(m => new MemberListItemVM
            {
                MemberId = m.Id,
                Name = m.User.Name,
                Email = m.User.Email
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMembers(int trainerId, List<int> selectedMemberIds)
    {
        var existing = _db.TrainerMemberAssignments.Where(a => a.TrainerId == trainerId);
        _db.TrainerMemberAssignments.RemoveRange(existing);

        foreach (var memberId in selectedMemberIds.Distinct())
        {
            _db.TrainerMemberAssignments.Add(new TrainerMemberAssignment
            {
                TrainerId = trainerId,
                MemberId = memberId,
                AssignedDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Assignments updated.";
        return RedirectToAction(nameof(ManageTrainers));
    }

    // ================= SUBSCRIPTIONS / PLANS =================

    [HttpGet]
    public async Task<IActionResult> ManageSubscriptions(string? search, int page = 1)
    {
        var query = _db.Subscriptions
            .Include(s => s.Member)
                .ThenInclude(m => m.User)
            .Include(s => s.Plan)
            .Include(s => s.Payments)
            .OrderByDescending(s => s.StartDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(s => s.Member.User.Name.ToLower().Contains(search)
                                  || s.Plan.PlanName.ToLower().Contains(search));
        }

        var paged = await PaginatedList<Subscription>.CreateAsync(query, page, PageSize);

        var subs = paged.Items.Select(s => new SubscriptionAdminVM
        {
            SubscriptionId = s.Id,
            MemberName = s.Member.User.Name,
            PlanName = s.Plan.PlanName,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status.ToString(),
            AmountPaid = s.Payments.Sum(p => p.Amount)
        }).ToList();

        ViewBag.Search = search;
        ViewBag.PageIndex = paged.PageIndex;
        ViewBag.TotalPages = paged.TotalPages;
        ViewBag.TotalCount = paged.TotalCount;
        ViewBag.HasPrevious = paged.HasPreviousPage;
        ViewBag.HasNext = paged.HasNextPage;
        ViewBag.Action = nameof(ManageSubscriptions);

        return View(subs);
    }

    // POST /Admin/CancelSubscription
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSubscription(int subscriptionId)
    {
        var sub = await _db.Subscriptions.FindAsync(subscriptionId);
        if (sub == null) return NotFound();

        if (sub.Status != SubscriptionStatus.Active && sub.Status != SubscriptionStatus.PendingPayment)
        {
            TempData["Error"] = "Only active or pending subscriptions can be cancelled.";
            return RedirectToAction(nameof(ManageSubscriptions));
        }

        sub.Status = SubscriptionStatus.Cancelled;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Subscription cancelled.";
        return RedirectToAction(nameof(ManageSubscriptions));
    }

    // POST /Admin/ConfirmSubscription
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmSubscription(int subscriptionId)
    {
        var sub = await _db.Subscriptions.FindAsync(subscriptionId);
        if (sub == null) return NotFound();

        if (sub.Status != SubscriptionStatus.PendingPayment)
        {
            TempData["Error"] = "Only pending subscriptions can be confirmed.";
            return RedirectToAction(nameof(ManageSubscriptions));
        }

        sub.Status = SubscriptionStatus.Active;
        sub.StartDate = DateTime.UtcNow;
        var plan = await _db.SubscriptionPlans.FindAsync(sub.PlanId);
        if (plan != null)
            sub.EndDate = DateTime.UtcNow.AddMonths(plan.DurationMonths);

        await _db.SaveChangesAsync();

        TempData["Success"] = "Subscription confirmed and activated.";
        return RedirectToAction(nameof(ManageSubscriptions));
    }

    // POST /Admin/ExtendSubscription
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtendSubscription(int subscriptionId, int months)
    {
        var sub = await _db.Subscriptions.FindAsync(subscriptionId);
        if (sub == null) return NotFound();

        if (sub.Status != SubscriptionStatus.Active)
        {
            TempData["Error"] = "Only active subscriptions can be extended.";
            return RedirectToAction(nameof(ManageSubscriptions));
        }

        if (months < 1 || months > 12)
        {
            TempData["Error"] = "Extension must be between 1 and 12 months.";
            return RedirectToAction(nameof(ManageSubscriptions));
        }

        sub.EndDate = sub.EndDate.AddMonths(months);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Subscription extended by {months} month(s).";
        return RedirectToAction(nameof(ManageSubscriptions));
    }

    [HttpGet]
    public async Task<IActionResult> ManagePlans()
    {
        var plans = await _db.SubscriptionPlans.ToListAsync();
        return View(plans);
    }

    [HttpGet]
    public IActionResult CreatePlan() => View("PlanForm", new PlanFormVM());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan(PlanFormVM model)
    {
        if (!ModelState.IsValid) return View("PlanForm", model);

        _db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            PlanName = model.PlanName,
            DurationMonths = model.DurationMonths,
            Price = model.Price
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Plan created.";
        return RedirectToAction(nameof(ManagePlans));
    }

    [HttpGet]
    public async Task<IActionResult> EditPlan(int id)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(id);
        if (plan == null) return NotFound();

        return View("PlanForm", new PlanFormVM
        {
            Id = plan.Id,
            PlanName = plan.PlanName,
            DurationMonths = plan.DurationMonths,
            Price = plan.Price
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPlan(int id, PlanFormVM model)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(id);
        if (plan == null) return NotFound();

        if (!ModelState.IsValid) return View("PlanForm", model);

        plan.PlanName = model.PlanName;
        plan.DurationMonths = model.DurationMonths;
        plan.Price = model.Price;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Plan updated.";
        return RedirectToAction(nameof(ManagePlans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlan(int id)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(id);
        if (plan == null) return NotFound();

        plan.IsDeleted = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Plan removed.";
        return RedirectToAction(nameof(ManagePlans));
    }

    // ================= CLASSES =================

    [HttpGet]
    public async Task<IActionResult> ManageClasses(string? search, int page = 1)
    {
        var query = _db.Classes.Include(c => c.Trainer).ThenInclude(t => t.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(c => c.ClassName.ToLower().Contains(search)
                                  || c.Trainer.User.Name.ToLower().Contains(search));
        }

        var paged = await PaginatedList<GymClass>.CreateAsync(query, page, PageSize);

        ViewBag.Search = search;
        ViewBag.PageIndex = paged.PageIndex;
        ViewBag.TotalPages = paged.TotalPages;
        ViewBag.TotalCount = paged.TotalCount;
        ViewBag.HasPrevious = paged.HasPreviousPage;
        ViewBag.HasNext = paged.HasNextPage;
        ViewBag.Action = nameof(ManageClasses);

        return View(paged.Items);
    }

    [HttpGet]
    public async Task<IActionResult> CreateClass()
    {
        var vm = new ClassFormVM { AvailableTrainers = await GetTrainerOptionsAsync() };
        return View("ClassForm", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClass(ClassFormVM model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableTrainers = await GetTrainerOptionsAsync();
            return View("ClassForm", model);
        }

        _db.Classes.Add(new GymClass
        {
            ClassName = model.ClassName,
            TrainerId = model.TrainerId,
            Capacity = model.Capacity,
            Duration = model.Duration
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Class created.";
        return RedirectToAction(nameof(ManageClasses));
    }

    [HttpGet]
    public async Task<IActionResult> EditClass(int id)
    {
        var gymClass = await _db.Classes.FindAsync(id);
        if (gymClass == null) return NotFound();

        var vm = new ClassFormVM
        {
            Id = gymClass.Id,
            ClassName = gymClass.ClassName,
            TrainerId = gymClass.TrainerId,
            Capacity = gymClass.Capacity,
            Duration = gymClass.Duration,
            AvailableTrainers = await GetTrainerOptionsAsync()
        };
        return View("ClassForm", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClass(int id, ClassFormVM model)
    {
        var gymClass = await _db.Classes.FindAsync(id);
        if (gymClass == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.AvailableTrainers = await GetTrainerOptionsAsync();
            return View("ClassForm", model);
        }

        gymClass.ClassName = model.ClassName;
        gymClass.TrainerId = model.TrainerId;
        gymClass.Capacity = model.Capacity;
        gymClass.Duration = model.Duration;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Class updated.";
        return RedirectToAction(nameof(ManageClasses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClass(int id)
    {
        var gymClass = await _db.Classes.FindAsync(id);
        if (gymClass == null) return NotFound();

        gymClass.IsDeleted = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Class removed.";
        return RedirectToAction(nameof(ManageClasses));
    }

    private async Task<List<TrainerOptionVM>> GetTrainerOptionsAsync() =>
        await _db.Trainers.Include(t => t.User)
            .Select(t => new TrainerOptionVM { Id = t.Id, Name = t.User.Name })
            .ToListAsync();

    // ================= WEEKLY CLASS SCHEDULE (CALENDAR) =================

    // GET /Admin/Schedule
    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var slots = (await _db.ClassSchedules
            .Include(s => s.Class)
                .ThenInclude(c => c.Trainer)
                    .ThenInclude(t => t.User)
            .Include(s => s.Class)
                .ThenInclude(c => c.Bookings)
            .ToListAsync())
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToList();

        var classes = await _db.Classes
            .Select(c => new ClassOptionForScheduleVM
            {
                Id = c.Id,
                ClassName = c.ClassName
            })
            .ToListAsync();

        var vm = new WeeklyScheduleVM
        {
            AllClasses = classes,
            Slots = slots.Select(s => new ScheduleSlotVM
            {
                ScheduleId = s.Id,
                ClassId = s.ClassId,
                ClassName = s.Class.ClassName,
                TrainerName = s.Class.Trainer.User.Name,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Capacity = s.Class.Capacity,
                BookedCount = s.Class.Bookings.Count(b => b.Status == BookingStatus.Confirmed)
            }).ToList()
        };

        return View(vm);
    }
    

    // POST /Admin/AddScheduleSlot
    // Business rule: prevent overlapping schedule slots for the same class on the same day.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddScheduleSlot(AddScheduleSlotVM model)
    {
        if (model.EndTime <= model.StartTime)
        {
            TempData["Error"] = "End time must be after start time.";
            return RedirectToAction(nameof(Schedule));
        }

        var overlap = await _db.ClassSchedules.AnyAsync(s =>
            s.ClassId == model.ClassId &&
            s.DayOfWeek == model.DayOfWeek &&
            s.StartTime < model.EndTime &&
            model.StartTime < s.EndTime);

        if (overlap)
        {
            TempData["Error"] = "This class already has an overlapping slot on that day.";
            return RedirectToAction(nameof(Schedule));
        }

        _db.ClassSchedules.Add(new ClassSchedule
        {
            ClassId = model.ClassId,
            DayOfWeek = model.DayOfWeek,
            StartTime = model.StartTime,
            EndTime = model.EndTime
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Schedule slot added.";
        return RedirectToAction(nameof(Schedule));
    }

    // POST /Admin/DeleteScheduleSlot
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteScheduleSlot(int scheduleId)
    {
        var slot = await _db.ClassSchedules.FindAsync(scheduleId);
        if (slot == null) return NotFound();

        _db.ClassSchedules.Remove(slot);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Schedule slot removed.";
        return RedirectToAction(nameof(Schedule));
    }

    // ================= REPORTS =================

    // GET /Admin/Reports?from=...&to=...
    [HttpGet]
    public async Task<IActionResult> Reports(DateTime? from, DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-6);
        var toDate = to ?? DateTime.UtcNow;

        var payments = await _db.Payments
            .Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
            .ToListAsync();

        var activeSubs = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active);

        var newMembers = await _db.Members.CountAsync(m => m.MembershipDate >= fromDate && m.MembershipDate <= toDate);

        var popularClasses = await _db.Bookings
            .Include(bk => bk.Class)
            .Where(bk => bk.BookingDate >= fromDate && bk.BookingDate <= toDate)
            .GroupBy(bk => bk.Class.ClassName)
            .Select(g => new PopularClassVM { ClassName = g.Key, BookingCount = g.Count() })
            .OrderByDescending(g => g.BookingCount)
            .Take(5)
            .ToListAsync();

        var revenueByMonth = payments
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyRevenueVM
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(p => p.Amount)
            }).ToList();

        var vm = new ReportsVM
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = payments.Sum(p => p.Amount),
            ActiveSubscriptionCount = activeSubs,
            NewMembersCount = newMembers,
            MostPopularClasses = popularClasses,
            RevenueByMonth = revenueByMonth
        };

        return View(vm);
    }
}
