using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Models;
using StockFlow.Security;
using StockFlow.Helpers;

namespace StockFlow.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    public AccountController(ApplicationDbContext context) => _context = context;

    public static readonly List<string> SecurityQuestions = new()
    {
        "What was the name of your first pet?",
        "What is your mother's maiden name?",
        "What was the name of your primary school?",
        "What is your favourite movie?",
        "What city were you born in?",
        "What is the name of your childhood best friend?",
        "What was your childhood nickname?"
    };

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

        if (user.Role == "Admin")
        {
            ViewBag.Error = "Admin accounts must use the Admin tab.";
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

        NotificationHelper.Add(_context,
        $"User '{user.Username}' logged in.",
        "Account", "Admin");
        return RedirectToAction("Index", "Home");
    }

    // Admin tab submits to this action — separate from Login POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(Admin input)
    {
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrEmpty(input.Password))
        {
            ViewBag.Error = "Enter your username and password.";
            ViewBag.ActiveTab = "admin";   // keep admin tab active on error
            return View("Login", input);
        }

        var username = input.Username.Trim();
        var user = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (user is null || !PasswordSecurity.Verify(input.Password, user.Password))
        {
            ViewBag.Error = "Invalid username or password.";
            ViewBag.ActiveTab = "admin";
            return View("Login", input);
        }

        // ── Block non-Admin accounts from the Admin tab ──
        if (user.Role != "Admin")
        {
            ViewBag.Error = "This login is for Administrators only.";
            ViewBag.ActiveTab = "admin";
            return View("Login", input);
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

        NotificationHelper.Add(_context,
        $"New account registered: {username}",
        "Staff", "Admin");
        await _context.SaveChangesAsync();

        TempData["Success"] = "Account created! You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    // FORGOT PASSWORD — Step 1: Enter username

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        // Guard: if somehow already logged in, just go home
        if (HttpContext.Session.GetString("Username") is not null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            ViewBag.Error = "Please enter your username.";
            return View();
        }

        var user = await _context.Admins.FirstOrDefaultAsync(a =>
            a.Username == username.Trim());

        if (user is null || string.IsNullOrEmpty(user.SecurityQuestion))
        {
            // Deliberately vague — don't reveal whether the account exists
            ViewBag.Error = "No account found, or no security question is set for this account.";
            return View();
        }

        HttpContext.Session.SetString("ResetUsername", user.Username);
        return RedirectToAction(nameof(VerifyQuestion));
    }

    // FORGOT PASSWORD — Step 2: Answer security question

    [HttpGet]
    public async Task<IActionResult> VerifyQuestion()
    {
        var username = HttpContext.Session.GetString("ResetUsername");
        if (username is null) return RedirectToAction(nameof(ForgotPassword));

        var user = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (user is null) return RedirectToAction(nameof(ForgotPassword));

        ViewBag.SecurityQuestion = user.SecurityQuestion;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyQuestion(string answer)
    {
        var username = HttpContext.Session.GetString("ResetUsername");
        if (username is null) return RedirectToAction(nameof(ForgotPassword));

        var user = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (user is null) return RedirectToAction(nameof(ForgotPassword));

        ViewBag.SecurityQuestion = user.SecurityQuestion;

        if (string.IsNullOrWhiteSpace(answer) ||
            answer.Trim().ToLowerInvariant() != user.SecurityAnswer)
        {
            ViewBag.Error = "Incorrect answer. Please try again.";
            return View();
        }

        HttpContext.Session.SetString("ResetVerified", "true");
        return RedirectToAction(nameof(ResetPassword));
    }

    // FORGOT PASSWORD — Step 3: Set new password

    [HttpGet]
    public IActionResult ResetPassword()
    {
        if (HttpContext.Session.GetString("ResetVerified") != "true")
            return RedirectToAction(nameof(ForgotPassword));
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
    {
        if (HttpContext.Session.GetString("ResetVerified") != "true")
            return RedirectToAction(nameof(ForgotPassword));

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            ViewBag.Error = "Password must be at least 8 characters.";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match.";
            return View();
        }

        var username = HttpContext.Session.GetString("ResetUsername");
        var user     = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);

        if (user is null) return RedirectToAction(nameof(ForgotPassword));

        // Use Hash — never store plain text
        user.Password = PasswordSecurity.Hash(newPassword);
        await _context.SaveChangesAsync();

        // Clean up reset session keys
        HttpContext.Session.Remove("ResetUsername");
        HttpContext.Session.Remove("ResetVerified");

        TempData["Success"] = "Password reset successfully. Please sign in with your new password.";
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

    // MANAGE USERS (Admin only)

    [StockFlow.Filters.SessionAuthorize("Admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _context.Admins.OrderBy(a => a.Role).ThenBy(a => a.Username).ToListAsync();
        return View(users);
    }

    [StockFlow.Filters.SessionAuthorize("Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Admins.FindAsync(id);
        if (user is null) return NotFound();

        // Prevent deleting own account
        if (user.Id == HttpContext.Session.GetInt32("AdminId"))
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Users));
        }

        return View(user);
    }

    [StockFlow.Filters.SessionAuthorize("Admin")]
    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(int id)
    {
        // Re-check own account guard on POST — don't rely on UI alone
        if (id == HttpContext.Session.GetInt32("AdminId"))
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _context.Admins.FindAsync(id);
        if (user is not null)
        {
            _context.Admins.Remove(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.Username}' deleted.";
        }

        return RedirectToAction(nameof(Users));
    }

}
