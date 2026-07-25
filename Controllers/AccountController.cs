using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Models;
using StockFlow.Security;

namespace StockFlow.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    public AccountController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public IActionResult Login() => HttpContext.Session.GetString("Username") is null
        ? View(new Admin()) : RedirectToAction("Index", "Home");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(Admin input)
    {
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrEmpty(input.Password))
        {
            ViewBag.Error = "Enter your username and password.";
            return View(input);
        }

        var username = input.Username.Trim();
        var user = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (user is null || !PasswordSecurity.Verify(input.Password, user.Password))
        {
            ViewBag.Error = "Invalid username or password.";
            return View(input);
        }

        // Existing demo accounts are converted without retaining their plain-text password.
        if (PasswordSecurity.NeedsUpgrade(user.Password))
        {
            user.Password = PasswordSecurity.Hash(input.Password);
            await _context.SaveChangesAsync();
        }

        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetInt32("AdminId", user.Id);
        HttpContext.Session.SetString("Role", user.Role);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        // Registration is a one-time bootstrap step; subsequent user creation should be an admin workflow.
        if (await _context.Admins.AnyAsync()) return RedirectToAction(nameof(Login));
        return View(new Admin());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(Admin input)
    {
        if (await _context.Admins.AnyAsync()) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 8)
        {
            ViewBag.Error = "Use a username and a password of at least 8 characters.";
            return View(input);
        }

        var username = input.Username.Trim();
        if (await _context.Admins.AnyAsync(a => a.Username == username))
        {
            ViewBag.Error = "That username is already in use.";
            return View(input);
        }

        _context.Admins.Add(new Admin
        {
            Username = username,
            Password = PasswordSecurity.Hash(input.Password),
            Role = "Admin"
        });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Account created. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var adminId = HttpContext.Session.GetInt32("AdminId");
        if (adminId is null) return RedirectToAction(nameof(Login));
        var admin = await _context.Admins.FindAsync(adminId.Value);
        return admin is null ? RedirectToAction(nameof(Login)) : View(admin);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
