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


    //LOGIN
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


    //REGISTER
    [HttpGet]
    public IActionResult Register() => View(new Admin());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(Admin input, string? SecurityAnswer)
    {
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 8)
        {
            ViewBag.Error = "Use a username and a password of at least 8 characters.";
            return View(input);
        }

        if (string.IsNullOrWhiteSpace(input.SecurityQuestion) || string.IsNullOrWhiteSpace(SecurityAnswer))
        {
            ViewBag.Error = "Please select a security question and provide an answer.";
            return View(input);
        }

        var username = input.Username.Trim();
        if (await _context.Admins.AnyAsync(a => a.Username == username))
        {
            ViewBag.Error = "That username is already taken.";
            return View(input);
        }

        _context.Admins.Add(new Admin
        {
            Username         = username,
            Password         = PasswordSecurity.Hash(input.Password),
            Role             = "User",
            SecurityQuestion = input.SecurityQuestion.Trim(),
            SecurityAnswer   = SecurityAnswer.Trim().ToLowerInvariant()
        });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Account created! You can now sign in.";
        return RedirectToAction(nameof(Login));
    }


    //PROFILE
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var adminId = HttpContext.Session.GetInt32("AdminId");
        if (adminId is null) return RedirectToAction(nameof(Login));
        var admin = await _context.Admins.FindAsync(adminId.Value);
        return admin is null ? RedirectToAction(nameof(Login)) : View(admin);
    }


    //LOGOUT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();
}
