using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    [Authorize]
    public class MyAuctionsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public MyAuctionsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CurrentSort { get; set; } = "";
        public int CurrentOccurring { get; set; } = 0;

        public List<Dictionary<string, object>>? MyAuctions { get; set; }

        public void OnGet(string sort)
        {
            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Define default values
            int order = 0;
            bool reversed = false;
            // Get occurring from resquest query string
            if (!int.TryParse(HttpContext.Request.Query["occurring"], out int occurring))
            {
                occurring = 0; // Default value: false
            }

            var auctionsUtils = new Utils.Auctions(_configuration);

            auctionsUtils.ParseAuctionsOptions(sort, ref order, ref reversed);

            MyAuctions = auctionsUtils.GetAuctions(clientId, order, reversed, occurring == 1);

            CurrentSort = sort;
            CurrentOccurring = occurring;
        }
    }
}
