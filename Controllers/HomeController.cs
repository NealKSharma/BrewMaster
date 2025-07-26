using BrewMaster.Data;
using BrewMaster.Helpers;
using BrewMaster.Models;
using BrewMaster.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BrewMaster.Controllers
{
    [AllowAnonymousOnly]
    public class HomeController : Controller
    {
        private readonly DbHelper _helper;
        private readonly ErrorLogger _errorLogger;

        public HomeController(DbHelper helper, ErrorLogger errorLogger)
        {
            _helper = helper;
            _errorLogger = errorLogger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            var model = new SignUpViewModel
            {
                SecurityQuestions = GetSecurityQuestions()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.SecurityQuestions = GetSecurityQuestions();
                return View(model);
            }

            if (_helper.Exists("Email", model.Email))
                ModelState.AddModelError("Email", "Email already exists. Please use another one.");

            if (_helper.Exists("UserName", model.Username))
                ModelState.AddModelError("Username", "Username already exists. Please choose another one.");

            if (!ModelState.IsValid)
            {
                model.SecurityQuestions = GetSecurityQuestions();
                return View(model);
            }

            try
            {
                string hashedPassword = PasswordHasher.HashPassword(model.Password);
                _helper.InsertUser(model, hashedPassword);
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                model.SecurityQuestions = GetSecurityQuestions();
                return View(model);
            }
        }

        public List<SelectListItem> GetSecurityQuestions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "What was the name of your first pet?", Value = "What was the name of your first pet?" },
                new SelectListItem { Text = "In which city were you born?", Value = "In which city were you born?" },
                new SelectListItem { Text = "What was your mother's maiden name?", Value = "What was your mother's maiden name?" },
                new SelectListItem { Text = "What was the name of your first school?", Value = "What was the name of your first school?" },
                new SelectListItem { Text = "What is your favorite movie?", Value = "What is your favorite movie?" },
                new SelectListItem { Text = "What was the make of your first car?", Value = "What was the make of your first car?" },
                new SelectListItem { Text = "What is your favorite book?", Value = "What is your favorite book?" },
                new SelectListItem { Text = "What was the name of your childhood best friend?", Value = "What was the name of your childhood best friend?" }
            };
        }

        [HttpGet]
        public IActionResult Login()
        {
            var model = new CombinedLoginResetViewModel();
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var combinedModel = new CombinedLoginResetViewModel { Login = model };
                ModelState.AddModelError("", "Invalid login input.");
                return View(combinedModel);
            }

            try
            {
                var loginResult = _helper.ValidateUserLogin(model.Username, model.Password);

                if (!loginResult.IsValid)
                {
                    var combinedModel = new CombinedLoginResetViewModel { Login = model };
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View(combinedModel);
                }

                HttpContext.Session.SetString("UserName", model.Username);
                HttpContext.Session.SetString("UserRole", loginResult.UserRole);
                HttpContext.Session.SetString("UserId", loginResult.UserId.ToString());

                _helper.LogUserLoginAudit(
                    loginResult.UserId,
                    model.Username,
                    loginResult.UserRole,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                if (loginResult.UserRole == "Admin")
                    return RedirectToAction("Index", "Admin");

                if (loginResult.UserRole == "User")
                    return RedirectToAction("Index", "User");

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                var combinedModel = new CombinedLoginResetViewModel { Login = model };
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return View(combinedModel);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GetSecurityQuestionAjax([FromBody] UsernameRequest data)
        {
            try
            {
                string username = data.Username;
                if (string.IsNullOrWhiteSpace(username))
                    return Json(new { success = false, message = "Username is required." });

                var question = _helper.GetSecurityQuestion(username);
                if (string.IsNullOrEmpty(question))
                    return Json(new { success = false, message = "User not found." });

                return Json(new { success = true, question });
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return Json(new { success = false, message = "Unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ResetPasswordViewModel model)
        {
            model.SecurityQuestion ??= _helper.GetSecurityQuestion(model.Username);

            if (!ModelState.IsValid)
            {
                ViewBag.ShowResetPanel = true;
                var combinedModel = new CombinedLoginResetViewModel { Reset = model };
                return View("Login", combinedModel);
            }

            if (!_helper.VerifySecurityAnswer(model.Username, model.SecurityAnswer))
            {
                ModelState.AddModelError("", "Incorrect security answer.");
                model.SecurityQuestion = _helper.GetSecurityQuestion(model.Username);
                ViewBag.ShowResetPanel = true;
                var combinedModel = new CombinedLoginResetViewModel { Reset = model };
                return View("Login", combinedModel);
            }

            try
            {
                string hashed = PasswordHasher.HashPassword(model.Password);
                _helper.UpdatePassword(model.Username, hashed);
                TempData["ResetSuccess"] = "Password reset successfully.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                model.SecurityQuestion = _helper.GetSecurityQuestion(model.Username);
                ViewBag.ShowResetPanel = true;
                var combinedModel = new CombinedLoginResetViewModel { Reset = model };
                return View("Login", combinedModel);
            }
        }
    }
}
