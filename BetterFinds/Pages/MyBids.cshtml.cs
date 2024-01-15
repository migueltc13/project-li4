using BetterFinds.Utils;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    public class MyBidsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public MyBidsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Dictionary<int, List<Dictionary<string, object>>>>? ClientBids { get; set; }

        public void OnGet()
        {
            Client clientUtils = new Client(_configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            Bids bidsUtilds = new Bids(_configuration);
            ClientBids = bidsUtilds.GetClientBids(clientId);
        }
    }
}
