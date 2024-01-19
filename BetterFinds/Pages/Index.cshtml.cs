using BetterFinds.Hubs;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace BetterFinds.Pages
{
    public class IndexModel(IConfiguration configuration, IHubContext<NotificationHub> hubcontext) : PageModel
    {
        public string CurrentSort { get; set; } = "";

        public List<Dictionary<string, object>>? Auctions { get; set; }

        public void OnGet(string sort)
        {
            // Define default values
            int order = 0;
            bool reversed = false;
            bool occurring = true;

            var auctionsUtils = new Utils.Auctions(configuration, hubcontext);

            auctionsUtils.ParseAuctionsOptions(sort, ref order, ref reversed);

            Auctions = auctionsUtils.GetAuctions(clientId: 0, order, reversed, occurring);

            CurrentSort = sort;
        }
    }
}
