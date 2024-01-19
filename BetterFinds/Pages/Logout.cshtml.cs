using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        Console.WriteLine($"User @{User?.Identity?.Name} logged out.");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("login", new { logout = 1 });
    }
}
