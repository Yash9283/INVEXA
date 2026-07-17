using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;
using StockFlow.Models;
using System.Linq;

namespace StockFlow.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var sales = _context.Sales.ToList();
            return View(sales);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Sale sale)
        {
            if (ModelState.IsValid)
            {
                _context.Sales.Add(sale);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(sale);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var sale = _context.Sales.Find(id);

            if (sale == null)
                return NotFound();

            return View(sale);
        }

        [HttpPost]
        public IActionResult Edit(Sale sale)
        {
            if (ModelState.IsValid)
            {
                _context.Sales.Update(sale);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(sale);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var sale = _context.Sales.Find(id);

            if (sale == null)
                return NotFound();

            return View(sale);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var sale = _context.Sales.Find(id);

            if (sale != null)
            {
                _context.Sales.Remove(sale);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}