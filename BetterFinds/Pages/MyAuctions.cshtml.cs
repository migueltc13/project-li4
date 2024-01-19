using BetterFinds.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace BetterFinds.Pages
{
    [Authorize]
    public class MyAuctionsModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext) : PageModel
    {
        public string CurrentSort { get; set; } = "";
        public int CurrentOccurring { get; set; } = 0;

        public List<Dictionary<string, object>>? MyAuctions { get; set; }

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
