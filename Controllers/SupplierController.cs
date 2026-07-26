using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;
using StockFlow.Models;
using StockFlow.Filters;
using System.Linq;
using StockFlow.Helpers;

namespace StockFlow.Controllers
{
    [SessionAuthorize]
    public class SupplierController : BaseController
    {
        public SupplierController(ApplicationDbContext context) : base(context) { }

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

                NotificationHelper.Add(_context,
                $"Supplier added: {supplier.SupplierName}",
                "Supplier", "Admin");
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

                NotificationHelper.Add(_context,
                $"Supplier updated: {supplier.SupplierName}",
                "Supplier", "Admin");
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
                NotificationHelper.Add(_context,
                $"Supplier deleted: {supplier.SupplierName}",
                "Supplier", "Admin");
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
