using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;
using StockFlow.Models;
using StockFlow.Filters;
using System.Linq;
using StockFlow.Helpers;

namespace StockFlow.Controllers
{
    [SessionAuthorize]
    public class CategoryController : BaseController
    {
        public CategoryController(ApplicationDbContext context) : base(context) { }

        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();

                NotificationHelper.Add(_context,
                $"Category added: {category.CategoryName}",
                "Inventory", "Admin");
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(category);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(category);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        [SessionAuthorize("Admin")]
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var category = _context.Categories.Find(id);

            if (category != null)
            {
                _context.Categories.Remove(category);

                NotificationHelper.Add(_context,
                $"Category deleted: {category.CategoryName}",
                "Inventory", "Admin");

                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
