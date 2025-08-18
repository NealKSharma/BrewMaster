using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrewMaster.Utilities
{
    public class AllowAnonymousOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string? actionName = context.ActionDescriptor.RouteValues["action"];
            if (string.Equals(actionName, "Logout", StringComparison.OrdinalIgnoreCase))
            {
                base.OnActionExecuting(context);
                return;
            }

            var isLoggedIn = !string.IsNullOrEmpty(context.HttpContext.Session.GetString("UserRole"));

            if (isLoggedIn)
            {
                var role = context.HttpContext.Session.GetString("UserRole");

                if (role == "Admin")
                {
                    context.Result = new RedirectToActionResult("Index", "Admin", null);
                }
                else if (role == "User")
                {
                    context.Result = new RedirectToActionResult("Index", "User", null);
                }
                else
                {
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}