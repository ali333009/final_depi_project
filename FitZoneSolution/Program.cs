using FitZone.Data;
using FitZone.Models.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// We use ASP.NET Core's built-in password hasher (same algorithm as Identity)
// against our own simple Users table, rather than pulling in the full
// Identity UI/UserManager stack. Keeps auth aligned with the Users table
// design in the spec (Id, Name, Email, PasswordHash, Role).
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MemberOnly", p => p.RequireRole("Member"));
    options.AddPolicy("TrainerOnly", p => p.RequireRole("Trainer"));
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

var app = builder.Build();

// ---------- Seed database (dev convenience) ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
}

// ---------- Middleware pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
