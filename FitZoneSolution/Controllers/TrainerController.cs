using System.Security.Claims;
using FitZone.Data;
using FitZone.Models.Entities;
using FitZone.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitZone.Controllers;

[Authorize(Roles = "Trainer")]
public class TrainerController : Controller
{
    private readonly AppDbContext _db;

    public TrainerController(AppDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Trainer?> GetCurrentTrainerAsync() =>
        await _db.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.UserId == CurrentUserId);

    // GET /Trainer/Dashboard
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var trainer = await GetCurrentTrainerAsync();
        if (trainer == null) return NotFound();

        var assignedCount = await _db.TrainerMemberAssignments.CountAsync(a => a.TrainerId == trainer.Id);
        var classCount = await _db.Classes.CountAsync(c => c.TrainerId == trainer.Id);

        var upcoming = await _db.Classes
            .Include(c => c.Schedules)
            .Where(c => c.TrainerId == trainer.Id)
            .ToListAsync();

        var vm = new TrainerDashboardVM
        {
            TrainerName = trainer.User.Name,
            Specialization = trainer.Specialization,
            AssignedMemberCount = assignedCount,
            ClassCount = classCount,
            UpcomingClasses = upcoming.SelectMany(c => c.Schedules.Select(s => new UpcomingClassVM
            {
                ClassId = c.Id,
                ClassName = c.ClassName,
                TrainerName = trainer.User.Name,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })).OrderBy(c => c.DayOfWeek).ToList()
        };

