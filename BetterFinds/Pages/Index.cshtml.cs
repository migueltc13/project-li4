using BetterFinds.Hubs;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **Index** page.
    /// </summary>
    public class IndexModel : PageModel
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
         private readonly IConfiguration configuration;

        /// <summary>
        /// The IHubContext instance.
        /// </summary>
        private readonly IHubContext<NotificationHub> hubContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        /// <param name="hubContext">The IHubContext instance.</param>
        public IndexModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            this.configuration = configuration;
            this.hubContext = hubContext;
        }

        /// <summary>
        /// The current sort order of the auctions.
        /// </summary>
        public string CurrentSort { get; set; } = "";

        /// <summary>
        /// The list of auctions to display.
        /// </summary>
        public List<Dictionary<string, object>>? Auctions { get; set; }

        /// <summary>
        /// The action that occurs when the user visits the index page.
        /// </summary>
        /// <remarks>
        /// Displays the list of auctions sorted by the default sort order.
        /// The default sort is by the auction's end date, in ascending order.
        /// To see available sort options, see <see cref="Utils.Auctions.GetAuctions"/>.
        /// </remarks>
        /// <param name="sort">The sort order to use.</param>
        public void OnGet(string sort)
        {
            // Define default values
            int order = 0;
            bool reversed = false;
            bool occurring = true;

            var auctionsUtils = new Utils.Auctions(configuration, hubContext);

            auctionsUtils.ParseAuctionsOptions(sort, ref order, ref reversed);

            Auctions = auctionsUtils.GetAuctions(clientId: 0, order, reversed, occurring);

            CurrentSort = sort;
        }
    }
}