using BetterFinds.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    public class SearchModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public SearchModel(IConfiguration configuration)
        {
            _configuration = configuration;
            SearchResults = new List<Dictionary<string, object>>();
        }
        public List<Dictionary<string, object>> SearchResults { get; set; }
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

            // convert the query to a lowercase string
            query = query.ToString().ToLower();

            // Search for products and retrieve the matching auction page
            // TODO: optional add users profile pages as results
            var auctionsUtils = new Auctions(_configuration);

            List<Dictionary<string, object>>? Auctions = auctionsUtils.GetAuctions(clientId: 0, order: 0, reversed: false, occurring: true);

            // Iterate through the auctions and check if the product name or description contains the query
            for (int i = 0; i < Auctions.Count; i++)
            {
                Dictionary<string, object> auction = Auctions[i];
                string productName = ((string) auction["ProductName"]).ToLower();
                string productDescription = ((string) auction["ProductDescription"]).ToLower();

                Console.WriteLine(productName);
                Console.WriteLine(productDescription);

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
