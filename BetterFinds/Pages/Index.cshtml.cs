using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace BetterFinds.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;
        public IndexModel(IConfiguration configuration, IHubContext<NotificationHub> hubcontext)
        {
            _configuration = configuration;
            _hubContext = hubcontext;
        }

        public string CurrentSort { get; set; } = "";

        public List<Dictionary<string, object>>? Auctions { get; set; }

        public void OnGet(string sort)
        {
            // Define default values
            int order = 0;
            bool reversed = false;
            bool occurring = true;

            var auctionsUtils = new Utils.Auctions(_configuration, _hubContext);

            auctionsUtils.ParseAuctionsOptions(sort, ref order, ref reversed);

            Auctions = auctionsUtils.GetAuctions(clientId: 0, order, reversed, occurring);

            CurrentSort = sort;
        }
    }
}
