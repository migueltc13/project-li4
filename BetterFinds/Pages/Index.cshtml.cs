using BetterFinds.Utils;
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

        public List<Dictionary<string, object>>? Auctions { get; set; }

        public void OnGet()
        {
            var auctionsUtils = new Auctions(_configuration);

            // TODO get order/reverse methods on the frontend
            Auctions = auctionsUtils.GetAuctions(clientId: 0, order: 0, reversed: false);
        }
    }
}
