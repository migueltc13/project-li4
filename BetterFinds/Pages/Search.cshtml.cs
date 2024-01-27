using BetterFinds.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **Search** page.
    /// </summary>
    public class SearchModel : PageModel
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
        /// Initializes a new instance of the <see cref="SearchModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        /// <param name="hubContext">The IHubContext instance.</param>
        public SearchModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            this.configuration = configuration;
            this.hubContext = hubContext;
        }

        /// <summary>
        /// The query to search for.
        /// </summary>
        public string Query { get; set; } = "";

        /// <summary>
        /// The current sort option.
        /// </summary>
        public string CurrentSort { get; set; } = "";

        /// <summary>
        /// The current occurring option.
        /// </summary>
        public int CurrentOccurring { get; set; } = 0;

        /// <summary>
        /// The list of search results.
        /// </summary>
        public List<Dictionary<string, object>> SearchResults { get; set; } = [];

        /// <summary>
        /// Shows the search results with sort and filter options.
        /// </summary>
        /// <remarks>
        /// This page gets the search results from the database and displays them by
        /// the specified sort order through a case insensitive query string inserted by the user.
        /// The search results are filtered by the query and can be filtered to 
        /// show only auctions that are currently occurring or all auctions.
        /// To see available sort and filter options, see <see cref="Utils.Auctions.GetAuctions"/>.
        /// </remarks>
        /// <returns>A task that represents the action of loading the Search page.</returns>
        public IActionResult OnGet()
        {
            // get the query from the url query string
            if (!Request.Query.TryGetValue("query", out var query))
            {
                return Page();
            }

            // check if the query is empty
            if (string.IsNullOrWhiteSpace(query))
            {
                return Page();
            }

            // Save the query to the model
            Query = query.ToString();

            // convert the query to a lowercase string
            query = query.ToString().ToLower();

            // Define default values
            int order = 0;
            bool reversed = false;
            // Get occurring from request query string
            if (!int.TryParse(HttpContext.Request.Query["occurring"], out int occurring))
            {
                occurring = 1; // Default value: true
            }

            var auctionsUtils = new Utils.Auctions(configuration, hubContext);

            // Get string sort from url
            if (!Request.Query.TryGetValue("sort", out var sortVar))
            {
                sortVar = "date"; // Default value: date
            }
            string sort = sortVar.ToString();

            auctionsUtils.ParseAuctionsOptions(sort, ref order, ref reversed);

            var Auctions = auctionsUtils.GetAuctions(clientId: 0, order, reversed, occurring == 1);

            CurrentSort = sort;
            CurrentOccurring = occurring;

            // Iterate through the auctions and check if the product name or description contains the query
            for (int i = 0; i < Auctions.Count; i++)
            {
                Dictionary<string, object> auction = Auctions[i];
                string productName = ((string)auction["ProductName"]).ToLower();
                string productDescription = ((string)auction["ProductDescription"]).ToLower();

                // Console.WriteLine(productName);
                // Console.WriteLine(productDescription);

                if (productName.ToString().Contains(value: query!))
                {
                    SearchResults.Add(auction);
                }
                else if (productDescription.ToString().Contains(value: query!))
                {
                    SearchResults.Add(auction);
                }
            }

            return Page();
        }
    }
}
