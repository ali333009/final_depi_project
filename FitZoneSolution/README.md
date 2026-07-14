# FitZone Gym Management System

ASP.NET Core MVC + Entity Framework Core (Code-First), built from the FitZone
documentation (v2.0): member registration & subscriptions, class booking with
capacity/duplicate-booking rules, trainer workout plans & attendance, and an
Admin panel with member/trainer/plan/class management, a weekly class
schedule calendar, and revenue reports.

## Tech stack
- ASP.NET Core MVC (.NET 8)
- Entity Framework Core (SQL Server), Code-First with Fluent API
- Cookie authentication (custom `Users` table + `PasswordHasher<User>`, no full Identity UI)
- Bootstrap 5 + Bootstrap Icons

## Project structure
```
FitZoneSolution/
├── Controllers/        AuthController, MemberController, TrainerController, AdminController
├── Data/                AppDbContext, DbSeeder
├── Models/
│   ├── Entities/        User, Member, Trainer, SubscriptionPlan, Subscription,
│   │                    Payment, GymClass, ClassSchedule, Booking, WorkoutPlan,
│   │                    ProgressTracking, TrainerMemberAssignment
│   └── ViewModels/       Auth/Member/Trainer/Admin view models
├── Views/                Razor views per role (Auth, Member, Trainer, Admin, Shared)
├── wwwroot/css/site.css  Navy & gold theme
├── Program.cs
├── appsettings.json
└── FitZoneSolution.csproj
```

## Getting started

1. **Prerequisites**: [.NET 8 SDK](https://dotnet.microsoft.com/download), SQL Server
   (LocalDB, Express, or full) or swap the provider to SQLite if you prefer.

2. **Connection string** — edit `appsettings.json`:
   ```json
   "DefaultConnection": "Server=.\\SQLEXPRESS;Database=FitZoneDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   ```

3. **Restore & create the database** (from the project folder):
   ```bash
   dotnet restore
   dotnet tool install --global dotnet-ef   # if not already installed
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   `Program.cs` also calls `db.Database.Migrate()` and seeds an Admin account
   and 3 sample subscription plans automatically on first run, so the
   `dotnet ef database update` step is optional if you just run the app.

4. **Run**:
   ```bash
   dotnet run
   ```

5. **Seeded Admin login**:
   - Email: `admin@fitzone.local`
   - Password: `Admin@123`

   Create Trainers from **Admin → Trainers → New Trainer**; members can
   self-register from the Login page.

## Business rules implemented
- Class booking blocked once a class hits `Capacity`.
- A member cannot double-book the same class (checked in `MemberController.BookClass`).
- Booking requires an **active** subscription (`Subscription.Status == Active` and not expired).
- Subscribing creates a `Payment` record and flips the subscription to Active in
  one DB transaction (simulated payment confirmation).
- Trainers only see members assigned to them by Admin (`TrainerMemberAssignment`
  join table), enforced both in the query and with a `Forbid()` check on direct
  URL access to a specific member's workout page.
- Only Admin can delete; deletes are soft (`IsDeleted` flag + EF global query
  filters), so historical bookings/payments referencing a removed user still
  resolve.
- Weekly schedule slots for the same class/day are checked for time overlap
  before being added.

## Notes / next steps
- This was scaffolded in a sandboxed environment without access to nuget.org,
  so the code hasn't been compiled here — read through `Program.cs` and the
  controllers once before your first `dotnet build` in case of small typos,
  and run `dotnet ef migrations add InitialCreate` to generate the actual
  migration files (not included, since they're machine/EF-version specific).
- Future roadmap ideas from the spec: JWT for API access, ApexCharts on the
  Reports page, email/SMS notifications, a real payment gateway, AI workout
  recommendations, PWA support.
