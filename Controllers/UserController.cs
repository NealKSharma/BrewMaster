using BrewMaster.Data;
using BrewMaster.Helpers;
using BrewMaster.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace BrewMaster.Controllers
{
    [RoleAuthorize("User")]
    public class UserController : Controller
    {
        private readonly DbHelper _helper;
        private readonly ErrorLogger _errorLogger;

        public UserController(DbHelper helper, ErrorLogger errorLogger)
        {
            _helper = helper;
            _errorLogger = errorLogger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Cart()
        {
            return View();
        }
        public IActionResult Account()
        {
            return View();
        }
    }
}
