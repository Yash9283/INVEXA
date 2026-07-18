using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;
using StockFlow.Models;
using System.Linq;

namespace StockFlow.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Invoice List
        public IActionResult Index()
        {
            var invoices = _context.Invoices.ToList();
            return View(invoices);
        }

        // Generate Invoice (form dikhao)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Generate Invoice (form submit)
        [HttpPost]
        public IActionResult Create(Invoice invoice)
        {
            // Ye dono field form me nahi hote, controller khud set karta hai
            // isliye inki validation skip kar rahe hain
            ModelState.Remove("InvoiceNumber");
            ModelState.Remove("PaymentStatus");

            if (ModelState.IsValid)
            {
                invoice.InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                invoice.InvoiceDate = DateTime.Now;
                invoice.TotalAmount = invoice.Quantity * invoice.UnitPrice;
                invoice.PaymentStatus = "Paid";

                _context.Invoices.Add(invoice);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(invoice);
        }

        // Invoice Details
        public IActionResult Details(int id)
        {
            var invoice = _context.Invoices.Find(id);

            if (invoice == null)
                return NotFound();

            return View(invoice);
        }

        // Delete Invoice (confirmation dikhao)
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var invoice = _context.Invoices.Find(id);

            if (invoice == null)
                return NotFound();

            return View(invoice);
        }

        // Delete Invoice (confirm hone par delete karo)
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var invoice = _context.Invoices.Find(id);

            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}