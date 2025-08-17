using BrewMaster.Data;
using BrewMaster.Models;
using BrewMaster.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BrewMaster.Controllers
{
    [RoleAuthorize("User")]
    public class UserController(DbHelper helper, ErrorLogger errorLogger) : Controller
    {
        private readonly DbHelper _helper = helper;
        private readonly ErrorLogger _errorLogger = errorLogger;

        public IActionResult Index()
        {
            try
            {
                var model = new HomeViewModel
                {
                    Products = _helper.GetAvailableProducts()
                };

                if (TempData["ToastMessage"] != null)
                {
                    model.ToastMessage = TempData["ToastMessage"]?.ToString();
                    model.ToastType = TempData["ToastType"]?.ToString() ?? "info";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["ToastMessage"] = "An unexpected error occurred while loading products.";
                TempData["ToastType"] = "error";
                return View(new HomeViewModel());
            }
        }

        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            try
            {
                int userId = int.Parse(HttpContext.Session.GetString("UserId")!);
                var (success, message) = _helper.AddToCart(userId, productId);

                if (success)
                {
                    TempData["ToastMessage"] = message;
                    TempData["ToastType"] = "success";
                }

                return Json(new { success, message });
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return Json(new { success = false, message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet]
        public IActionResult ProductImage(int id)
        {
            try
            {
                var imageData = _helper.GetProductImage(id);

                if (imageData != null && imageData.Length > 0)
                {
                    string contentType = GetImageContentType(imageData);
                    return File(imageData, contentType);
                }

                var placeholderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "placeholder.png");
                var placeholderBytes = System.IO.File.ReadAllBytes(placeholderPath);
                return File(placeholderBytes, "image/png");
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                var placeholderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "placeholder.png");
                var placeholderBytes = System.IO.File.ReadAllBytes(placeholderPath);
                return File(placeholderBytes, "image/png");
            }
        }

        private string GetImageContentType(byte[] imageData)
        {
            if (imageData.Length >= 2)
            {
                // Check for JPEG
                if (imageData[0] == 0xFF && imageData[1] == 0xD8)
                    return "image/jpeg";

                // Check for PNG
                if (imageData.Length >= 8 && imageData[0] == 0x89 && imageData[1] == 0x50 &&
                    imageData[2] == 0x4E && imageData[3] == 0x47)
                    return "image/png";

                // Check for GIF
                if (imageData.Length >= 6 && imageData[0] == 0x47 && imageData[1] == 0x49 && imageData[2] == 0x46)
                    return "image/gif";

                // Check for BMP
                if (imageData[0] == 0x42 && imageData[1] == 0x4D)
                    return "image/bmp";
            }

            // Default to JPEG if we can't determine the type
            return "image/jpeg";
        }

        public IActionResult Cart()
        {
            int userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var model = _helper.GetCartItems(userId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int cartId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                    return RemoveItem(cartId);

                _helper.UpdateCartQuantity(cartId, quantity);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int cartId)
        {
            try
            {
                _helper.RemoveCartItem(cartId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout()
        {
            int userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Landing");

            try
            {
                var user = _helper.GetUserDetails(username);
                var cart = _helper.GetCartItems(userId);

                _helper.PlaceOrder(userId, user, cart);

                TempData["ToastMessage"] = "Order placed successfully!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                TempData["ErrorMessage"] = "Checkout failed. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            try
            {
                int userId = int.Parse(HttpContext.Session.GetString("UserId")!);

                int count = _helper.GetCartItemCount(userId);
                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return Json(new { count = 0 });
            }
        }

        [HttpGet]
        public IActionResult Account()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Landing");

            var model = _helper.GetUserDetails(username);
            return View(model);
        }

        [HttpPost]
        public JsonResult UpdateAllFields([FromBody] Dictionary<string, string> updatedFields)
        {
            string? username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Session expired. Please log in again." });

            bool allSuccess = true;

            foreach (var field in updatedFields)
            {
                bool success = _helper.UpdateSingleField(username, field.Key, field.Value);
                if (!success) allSuccess = false;
            }

            return Json(new { success = allSuccess, message = allSuccess ? "All fields updated successfully." : "Some fields could not be updated." });
        }
    }
}
