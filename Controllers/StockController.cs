using Microsoft.AspNetCore.Mvc;
using StockFlow.Filters;

namespace StockFlow.Controllers
{
    public class StockController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}