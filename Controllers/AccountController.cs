using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Models;
using System.Linq;

namespace StockFlow.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===========================
        // LOGIN PAGE
        // ===========================

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Username") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // ===========================
        // LOGIN
        // ===========================

        [HttpPost]
        public IActionResult Login(Admin admin)
        {
            if (!ModelState.IsValid)
                return View(admin);

            var user = _context.Admins.FirstOrDefault(x =>
                x.Username == admin.Username &&
                x.Password == admin.Password);

            if (user == null)
            {
                ViewBag.Error = "Invalid Username or Password";
                return View(admin);
            }

            HttpContext.Session.SetString("Username", user.Username);

            return RedirectToAction("Index", "Home");
        }

        // ===========================
        // REGISTER PAGE
        // ===========================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ===========================
        // REGISTER
        // ===========================

        [HttpPost]
        public IActionResult Register(Admin admin)
        {
            if (!ModelState.IsValid)
                return View(admin);

            bool exists = _context.Admins.Any(x => x.Username == admin.Username);

            if (exists)
            {
                ViewBag.Error = "Username already exists!";
                return View(admin);
            }

            _context.Admins.Add(admin);
            _context.SaveChanges();

            TempData["Success"] = "Registration Successful. Please Login.";

            return RedirectToAction("Login");
        }

        // ===========================
        // MY PROFILE
        // ===========================

        [HttpGet]
        public IActionResult Profile()
        {
            var username = HttpContext.Session.GetString("Username");

            if (username == null)
                return RedirectToAction("Login");

            var admin = _context.Admins.FirstOrDefault(x => x.Username == username);

            if (admin == null)
                return RedirectToAction("Login");

            return View(admin);
        }

        // ===========================
        // UPDATE PROFILE
        // ===========================

        [HttpPost]
        public IActionResult Profile(Admin admin)
        {
            var user = _context.Admins.Find(admin.Id);

            if (user == null)
                return NotFound();

            user.Username = admin.Username;

            _context.SaveChanges();

            HttpContext.Session.SetString("Username", user.Username);

            TempData["Success"] = "Profile Updated Successfully.";

            return RedirectToAction("Profile");
        }

        // ===========================
        // LOGOUT
        // ===========================

        // ===========================
        // LOGOUT
        // ===========================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }
    }
}
