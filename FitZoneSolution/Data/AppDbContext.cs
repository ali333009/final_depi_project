using FitZone.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitZone.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<TrainerMemberAssignment> TrainerMemberAssignments => Set<TrainerMemberAssignment>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<GymClass> Classes => Set<GymClass>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();
    public DbSet<ProgressTracking> ProgressRecords => Set<ProgressTracking>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---------- Users ----------
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
            e.HasQueryFilter(u => !u.IsDeleted);
        });

        // ---------- Member (1:1 with User) ----------
        b.Entity<Member>(e =>
        {
            e.HasOne(m => m.User)
                .WithOne(u => u.Member)
                .HasForeignKey<Member>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(m => !m.IsDeleted);
        });

        // ---------- Trainer (1:1 with User) ----------
        b.Entity<Trainer>(e =>
        {
            e.HasOne(t => t.User)
                .WithOne(u => u.Trainer)
                .HasForeignKey<Trainer>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(t => !t.IsDeleted);
        });

        // ---------- Trainer <-> Member assignment ----------
        b.Entity<TrainerMemberAssignment>(e =>
        {
            e.HasOne(a => a.Trainer)
                .WithMany(t => t.AssignedMembers)
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Member)
                .WithMany()
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // A member can only be assigned to one trainer at a time.
            e.HasIndex(a => a.MemberId).IsUnique();
        });

        // ---------- Subscription plans ----------
        b.Entity<SubscriptionPlan>(e =>
        {
            e.HasQueryFilter(p => !p.IsDeleted);
        });

        // ---------- Subscriptions ----------
        b.Entity<Subscription>(e =>
        {
            e.Property(s => s.Status).HasConversion<string>();
            e.HasOne(s => s.Member)
                .WithMany(m => m.Subscriptions)
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(s => !s.IsDeleted);
        });

        // ---------- Payments ----------
        b.Entity<Payment>(e =>
        {
            e.Property(p => p.Method).HasConversion<string>();
            e.HasOne(p => p.Subscription)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Classes ----------
        b.Entity<GymClass>(e =>
        {
            e.HasOne(c => c.Trainer)
                .WithMany(t => t.Classes)
                .HasForeignKey(c => c.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(c => !c.IsDeleted);
        });

        // ---------- Class schedules ----------
        b.Entity<ClassSchedule>(e =>
        {
            e.HasOne(cs => cs.Class)
                .WithMany(c => c.Schedules)
                .HasForeignKey(cs => cs.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(cs => cs.DayOfWeek).HasConversion<string>();
        });

        // ---------- Bookings ----------
        b.Entity<Booking>(e =>
        {
            e.Property(bk => bk.Status).HasConversion<string>();
            e.HasOne(bk => bk.Member)
                .WithMany(m => m.Bookings)
                .HasForeignKey(bk => bk.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(bk => bk.Class)
                .WithMany(c => c.Bookings)
                .HasForeignKey(bk => bk.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Business rule: prevent duplicate active booking of the same class by the same member.
            e.HasIndex(bk => new { bk.MemberId, bk.ClassId, bk.Status });
        });

        // ---------- Workout plans ----------
        b.Entity<WorkoutPlan>(e =>
        {
            e.HasOne(w => w.Trainer)
                .WithMany(t => t.WorkoutPlans)
                .HasForeignKey(w => w.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Member)
                .WithMany(m => m.WorkoutPlans)
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Progress tracking ----------
        b.Entity<ProgressTracking>(e =>
        {
            e.HasOne(pr => pr.Member)
                .WithMany(m => m.ProgressRecords)
                .HasForeignKey(pr => pr.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
