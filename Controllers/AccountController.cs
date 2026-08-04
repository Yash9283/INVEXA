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
        var lockKey = "LockoutUntil_user";
        var failKey = "FailedLogins_user";
        var lockTick = HttpContext.Session.GetString(lockKey);
        if (lockTick != null && DateTime.UtcNow.Ticks < long.Parse(lockTick))
        {
            var remaining = (int)((long.Parse(lockTick) - DateTime.UtcNow.Ticks) / TimeSpan.TicksPerMinute) + 1;
            ViewBag.Error = $"Too many failed attempts. Try again in {remaining} minute(s).";
            return View(input);
        }

        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrEmpty(input.Password))
        {
            ViewBag.Error = "Enter your username and password.";
            return View(input);
        }

        var username = input.Username.Trim();
        var user = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (user is null || !PasswordSecurity.Verify(input.Password, user.Password))
        {
            var fails = (HttpContext.Session.GetInt32(failKey) ?? 0) + 1;
            HttpContext.Session.SetInt32(failKey, fails);
            if (fails >= 5)
            {
                HttpContext.Session.SetString(lockKey, (DateTime.UtcNow.AddMinutes(15)).Ticks.ToString());
                HttpContext.Session.Remove(failKey);
                ViewBag.Error = "Too many failed attempts. Account locked for 15 minutes.";
            }
            else
            {
                ViewBag.Error = $"Invalid username or password. {5 - fails} attempt(s) remaining.";
            }
            return View(input);
        }

        if (user.Role != "User")
        {
            ViewBag.Error = "This login is for User accounts only.";
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
        HttpContext.Session.Remove(failKey);
        HttpContext.Session.Remove(lockKey);
        return RedirectToAction("Index", "Home");
    }

    // Supplier credentials are accepted only from the Supplier tab.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SupplierLogin(Admin input)
    {
        // ── Brute-force guard ──
        var lockKey = "LockoutUntil_supplier";
        var failKey = "FailedLogins_supplier";
        var lockTick = HttpContext.Session.GetString(lockKey);
        if (lockTick != null && DateTime.UtcNow.Ticks < long.Parse(lockTick))
        {
            var remaining = (int)((long.Parse(lockTick) - DateTime.UtcNow.Ticks) / TimeSpan.TicksPerMinute) + 1;
            ViewBag.Error = $"Too many failed attempts. Try again in {remaining} minute(s).";
            ViewBag.ActiveTab = "supplier";
            return View("Login", input);
        }

        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrEmpty(input.Password))
        {
            ViewBag.Error = "Enter your supplier username and password.";
            ViewBag.ActiveTab = "supplier";
            return View("Login", input);
        }

        var username = input.Username.Trim();
        var user = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (user is null || !PasswordSecurity.Verify(input.Password, user.Password))
        {
            var fails = (HttpContext.Session.GetInt32(failKey) ?? 0) + 1;
            HttpContext.Session.SetInt32(failKey, fails);
            if (fails >= 5)
            {
                HttpContext.Session.SetString(lockKey, (DateTime.UtcNow.AddMinutes(15)).Ticks.ToString());
                HttpContext.Session.Remove(failKey);
                ViewBag.Error = "Too many failed attempts. Account locked for 15 minutes.";
            }
            else
            {
                ViewBag.Error = $"Invalid username or password. {5 - fails} attempt(s) remaining.";
            }
            ViewBag.ActiveTab = "supplier";
            return View("Login", input);
        }

        if (user.Role != "Supplier" || user.SupplierId is null ||
            !await _context.Suppliers.AnyAsync(s => s.Id == user.SupplierId.Value))
        {
            ViewBag.Error = "This login is for Supplier accounts only.";
            ViewBag.ActiveTab = "supplier";
            return View("Login", input);
        }

        if (PasswordSecurity.NeedsUpgrade(user.Password))
        {
            user.Password = PasswordSecurity.Hash(input.Password);
            await _context.SaveChangesAsync();
        }

        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetInt32("AdminId", user.Id);
        HttpContext.Session.SetString("Role", "Supplier");
        HttpContext.Session.Remove(failKey);
        HttpContext.Session.Remove(lockKey);
        return RedirectToAction("MyOrders", "SupplierPortal");
    }

    // Admin tab submits to this action — separate from Login POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(Admin input)
    {
        var lockKey = "LockoutUntil_admin";
        var failKey = "FailedLogins_admin";

        // ── Hardcoded admin credentials (Admin / Admin@123) ──
        if ((input.Username?.Trim() ?? "") == "Admin" && (input.Password ?? "") == "Admin@123")
        {
            var seedAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == "Admin")
                          ?? await _context.Admins.FirstOrDefaultAsync(a => a.Role == "Admin");
            HttpContext.Session.SetString("Username", "Admin");
            HttpContext.Session.SetInt32("AdminId", seedAdmin?.Id ?? 0);
            HttpContext.Session.SetString("Role", "Admin");
            HttpContext.Session.Remove(failKey);
            HttpContext.Session.Remove(lockKey);
            return RedirectToAction("Index", "Home");
        }
        var lockTick = HttpContext.Session.GetString(lockKey);
        if (lockTick != null && DateTime.UtcNow.Ticks < long.Parse(lockTick))
        {
            var remaining = (int)((long.Parse(lockTick) - DateTime.UtcNow.Ticks) / TimeSpan.TicksPerMinute) + 1;
            ViewBag.Error = $"Too many failed attempts. Try again in {remaining} minute(s).";
            ViewBag.ActiveTab = "admin";
            return View("Login", input);
        }

        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrEmpty(input.Password))
        {
            ViewBag.Error = "Enter your username and password.";
            ViewBag.ActiveTab = "admin";
            return View("Login", input);
        }

        var username = input.Username.Trim();
        var user = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (user is null || !PasswordSecurity.Verify(input.Password, user.Password))
        {
            var fails = (HttpContext.Session.GetInt32(failKey) ?? 0) + 1;
            HttpContext.Session.SetInt32(failKey, fails);
            if (fails >= 5)
            {
                HttpContext.Session.SetString(lockKey, (DateTime.UtcNow.AddMinutes(15)).Ticks.ToString());
                HttpContext.Session.Remove(failKey);
                ViewBag.Error = "Too many failed attempts. Account locked for 15 minutes.";
            }
            else
            {
                ViewBag.Error = $"Invalid username or password. {5 - fails} attempt(s) remaining.";
            }
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
        HttpContext.Session.Remove(failKey);
        HttpContext.Session.Remove(lockKey);
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

    // UPLOAD PROFILE PHOTO
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile photoFile)
    {
        var adminId = HttpContext.Session.GetInt32("AdminId");
        if (adminId is null) return RedirectToAction(nameof(Login));
        
        var admin = await _context.Admins.FindAsync(adminId.Value);
        if (admin is null) return RedirectToAction(nameof(Login));

        if (photoFile == null || photoFile.Length == 0)
        {
            TempData["Error"] = "Please select a photo to upload.";
            return RedirectToAction(nameof(Profile));
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            TempData["Error"] = "Only image files (JPG, PNG, GIF) are allowed.";
            return RedirectToAction(nameof(Profile));
        }

        // Validate file size (max 5MB)
        if (photoFile.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "File size must be less than 5MB.";
            return RedirectToAction(nameof(Profile));
        }

        // Create profiles directory if it doesn't exist
        var profilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
        if (!Directory.Exists(profilesPath))
        {
            Directory.CreateDirectory(profilesPath);
        }

        // Delete old photo if exists
        if (!string.IsNullOrEmpty(admin.ProfilePhoto))
        {
            var oldPhotoPath = Path.Combine(profilesPath, admin.ProfilePhoto);
            if (System.IO.File.Exists(oldPhotoPath))
            {
                System.IO.File.Delete(oldPhotoPath);
            }
        }

        // Generate unique filename
        var fileName = $"{admin.Id}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(profilesPath, fileName);

        // Save the file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photoFile.CopyToAsync(stream);
        }

        // Update database
        admin.ProfilePhoto = fileName;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile photo uploaded successfully!";
        return RedirectToAction(nameof(Profile));
    }

    // DELETE PROFILE PHOTO
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProfilePhoto()
    {
        var adminId = HttpContext.Session.GetInt32("AdminId");
        if (adminId is null) return RedirectToAction(nameof(Login));
        
        var admin = await _context.Admins.FindAsync(adminId.Value);
        if (admin is null) return RedirectToAction(nameof(Login));

        if (!string.IsNullOrEmpty(admin.ProfilePhoto))
        {
            var profilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
            var filePath = Path.Combine(profilesPath, admin.ProfilePhoto);
            
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            admin.ProfilePhoto = null;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Profile photo removed successfully!";
        }

        return RedirectToAction(nameof(Profile));
    }


    //CHANGE CREDENTIALS (username + password) - for the logged-in user
    [HttpGet]
    public async Task<IActionResult> ChangeCredentials()
    {
        var adminId = HttpContext.Session.GetInt32("AdminId");
        if (adminId is null) return RedirectToAction(nameof(Login));
        var admin = await _context.Admins.FindAsync(adminId.Value);
        return admin is null ? RedirectToAction(nameof(Login)) : View(admin);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeCredentials(string currentPassword, string newUsername, string? newPassword, string? confirmPassword)
    {
        var adminId = HttpContext.Session.GetInt32("AdminId");
        if (adminId is null) return RedirectToAction(nameof(Login));
        var admin = await _context.Admins.FindAsync(adminId.Value);
        if (admin is null) return RedirectToAction(nameof(Login));

        // 1) Current password must be correct to authorise any change
        if (string.IsNullOrEmpty(currentPassword) || !PasswordSecurity.Verify(currentPassword, admin.Password))
        {
            ViewBag.Error = "Current password is incorrect.";
            return View(admin);
        }

        // 2) Validate the new username
        newUsername = (newUsername ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(newUsername))
        {
            ViewBag.Error = "Username cannot be empty.";
            return View(admin);
        }
        if (newUsername != admin.Username &&
            await _context.Admins.AnyAsync(a => a.Username == newUsername && a.Id != admin.Id))
        {
            ViewBag.Error = "That username is already taken.";
            return View(admin);
        }

        // 3) New password is optional - change only if provided
        if (!string.IsNullOrEmpty(newPassword))
        {
            if (newPassword.Length < 8)
            {
                ViewBag.Error = "New password must be at least 8 characters.";
                return View(admin);
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New passwords do not match.";
                return View(admin);
            }
            admin.Password = PasswordSecurity.Hash(newPassword);
        }

        admin.Username = newUsername;
        await _context.SaveChangesAsync();

        // Keep the session in sync with the new username
        HttpContext.Session.SetString("Username", admin.Username);

        NotificationHelper.Add(_context, $"Credentials updated for '{admin.Username}'.", "Account", "Admin");
        await _context.SaveChangesAsync();

        TempData["Success"] = "Your username / password has been updated successfully.";
        return RedirectToAction(nameof(Profile));
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

    // CREATE SUPPLIER LOGIN (Admin only) - links a login account to a Supplier
    [StockFlow.Filters.SessionAuthorize("Admin")]
    [HttpGet]
    public async Task<IActionResult> CreateSupplierLogin()
    {
        ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();
        return View();
    }

    [StockFlow.Filters.SessionAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplierLogin(int supplierId, string username, string password)
    {
        async Task ReloadSuppliers() => ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();

        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier is null)
        {
            ViewBag.Error = "Please choose a valid supplier.";
            await ReloadSuppliers(); return View();
        }

        username = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            ViewBag.Error = "Enter a username and a password of at least 8 characters.";
            await ReloadSuppliers(); return View();
        }

        if (await _context.Admins.AnyAsync(a => a.Username == username))
        {
            ViewBag.Error = "That username is already taken.";
            await ReloadSuppliers(); return View();
        }

        _context.Admins.Add(new Admin
        {
            Username   = username,
            Password   = PasswordSecurity.Hash(password),
            Role       = "Supplier",
            SupplierId = supplier.Id
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Supplier login created for '{supplier.SupplierName}' (username: {username}).";
        return RedirectToAction(nameof(Users));
    }

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