        return View(vm);
    }

    // GET /Trainer/MyMembers
    // Business rule: trainer can only view members assigned by Admin.
    [HttpGet]
    public async Task<IActionResult> MyMembers()
    {
        var trainer = await GetCurrentTrainerAsync();
        if (trainer == null) return NotFound();

        var assignments = await _db.TrainerMemberAssignments
            .Include(a => a.Member).ThenInclude(m => m.User)
            .Where(a => a.TrainerId == trainer.Id)
            .ToListAsync();

        var memberIds = assignments.Select(a => a.MemberId).ToList();
        var activeSubs = await _db.Subscriptions
            .Where(s => memberIds.Contains(s.MemberId) && s.Status == SubscriptionStatus.Active)
            .ToListAsync();

        var vm = assignments.Select(a => new AssignedMemberVM
        {
            MemberId = a.MemberId,
            MemberName = a.Member.User.Name,
            Phone = a.Member.Phone,
            SubscriptionStatus = activeSubs.Any(s => s.MemberId == a.MemberId) ? "Active" : "Inactive"
        }).ToList();

        return View(vm);
    }

    // GET /Trainer/CreateWorkout/{memberId}
    [HttpGet]
    public async Task<IActionResult> CreateWorkout(int memberId)
    {
        var trainer = await GetCurrentTrainerAsync();
        if (trainer == null) return NotFound();

        var isAssigned = await _db.TrainerMemberAssignments
            .AnyAsync(a => a.TrainerId == trainer.Id && a.MemberId == memberId);
        if (!isAssigned) return Forbid();

        var member = await _db.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return NotFound();

        return View(new CreateWorkoutVM { MemberId = member.Id, MemberName = member.User.Name });
    }

    // POST /Trainer/CreateWorkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWorkout(CreateWorkoutVM model)
    {
        var trainer = await GetCurrentTrainerAsync();
        if (trainer == null) return NotFound();

        var isAssigned = await _db.TrainerMemberAssignments
            .AnyAsync(a => a.TrainerId == trainer.Id && a.MemberId == model.MemberId);
        if (!isAssigned) return Forbid();

        if (!ModelState.IsValid) return View(model);

        _db.WorkoutPlans.Add(new WorkoutPlan
        {
            TrainerId = trainer.Id,
            MemberId = model.MemberId,
            PlanDetails = model.PlanDetails,
            AssignedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Workout plan saved and visible on the member's portal.";
        return RedirectToAction(nameof(MyMembers));
    }

    // GET /Trainer/Attendance/{classId}
    [HttpGet]
    public async Task<IActionResult> Attendance(int classId)
    {
        var trainer = await GetCurrentTrainerAsync();
        if (trainer == null) return NotFound();

        var gymClass = await _db.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.TrainerId == trainer.Id);
        if (gymClass == null) return Forbid();

        var bookings = await _db.Bookings
            .Include(bk => bk.Member).ThenInclude(m => m.User)
            .Where(bk => bk.ClassId == classId)
            .ToListAsync();

        var vm = new AttendanceVM
        {
            ClassId = classId,
            ClassName = gymClass.ClassName,
            Rows = bookings.Select(bk => new AttendanceRowVM
            {
                BookingId = bk.Id,
                MemberName = bk.Member.User.Name,
                Status = bk.Status.ToString()
            }).ToList()
        };

        return View(vm);
    }

    // POST /Trainer/Attendance -> mark a booking Attended / NoShow
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAttendance(int bookingId, BookingStatus status, int classId)
    {
        var trainer = await GetCurrentTrainerAsync();
        if (trainer == null) return NotFound();

        if (status != BookingStatus.Attended && status != BookingStatus.NoShow)
        {
            TempData["Error"] = "Invalid attendance status.";
            return RedirectToAction(nameof(Attendance), new { classId });
        }

        var booking = await _db.Bookings.Include(bk => bk.Class)
            .FirstOrDefaultAsync(bk => bk.Id == bookingId && bk.Class.TrainerId == trainer.Id);
        if (booking == null) return Forbid();

        if (booking.Status != BookingStatus.Confirmed)
        {
            TempData["Error"] = "Only confirmed bookings can be marked.";
            return RedirectToAction(nameof(Attendance), new { classId });
        }

        booking.Status = status;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Attendance), new { classId });
    }

    // GET /Trainer/ViewMemberProgress/{memberId}
    [HttpGet]
    public async Task<IActionResult> ViewMemberProgress(int memberId)
    {
        var trainer = await GetCurrentTrainerAsync();
        if (trainer == null) return NotFound();

        var isAssigned = await _db.TrainerMemberAssignments
            .AnyAsync(a => a.TrainerId == trainer.Id && a.MemberId == memberId);
        if (!isAssigned) return Forbid();

        var member = await _db.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return NotFound();

        var progress = await _db.ProgressRecords
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.Date)
            .Select(p => new ProgressPointVM { Date = p.Date, Weight = p.Weight, BodyFat = p.BodyFat, Notes = p.Notes })
            .ToListAsync();

        return View(new MemberProgressVM
        {
            MemberId = memberId,
            MemberName = member.User.Name,
            Progress = progress
        });
    }

    // GET /Trainer/Schedule
    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var trainer = await GetCurrentTrainerAsync();
        if (trainer == null) return NotFound();

        var classIds = await _db.Classes
            .Where(c => c.TrainerId == trainer.Id)
            .Select(c => c.Id)
            .ToListAsync();

        var slotsEntities = await _db.ClassSchedules
            .Include(cs => cs.Class)
            .Where(cs => classIds.Contains(cs.ClassId))
            .ToListAsync();

        slotsEntities = slotsEntities.OrderBy(cs => cs.DayOfWeek).ThenBy(cs => cs.StartTime).ToList();

        var bookingCounts = await _db.Bookings
            .Where(b => classIds.Contains(b.ClassId) && b.Status == BookingStatus.Confirmed)
            .GroupBy(b => b.ClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToListAsync();

        var slots = slotsEntities.Select(cs => new TrainerScheduleSlotVM
        {
            ClassId = cs.ClassId,
            ClassName = cs.Class.ClassName,
            Capacity = cs.Class.Capacity,
            BookedCount = bookingCounts.FirstOrDefault(bc => bc.ClassId == cs.ClassId)?.Count ?? 0,
            DayOfWeek = cs.DayOfWeek,
            StartTime = cs.StartTime,
            EndTime = cs.EndTime
        }).ToList();

        return View(new TrainerScheduleVM { Slots = slots });
    }
}
