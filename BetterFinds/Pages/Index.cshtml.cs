using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CurrentSort { get; set; } = "";

        public List<Dictionary<string, object>>? Auctions { get; set; }

        public void OnGet(string sort)
        {
            // Define default values
            int order = 0;
            bool reversed = false;
            bool occurring = true;

            var auctionsUtils = new Utils.Auctions(_configuration);

            auctionsUtils.ParseAuctionsOptions(sort, ref order, ref reversed);

            Auctions = auctionsUtils.GetAuctions(clientId: 0, order, reversed, occurring);

            CurrentSort = sort;
        }
    }
}
