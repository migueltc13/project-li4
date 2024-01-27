using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **Error** page.
    /// This class is decorated with the ResponseCache and IgnoreAntiforgeryToken attributes.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        /// <summary>
        /// The request ID.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// A boolean indicating whether or not the request ID should be shown.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        /// <summary>
        /// Shows the error page.
        /// </summary>
        /// <remarks>
        /// This page shows the error page with the request ID, if available.
        /// Used to present the error page with diagnostics when an error occurs.
        /// </remarks>
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}
