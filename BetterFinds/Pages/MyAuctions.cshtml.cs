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

        public List<Dictionary<string, object>>? MyAuctions { get; set; }

        public void OnGet()
        {
            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Get user's auctions from database
            var auctionsUtils = new Utils.Auctions(_configuration);
            // TODO get order/reverse methods on the frontend
            MyAuctions = auctionsUtils.GetAuctions(clientId: clientId, order: 0, reversed: false);
        }
    }
}
