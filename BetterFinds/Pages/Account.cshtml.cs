using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    [Authorize]
    public class AccountModel : PageModel
    {
        public void OnGet()
        {
            // Get FullName, Username, Email, and OptNewsletter camps
            // TODO option to change account options
        }
    }
}
