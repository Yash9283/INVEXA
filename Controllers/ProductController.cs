using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockFlow.Data;
using StockFlow.Filters;
using StockFlow.Models;
using StockFlow.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class ProductController : BaseController
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;

    public ProductController(ApplicationDbContext context, IConfiguration configuration,
        IHttpClientFactory httpClientFactory, IWebHostEnvironment environment) : base(context)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive).Include(p => p.Category).Include(p => p.Supplier)
            .OrderBy(p => p.ProductName).ToListAsync();
        return View(products);
    }

    [SessionAuthorize("Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookups();
        return View(new Product { ReorderLevel = 10, ReorderQuantity = 20, LeadTimeDays = 3 });
    }

    [SessionAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imageUpload)
    {
        product.ProductName = product.ProductName?.Trim() ?? string.Empty;
        product.SKU = product.SKU?.Trim().ToUpperInvariant() ?? string.Empty;
        await ValidateProduct(product);
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(product);
        }

        product.ImageUrl = await SaveProductImage(imageUpload, product.ImageUrl);
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(product);
        }

        product.CategoryName = await CategoryName(product.CategoryId);
        product.UpdatedAt = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        if (product.Quantity > 0)
        {
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, MovementType = "Opening", QuantityDelta = product.Quantity,
                BalanceAfter = product.Quantity, ReferenceNumber = "OPENING", Notes = "Opening inventory",
                PerformedBy = UserName()
            });
            await _context.SaveChangesAsync();
        }
        await transaction.CommitAsync();

        NotificationHelper.Add(_context,
        $"Product added: {product.ProductName} (SKU: {product.SKU})",
        "Inventory", "Admin");
        await _context.SaveChangesAsync();

        TempData["Success"] = "Product created and opening stock recorded.";
        return RedirectToAction(nameof(Index));
    }

    [SessionAuthorize("Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null || !product.IsActive) return NotFound();
        await LoadLookups();
        return View(product);
    }

    [SessionAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string productName, string sku, decimal costPrice, decimal price,
        int reorderLevel, int reorderQuantity, int leadTimeDays, int? categoryId, int? supplierId, string? imageUrl, IFormFile? imageUpload)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null || !product.IsActive) return NotFound();

        product.ProductName = productName?.Trim() ?? string.Empty;
        product.SKU = sku?.Trim().ToUpperInvariant() ?? string.Empty;
        product.CostPrice = costPrice;
        product.Price = price;
        product.ReorderLevel = reorderLevel;
        product.ReorderQuantity = reorderQuantity;
        product.LeadTimeDays = leadTimeDays;
        product.CategoryId = categoryId;
        product.SupplierId = supplierId;
        product.ImageUrl = imageUrl;
        await ValidateProduct(product, id);
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(product);
        }

        product.ImageUrl = await SaveProductImage(imageUpload, product.ImageUrl);
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(product);
        }

        product.CategoryName = await CategoryName(product.CategoryId);
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        NotificationHelper.Add(_context,
        $"Product updated: {product.ProductName}",
        "Inventory", "Admin");
        await _context.SaveChangesAsync();

        TempData["Success"] = "Product details updated. Use Stock Adjustments to change stock.";
        return RedirectToAction(nameof(Index));
    }

    [SessionAuthorize("Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound() : View(product);
    }

    [SessionAuthorize("Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return NotFound();
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        NotificationHelper.Add(_context,
        $"Product archived: {product.ProductName}",
        "Inventory", "Admin");
        await _context.SaveChangesAsync();

        TempData["Success"] = "Product archived. Sales and stock history remain intact.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AutoFetchImageOptions(string productName, int count = 5)
    {
        var placeholder = Url.Content("~/Images/product-placeholder.svg") ?? "/Images/product-placeholder.svg";
        if (string.IsNullOrWhiteSpace(productName))
            return BadRequest(new { images = new string[] { }, message = "Enter a product name first." });

        var apiKey = _configuration["ImageSearch:PexelsApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return Ok(new { images = new string[] { }, message = "Image search is not configured." });

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", apiKey);
            
            // Clean and improve search query for better product matching
            var cleanedName = productName.Trim()
                .Replace("&", "and")
                .Replace("+", "plus")
                .ToLower();
            
            // Remove common non-specific words for better matching
            var stopWords = new[] { "new", "used", "original", "genuine", "branded", "high quality", "premium" };
            foreach (var word in stopWords)
            {
                cleanedName = cleanedName.Replace(word, "").Trim();
            }
            
            // Strategy 1: Try exact product search first
            var searchQuery = $"{cleanedName} product white background";
            var imageUrls = new List<string>();
            
            // Try first search with strict query
            var requestUrl = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(searchQuery)}&per_page={count * 2}&orientation=square";
            using var response = await client.GetAsync(requestUrl);
            
            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var photos = document.RootElement.GetProperty("photos");
                
                // Filter images that are more product-focused
                for (int i = 0; i < photos.GetArrayLength() && imageUrls.Count < count; i++)
                {
                    var photo = photos[i];
                    var alt = photo.GetProperty("alt").GetString()?.ToLower() ?? "";
                    
                    // Check if image description contains product name keywords
                    var productKeywords = cleanedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var matchCount = productKeywords.Count(keyword => alt.Contains(keyword));
                    
                    // Only add if at least one keyword matches or if we have few results
                    if (matchCount > 0 || imageUrls.Count < 2)
                    {
                        var url = photo.GetProperty("src").GetProperty("medium").GetString();
                        if (url != null && !imageUrls.Contains(url))
                        {
                            imageUrls.Add(url);
                        }
                    }
                }
            }
            
            // Strategy 2: If we don't have enough images, try simpler search
            if (imageUrls.Count < count)
            {
                var simpleQuery = cleanedName; // Just product name
                requestUrl = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(simpleQuery)}&per_page={count}&orientation=square";
                
                using var response2 = await client.GetAsync(requestUrl);
                if (response2.IsSuccessStatusCode)
                {
                    using var document2 = JsonDocument.Parse(await response2.Content.ReadAsStringAsync());
                    var photos2 = document2.RootElement.GetProperty("photos");
                    
                    for (int i = 0; i < photos2.GetArrayLength() && imageUrls.Count < count; i++)
                    {
                        var url = photos2[i].GetProperty("src").GetProperty("medium").GetString();
                        if (url != null && !imageUrls.Contains(url))
                        {
                            imageUrls.Add(url);
                        }
                    }
                }
            }
            
            return Ok(new { 
                images = imageUrls.Take(count).ToArray(), 
                message = imageUrls.Count > 0 ? $"Found {imageUrls.Count} images. Select the best match." : "No images found. Try uploading manually." 
            });
        }
        catch (Exception)
        {
            return Ok(new { images = new string[] { }, message = "Image search unavailable." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> AutoFetchImage(string productName)
    {
        var placeholder = Url.Content("~/Images/product-placeholder.svg") ?? "/Images/product-placeholder.svg";
        if (string.IsNullOrWhiteSpace(productName))
            return BadRequest(new { imageUrl = placeholder, found = false, message = "Enter a product name first." });

        var apiKey = _configuration["ImageSearch:PexelsApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return Ok(new { imageUrl = placeholder, found = false, message = "Image search is not configured. Add a Pexels API key to appsettings.json." });

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", apiKey);
            
            // Clean and improve search query
            var cleanedName = productName.Trim()
                .Replace("&", "and")
                .Replace("+", "plus")
                .ToLower();
            
            // Remove common filler words
            var stopWords = new[] { "new", "used", "original", "genuine", "branded", "high quality", "premium" };
            foreach (var word in stopWords)
            {
                cleanedName = cleanedName.Replace(word, "").Trim();
            }
            
            // Add product context with white background for better product images
            var searchQuery = $"{cleanedName} product white background";
            
            var requestUrl = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(searchQuery)}&per_page=5&orientation=square";
            using var response = await client.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode) 
                return Ok(new { imageUrl = placeholder, found = false, message = "No matching image was found." });

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var photos = document.RootElement.GetProperty("photos");
            
            if (photos.GetArrayLength() == 0) 
                return Ok(new { imageUrl = placeholder, found = false, message = "No matching image was found." });

            // Try to find best matching image by checking alt text
            string? bestImageUrl = null;
            var productKeywords = cleanedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < photos.GetArrayLength(); i++)
            {
                var photo = photos[i];
                var alt = photo.GetProperty("alt").GetString()?.ToLower() ?? "";
                var matchCount = productKeywords.Count(keyword => alt.Contains(keyword));
                
                if (matchCount > 0)
                {
                    bestImageUrl = photo.GetProperty("src").GetProperty("medium").GetString();
                    break;
                }
            }
            
            // If no keyword match found, use first image
            if (bestImageUrl == null)
            {
                bestImageUrl = photos[0].GetProperty("src").GetProperty("medium").GetString();
            }
            
            return Ok(new { 
                imageUrl = bestImageUrl ?? placeholder, 
                found = bestImageUrl is not null, 
                message = bestImageUrl is null ? "No matching image was found." : "Image found. Review and select if appropriate." 
            });
        }
        catch (Exception)
        {
            return Ok(new { imageUrl = placeholder, found = false, message = "Image search is unavailable right now." });
        }
    }

    private async Task ValidateProduct(Product product, int? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(product.ProductName)) ModelState.AddModelError(nameof(product.ProductName), "Product name is required.");
        if (string.IsNullOrWhiteSpace(product.SKU)) ModelState.AddModelError(nameof(product.SKU), "SKU is required.");
        if (product.CostPrice < 0 || product.Price < 0) ModelState.AddModelError(nameof(product.Price), "Prices cannot be negative.");
        if (product.ReorderLevel < 0 || product.ReorderQuantity < 1 || product.LeadTimeDays < 0) ModelState.AddModelError(nameof(product.ReorderLevel), "Enter valid reorder settings.");
        if (product.CategoryId is null) ModelState.AddModelError(nameof(product.CategoryId), "Category is required.");
        else if (!await _context.Categories.AnyAsync(c => c.Id == product.CategoryId)) ModelState.AddModelError(nameof(product.CategoryId), "Choose a valid category.");
        if (product.SupplierId is null) ModelState.AddModelError(nameof(product.SupplierId), "Supplier is required.");
        else if (!await _context.Suppliers.AnyAsync(s => s.Id == product.SupplierId)) ModelState.AddModelError(nameof(product.SupplierId), "Choose a valid supplier.");
        if (!string.IsNullOrWhiteSpace(product.SKU) && await _context.Products.AnyAsync(p => p.SKU == product.SKU && p.Id != currentId))
            ModelState.AddModelError(nameof(product.SKU), "This SKU is already in use.");
    }

    private async Task<string?> CategoryName(int? categoryId) => categoryId is null ? null :
        await _context.Categories.Where(c => c.Id == categoryId).Select(c => c.CategoryName).FirstOrDefaultAsync();

    private async Task LoadLookups()
    {
        ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(), "Id", "CategoryName");
        ViewBag.Suppliers = new SelectList(await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync(), "Id", "SupplierName");
    }

    private string UserName() => HttpContext.Session.GetString("Username") ?? "System";

    private async Task<string?> SaveProductImage(IFormFile? imageUpload, string? selectedImageUrl)
    {
        if (imageUpload is null || imageUpload.Length == 0) return selectedImageUrl;
        if (!imageUpload.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("imageUpload", "Upload a valid image file.");
            return selectedImageUrl;
        }
        if (imageUpload.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError("imageUpload", "Image must be 5 MB or smaller.");
            return selectedImageUrl;
        }

        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsPath);
        var extension = Path.GetExtension(imageUpload.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(uploadsPath, fileName);
        await using var fileStream = System.IO.File.Create(destination);
        await imageUpload.CopyToAsync(fileStream);
        return $"/uploads/products/{fileName}";
    }
}
