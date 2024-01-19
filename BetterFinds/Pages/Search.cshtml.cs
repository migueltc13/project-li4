using BetterFinds.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace BetterFinds.Pages;

public class SearchModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext) : PageModel
{
    public string Query { get; set; } = "";
    public string CurrentSort { get; set; } = "";
    public int CurrentOccurring { get; set; } = 0;

    public List<Dictionary<string, object>> SearchResults { get; set; } = [];
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
        // Get occurring from resquest query string
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
