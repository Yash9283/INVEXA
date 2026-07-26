using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;
using StockFlow.Models;
using StockFlow.Filters;
using System.Linq;

namespace StockFlow.Controllers
{
    [SessionAuthorize]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var suppliers = _context.Suppliers.ToList();
            return View(suppliers);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Add(supplier);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(supplier);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var supplier = _context.Suppliers.Find(id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        [SessionAuthorize("Admin")]
        [HttpPost]
        public IActionResult Edit(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Update(supplier);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(supplier);
        }

        [SessionAuthorize("Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var supplier = _context.Suppliers.Find(id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        [SessionAuthorize("Admin")]
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var supplier = _context.Suppliers.Find(id);

            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
