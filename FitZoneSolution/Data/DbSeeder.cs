using FitZone.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace FitZone.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db, IPasswordHasher<User> hasher)
    {
        
        if (!db.Users.Any(u => u.Role == UserRole.Admin))
        {
            var admin = new User
            {
                Name = "System Admin",
                Email = "admin@fitzone.local",
                Role = UserRole.Admin
            };

            admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");
            db.Users.Add(admin);
        }

        
        // Trainers Users
        
        if (!db.Users.Any(u => u.Role == UserRole.Trainer))
        {
            var trainersUsers = new List<User>
            {
                new User
                {
                    Name = "Ali Salama",
                    Email = "ahmed@fitzone.local",
                    Role = UserRole.Trainer
                },
                new User
                {
                    Name = "AliWael",
                    Email = "AliWael@gmail",
                    Role = UserRole.Trainer
                },
                new User
                {
                    Name = "Hazem",
                    Email = "Hazem@gmail",
                    Role = UserRole.Trainer
                },
                new User
                {
                    Name="mina",
                    Email="Mina@gmail",
                    Role=UserRole.Trainer

                }
                
            };

            foreach (var user in trainersUsers)
            {
                user.PasswordHash = hasher.HashPassword(user, "Trainer@123");
            }

            db.Users.AddRange(trainersUsers);
        }

        // Subscription Plans
        if (!db.SubscriptionPlans.Any())
        {
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    PlanName = "Monthly Basic",
                    DurationMonths = 1,
                    Price = 500
                },
                new SubscriptionPlan
                {
                    PlanName = "Quarterly Standard",
                    DurationMonths = 3,
                    Price = 1350
                },
                new SubscriptionPlan
                {
                    PlanName = "Annual Premium",
                    DurationMonths = 12,
                    Price = 4800
                }
            );
        }

        db.SaveChanges();
        
        // Trainers
        
        if (!db.Trainers.Any())
        {
            var trainerUsers = db.Users
                .Where(x => x.Role == UserRole.Trainer)
                .ToList();

            db.Trainers.AddRange(
                new Trainer
                {
                    UserId = trainerUsers[0].Id,
                    Specialization = "Strength & Conditioning 🏋️ 💪\n"
                },
                new Trainer
                {
                    UserId = trainerUsers[1].Id,
                    Specialization = "Yoga & Flexibility 🧘 "
                },
                new Trainer
                {
                    UserId = trainerUsers[2].Id,
                    Specialization = "Boxing & HIIT 🥊"
                },
                new Trainer
                {
                    UserId = trainerUsers[3].Id,
                    Specialization= "Nutrition & Wellness"
                }
                
            );

            db.SaveChanges();
        }

        
        // Gym Classes
        
        if (!db.Classes.Any())
        {
            var trainers = db.Trainers.ToList();

            db.Classes.AddRange(
                new GymClass
                {
                    ClassName = "Bodybuilding Beginners",
                    TrainerId = trainers[0].Id,
                    Capacity = 20,
                    Duration = 60
                },
                new GymClass
                {
                    ClassName = "CrossFit Advanced",
                    TrainerId = trainers[1].Id,
                    Capacity = 15,
                    Duration = 75
                },
                new GymClass
                {
                    ClassName = "Morning Yoga",
                    TrainerId = trainers[2].Id,
                    Capacity = 25,
                    Duration = 45
                },
                new GymClass
                {
                    ClassName = "HIIT Workout",
                    TrainerId = trainers[1].Id,
                    Capacity = 18,
                    Duration = 50
                }
            );
        }
        
        // Class Schedules
        
        if (!db.ClassSchedules.Any())
        {
            var classes = db.Classes.ToList();

            db.ClassSchedules.AddRange(

                // Bodybuilding Beginners
                new ClassSchedule
                {
                    ClassId = classes[0].Id,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0)
                },
                new ClassSchedule
                {
                    ClassId = classes[0].Id,
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0)
                },

                // CrossFit Advanced
                new ClassSchedule
                {
                    ClassId = classes[1].Id,
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(19, 15, 0)
                },
                new ClassSchedule
                {
                    ClassId = classes[1].Id,
                    DayOfWeek = DayOfWeek.Thursday,
                    StartTime = new TimeSpan(18, 0, 0),
                    EndTime = new TimeSpan(19, 15, 0)
                },

                // Morning Yoga
                new ClassSchedule
                {
                    ClassId = classes[2].Id,
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(8, 45, 0)
                },
                new ClassSchedule
                {
                    ClassId = classes[2].Id,
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(8, 45, 0)
                },

                // HIIT Workout
                new ClassSchedule
                {
                    ClassId = classes[3].Id,
                    DayOfWeek = DayOfWeek.Friday,
                    StartTime = new TimeSpan(17, 0, 0),
                    EndTime = new TimeSpan(17, 50, 0)
                },
                new ClassSchedule
                {
                    ClassId = classes[3].Id,
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = new TimeSpan(17, 0, 0),
                    EndTime = new TimeSpan(17, 50, 0)
                }
            );

            db.SaveChanges();
        }
       
        // Subscriptions
        
        if (!db.Subscriptions.Any())
        {
            var members = db.Members.ToList();
            var plans = db.SubscriptionPlans.ToList();

            if (members.Count >= 4 && plans.Count >= 3)
            {
                db.Subscriptions.AddRange(

                    new Subscription
                    {
                        MemberId = members[0].Id,
                        PlanId = plans[0].Id,
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddMonths(1),
                        Status = SubscriptionStatus.Active
                    },

                    new Subscription
                    {
                        MemberId = members[1].Id,
                        PlanId = plans[1].Id,
                        StartDate = DateTime.Today.AddDays(-15),
                        EndDate = DateTime.Today.AddMonths(3).AddDays(-15),
                        Status = SubscriptionStatus.Active
                    },

                    new Subscription
                    {
                        MemberId = members[2].Id,
                        PlanId = plans[2].Id,
                        StartDate = DateTime.Today.AddMonths(-12),
                        EndDate = DateTime.Today,
                        Status = SubscriptionStatus.Expired
                    },

                    new Subscription
                    {
                        MemberId = members[3].Id,
                        PlanId = plans[0].Id,
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddMonths(1),
                        Status = SubscriptionStatus.PendingPayment
                    }

                );

                db.SaveChanges();
            }
        }

        db.SaveChanges();
    }

}