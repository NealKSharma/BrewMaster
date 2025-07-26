using BrewMaster.Data;
using BrewMaster.Helpers;
using BrewMaster.Models;
using BrewMaster.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace BrewMaster.Controllers
{
    [RoleAuthorize("Admin")]
    public class AdminController : Controller
    {
        private readonly DbHelper _helper;
        private readonly ErrorLogger _errorLogger;

        public AdminController(DbHelper helper, ErrorLogger errorLogger)
        {
            _helper = helper;
            _errorLogger = errorLogger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Logs()
        {
            return View();
        }
        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Products()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ManageProducts(ProductViewModel model, string action)
        {
            // Create a log file with timestamp
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), $"debug_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            void WriteLog(string message)
            {
                try
                {
                    System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - {message}\n"); // Use full namespace
                }
                catch { } // Ignore logging errors
            }

            WriteLog($"=== ManageProducts START - Action: '{action}' ===");

            if (!ModelState.IsValid)
            {
                WriteLog("ModelState is invalid");
                foreach (var error in ModelState)
                {
                    WriteLog($"ModelState Error - Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                TempData["Error"] = "Please correct the validation errors.";
                ViewBag.ProductTable = _helper.GetAllProducts();
                return View("Products", model);
            }

            try
            {
                WriteLog("=== IMAGE PROCESSING START ===");

                // Debug image upload
                if (model.ImageUpload != null)
                {
                    WriteLog($"ImageUpload detected:");
                    WriteLog($"  FileName: {model.ImageUpload.FileName}");
                    WriteLog($"  ContentType: {model.ImageUpload.ContentType}");
                    WriteLog($"  Length: {model.ImageUpload.Length}");

                    try
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                        var ext = Path.GetExtension(model.ImageUpload.FileName)?.ToLower();
                        WriteLog($"  Extension: '{ext}'");

                        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                        {
                            WriteLog("  Extension validation failed");
                            TempData["Error"] = "Invalid image format. Allowed: jpg, jpeg, png, gif, bmp";
                            ViewBag.ProductTable = _helper.GetAllProducts();
                            return View("Products", model);
                        }

                        if (model.ImageUpload.Length > 5 * 1024 * 1024)
                        {
                            WriteLog("  Size validation failed");
                            TempData["Error"] = "Image size must be under 5MB.";
                            ViewBag.ProductTable = _helper.GetAllProducts();
                            return View("Products", model);
                        }

                        WriteLog("  Creating MemoryStream...");
                        using var ms = new MemoryStream();

                        WriteLog("  Copying image to MemoryStream...");
                        await model.ImageUpload.CopyToAsync(ms);

                        WriteLog("  Converting to byte array...");
                        model.ProductImage = ms.ToArray();

                        WriteLog($"  Image processed successfully. Byte array length: {model.ProductImage.Length}");
                    }
                    catch (Exception imgEx)
                    {
                        WriteLog($"=== IMAGE PROCESSING EXCEPTION ===");
                        WriteLog($"Exception Type: {imgEx.GetType().Name}");
                        WriteLog($"Message: {imgEx.Message}");
                        WriteLog($"Stack trace: {imgEx.StackTrace}");

                        _errorLogger.LogError(imgEx);
                        TempData["Error"] = $"Image processing failed: {imgEx.Message}";
                        ViewBag.ProductTable = _helper.GetAllProducts();
                        return View("Products", model);
                    }
                }
                else
                {
                    WriteLog("No image upload detected");
                }

                WriteLog("=== IMAGE PROCESSING END ===");

                WriteLog("=== PROCESSING ACTION ===");

                switch (action)
                {
                    case "Add":
                        WriteLog("Processing Add action");
                        WriteLog($"About to call _helper.AddProduct with ProductImage length: {model.ProductImage?.Length ?? 0}");

                        bool addResult = _helper.AddProduct(model);
                        WriteLog($"AddProduct returned: {addResult}");

                        if (addResult)
                        {
                            WriteLog("Product added successfully");
                            TempData["Success"] = "Product added successfully.";
                        }
                        else
                        {
                            WriteLog("Failed to add product");
                            TempData["Error"] = "Failed to add product.";
                        }
                        break;

                    case "Update":
                        WriteLog("Processing Update action");
                        if (_helper.UpdateProducts(model))
                            TempData["Success"] = "Product updated successfully.";
                        else
                            TempData["Error"] = "Failed to update product.";
                        break;

                    case "Delete":
                        WriteLog("Processing Delete action");
                        if (_helper.DeleteProducts(model))
                            TempData["Success"] = "Product deleted.";
                        else
                            TempData["Error"] = "Delete failed.";
                        break;

                    case "Search":
                        WriteLog("Processing Search action");
                        ViewBag.ProductTable = _helper.SearchProductById(model.ProductId);
                        return View("Products", model);

                    case "ShowAll":
                        WriteLog("Processing ShowAll action");
                        ViewBag.ProductTable = _helper.GetAllProducts();
                        return View("Products", model);

                    default:
                        WriteLog($"Unknown action: {action}");
                        TempData["Error"] = "Invalid action.";
                        break;
                }

                WriteLog("Getting all products for display");
                ViewBag.ProductTable = _helper.GetAllProducts();

                WriteLog("About to return view");
                var result = View("Products", model);
                WriteLog("View created successfully");

                return result;
            }
            catch (Exception ex)
            {
                WriteLog($"=== ACTION PROCESSING EXCEPTION ===");
                WriteLog($"Exception Type: {ex.GetType().Name}");
                WriteLog($"Message: {ex.Message}");
                WriteLog($"Stack trace: {ex.StackTrace}");

                _errorLogger.LogError(ex);
                TempData["Error"] = $"An unexpected error occurred: {ex.Message}";

                try
                {
                    ViewBag.ProductTable = _helper.GetAllProducts();
                }
                catch (Exception dbEx)
                {
                    WriteLog($"Failed to get products for error view: {dbEx.Message}");
                    ViewBag.ProductTable = new System.Data.DataTable();
                }

                return View("Products", model);
            }
            finally
            {
                WriteLog("=== ManageProducts END ===");
            }
        }
    }
}