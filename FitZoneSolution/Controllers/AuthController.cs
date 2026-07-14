using System.Security.Claims;
using FitZone.Data;
using FitZone.Models.Entities;
using FitZone.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitZone.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // GET /Auth/Register
    [HttpGet]
    public IActionResult Register() => View(new RegisterVM());

    // POST /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == model.Email);
        if (emailTaken)
        {
            ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
            return View(model);
        }

        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            Role = UserRole.Member
        };
        user.PasswordHash = _hasher.HashPassword(user, model.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var member = new Member
        {
            UserId = user.Id,
            Phone = model.Phone,
            MembershipDate = DateTime.UtcNow
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        await SignInUserAsync(user);
        return RedirectToAction("Dashboard", "Member");
    }

    // GET /Auth/Login
    [HttpGet]
    public IActionResult Login() => View(new LoginVM());

    // POST /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInUserAsync(user, model.RememberMe);

        return user.Role switch
        {
            UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
            UserRole.Trainer => RedirectToAction("Dashboard", "Trainer"),
            _ => RedirectToAction("Dashboard", "Member")
        };
    }

    // POST /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private async Task SignInUserAsync(User user, bool rememberMe = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }
}
