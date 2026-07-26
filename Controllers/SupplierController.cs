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

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

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

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var supplier = _context.Suppliers.Find(id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

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

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var supplier = _context.Suppliers.Find(id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var supplier = _context.Suppliers.Find(id);

            if (supplier == null)
                return RedirectToAction("Index");

            // Purchase orders MUST have a supplier (required link) -> block delete and explain.
            bool usedInPurchaseOrders = _context.PurchaseOrders.Any(p => p.SupplierId == id);
            if (usedInPurchaseOrders)
            {
                TempData["Error"] = $"'{supplier.SupplierName}' delete nahi ho sakta kyunki iske purchase orders maujood hain. Pehle un orders ko hataayein ya kisi aur supplier se jodein.";
                return RedirectToAction("Index");
            }

            // Products can exist without a supplier (optional link) -> unlink them, then delete.
            var linkedProducts = _context.Products.Where(p => p.SupplierId == id).ToList();
            foreach (var product in linkedProducts)
                product.SupplierId = null;

            _context.Suppliers.Remove(supplier);
            _context.SaveChanges();
            TempData["Success"] = $"Supplier '{supplier.SupplierName}' delete ho gaya.";
            return RedirectToAction("Index");
        }
    }
}
