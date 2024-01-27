using BetterFinds.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **My Bids** page.
    /// This class is decorated with the Authorize attribute.
    /// </summary>
    [Authorize]
    public class MyBidsModel : PageModel
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyBidsModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        public MyBidsModel(IConfiguration configuration) =>
            this.configuration = configuration;

        /// <summary>
        /// The list of client bids.
        /// </summary>
        public List<Dictionary<int, List<Dictionary<string, object>>>>? ClientBids { get; set; }

        /// <summary>
        /// Shows the client's bids.
        /// </summary>
        /// <remarks>
        /// This page gets the client's bids from the database and displays them.
        /// </remarks>
        public void OnGet()
        {
            Client clientUtils = new(configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            Bids bidsUtilds = new(configuration);
            ClientBids = bidsUtilds.GetClientBids(clientId);
        }
    }
}
