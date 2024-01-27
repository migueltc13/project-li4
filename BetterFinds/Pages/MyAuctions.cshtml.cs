using BetterFinds.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **My Auctions** page.
    /// This class is decorated with the Authorize attribute.
    /// </summary>
    [Authorize]
    public class MyAuctionsModel : PageModel
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
        /// Initializes a new instance of the <see cref="MyAuctionsModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        /// <param name="hubContext">The IHubContext instance.</param>
        public MyAuctionsModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            this.configuration = configuration;
            this.hubContext = hubContext;
        }

        /// <summary>
        /// The current sort.
        /// </summary>
        public string CurrentSort { get; set; } = "";

        /// <summary>
        /// The current occurring.
        /// </summary>
        public int CurrentOccurring { get; set; } = 0;

        /// <summary>
        /// The list of auctions.
        /// </summary>
        public List<Dictionary<string, object>>? MyAuctions { get; set; }

        /// <summary>
        /// Shows the client's auctions.
        /// </summary>
        /// <remarks>
        /// This page gets the client's auctions from the database and displays them by
        /// the specified sort order.
        /// To see available sort options, see <see cref="Utils.Auctions.GetAuctions"/>.
        /// </remarks>
        /// <param name="sort">The sort order to use.</param>
        /// returns>A task that represents the action of loading the MyAuctions page.</returns>
        public void OnGet(string sort)
        {
            // Get ClientId
            var clientUtils = new Utils.Client(configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Define default values
            int order = 0;
            bool reversed = false;
            // Get occurring from resquest query string
            if (!int.TryParse(HttpContext.Request.Query["occurring"], out int occurring))
            {
                occurring = 0; // Default value: false
            }

            var auctionsUtils = new Utils.Auctions(configuration, hubContext);

            auctionsUtils.ParseAuctionsOptions(sort, ref order, ref reversed);

            MyAuctions = auctionsUtils.GetAuctions(clientId, order, reversed, occurring == 1);

            CurrentSort = sort;
            CurrentOccurring = occurring;
        }
    }
}