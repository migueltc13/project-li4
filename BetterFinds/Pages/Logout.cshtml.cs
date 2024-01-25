using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the logout page.
    /// </summary>
    public class LogoutModel : PageModel
    {
        /// <summary>
        /// Logs the user out and redirects them to the login page.
        /// </summary>
        /// <remarks>
        /// This method is called when the user navigates to the logout page.
        /// It logs the user out and redirects them to the login page with a logout message.
        /// <returns>A asynchronous task that represents the logout operation.</returns>
        public async Task<IActionResult> OnGet()
        {
            Console.WriteLine($"User @{User?.Identity?.Name} logged out.");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("login", new { logout = 1 });
        }
    }
}
