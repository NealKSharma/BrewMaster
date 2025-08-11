using BrewMaster.Data;
using BrewMaster.Models;
using BrewMaster.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace BrewMaster.Controllers
{
    [RoleAuthorize("Admin")]
    public class AdminController(DbHelper helper, ErrorLogger errorLogger) : Controller
    {
        private readonly DbHelper _helper = helper;
        private readonly ErrorLogger _errorLogger = errorLogger;

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var model = new AdminDashboardViewModel
                {
                    TotalUsers = _helper.GetTotalUsersCount(),
                    TotalProducts = _helper.GetTotalProductsCount(),
                    TotalOrders = _helper.GetTotalOrdersCount(),
                    PendingOrdersCount = _helper.GetPendingOrdersCount(),
                    TotalRevenue = _helper.GetTotalRevenue(),

                    LowStockProducts = _helper.GetLowStockProducts(5),
                    OutOfStockProducts = _helper.GetOutOfStockProducts(),

                    RecentOrders = _helper.GetRecentOrders(5),
                    RecentUsers = _helper.GetRecentUsers(5)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["DashboardError"] = "Error loading dashboard data.";
                return View(new AdminDashboardViewModel());
            }
        }

        [HttpGet]
        public IActionResult Products()
        {
            var model = new ProductViewModel
            {
                ProductTable = _helper.GetAllProducts()
            };
            ViewBag.ProductTable = model.ProductTable;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageProducts(ProductViewModel model, string action)
        {
            try
            {
                if (action == "ShowAll")
                {
                    ModelState.Clear();
                    ViewBag.ProductTable = _helper.GetAllProducts();
                    TempData["ProductSuccess"] = "Showing all products.";
                    return View("Products", new ProductViewModel());
                }

                if (model.ImageUpload != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var ext = Path.GetExtension(model.ImageUpload.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                    {
                        TempData["ProductError"] = "Invalid image format. Allowed: jpg, jpeg, png, gif, bmp";
                        ViewBag.ProductTable = _helper.GetAllProducts();
                        return View("Products", model);
                    }

                    if (model.ImageUpload.Length > 5 * 1024 * 1024)
                    {
                        TempData["ProductError"] = "Image size must be under 5MB.";
                        ViewBag.ProductTable = _helper.GetAllProducts();
                        return View("Products", model);
                    }

                    using var ms = new MemoryStream();
                    await model.ImageUpload.CopyToAsync(ms);
                    model.ProductImage = ms.ToArray();
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ProductTable = _helper.GetAllProducts();
                    return View("Products", model);
                }

                if ((action == "Update" || action == "Delete") && model.ProductId <= 0)
                {
                    TempData["ProductError"] = "Please select a product from the table first.";
                    ViewBag.ProductTable = _helper.GetAllProducts();
                    return View("Products", model);
                }

                string currentUser = HttpContext.Session.GetString("UserName") ?? "Unknown";

                switch (action)
                {
                    case "Add":
                        if (_helper.AddProduct(currentUser, model))
                        {
                            TempData["ProductSuccess"] = "Product added successfully.";
                            model = new ProductViewModel();
                            ModelState.Clear();
                        }
                        else
                        {
                            TempData["ProductError"] = "Failed to add product. Please check the fields.";
                        }
                        break;

                    case "Update":
                        if (_helper.UpdateProducts(currentUser, model))
                        {
                            TempData["ProductSuccess"] = "Product updated successfully.";
                            model = new ProductViewModel();
                            ModelState.Clear();
                        }
                        else
                        {
                            TempData["ProductError"] = "Failed to update product. Please verify it exists.";
                        }
                        break;

                    case "Delete":
                        if (_helper.DeleteProducts(currentUser, model))
                        {
                            TempData["ProductSuccess"] = "Product deleted successfully.";
                            model = new ProductViewModel();
                            ModelState.Clear();
                        }
                        else
                        {
                            TempData["ProductError"] = "Failed to delete product. Please verify it exists.";
                        }
                        break;

                    default:
                        TempData["ProductError"] = "Invalid action specified.";
                        break;
                }

                ViewBag.ProductTable = _helper.GetAllProducts();
                return View("Products", model);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["ProductError"] = $"An unexpected error occurred: {ex.Message}";
                ViewBag.ProductTable = _helper.GetAllProducts();
                return View("Products", model);
            }
        }

        [HttpGet]
        public IActionResult Logs(string logType = "ErrorLog", string searchLogId = null)
        {
            try
            {
                var model = new LogViewModel
                {
                    SelectedLogType = logType,
                    SearchLogId = searchLogId,
                    LogsTable = _helper.GetLogs(logType, searchLogId)
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["LogError"] = "An error occurred while fetching logs.";
                return RedirectToAction("Logs", new { logType });
            }
        }

        [HttpPost]
        public IActionResult ShowAll(string logType)
        {
            return RedirectToAction("Logs", new { logType });
        }

        [HttpPost]
        public IActionResult DeleteLog(string logType, string searchLogId)
        {
            try
            {
                if (!string.IsNullOrEmpty(searchLogId) && int.TryParse(searchLogId, out int logId))
                {
                    _helper.DeleteLogById(logType, logId);
                    TempData["LogSuccess"] = "Log deleted successfully.";
                }
                else
                {
                    TempData["LogError"] = "Please enter a valid Log ID to delete.";
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["LogError"] = "Failed to delete the log.";
            }
            return RedirectToAction("Logs", new { logType });
        }

        [HttpPost]
        public IActionResult DeleteAllLogs(string logType)
        {
            try
            {
                _helper.DeleteAllLogs(logType);
                TempData["LogSuccess"] = "All logs deleted successfully.";
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["LogError"] = "Failed to delete all logs.";
            }
            return RedirectToAction("Logs", new { logType });
        }

        [HttpGet]
        public IActionResult Users(string searchUsername = null)
        {
            try
            {
                var model = new UserManagementViewModel
                {
                    SearchUsername = searchUsername,
                    UsersTable = _helper.GetUsers(searchUsername)
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["UserError"] = "An error occurred while fetching users.";
                return View(new UserManagementViewModel());
            }
        }

        [HttpPost]
        public IActionResult ShowAll()
        {
            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult AddUser(UserManagementViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Password))
                {
                    ModelState.AddModelError("Password", "Password is required for new users");
                }

                if (ModelState.IsValid)
                {
                    string currentUser = HttpContext.Session.GetString("UserName") ?? "Unknown";
                    _helper.AddUser(model.Email, model.Username, model.Password,
                                      model.FirstName, model.LastName, model.UserRole, currentUser);
                    TempData["UserSuccess"] = "User added successfully.";
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["UserError"] = string.Join("; ", errors);
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["UserError"] = "Failed to add user. Username or email might already exist.";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult UpdateUser(UserManagementViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Username))
                {
                    ModelState.AddModelError("Username", "Username is required for updates");
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    ModelState.Remove("Password");
                }

                if (ModelState.IsValid)
                {
                    string currentUser = HttpContext.Session.GetString("UserName") ?? "Unknown";
                    bool success = _helper.UpdateUser(model.Username, model.Email,
                                                        model.FirstName, model.LastName,
                                                        model.UserRole, model.Password, currentUser);
                    if (success)
                    {
                        TempData["UserSuccess"] = "User updated successfully.";
                    }
                    else
                    {
                        TempData["UserError"] = "User not found.";
                    }
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["UserError"] = string.Join("; ", errors);
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["UserError"] = "Failed to update user.";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult DeleteUser(string username)
        {
            try
            {
                if (!string.IsNullOrEmpty(username))
                {
                    string currentUser = HttpContext.Session.GetString("UserName") ?? "Unknown";
                    bool success = _helper.DeleteUser(username, currentUser);
                    if (success)
                    {
                        TempData["UserSuccess"] = "User deleted successfully.";
                    }
                    else
                    {
                        TempData["UserError"] = "User not found.";
                    }
                }
                else
                {
                    TempData["UserError"] = "Username is required for deletion.";
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["UserError"] = "Failed to delete user.";
            }

            return RedirectToAction("Users");
        }
    }
}