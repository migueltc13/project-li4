using BetterFinds.Utils;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages;

public class MyBidsModel(IConfiguration configuration) : PageModel
{
    public List<Dictionary<int, List<Dictionary<string, object>>>>? ClientBids { get; set; }

    public void OnGet()
    {
        Client clientUtils = new(configuration);
        int clientId = clientUtils.GetClientId(HttpContext, User);

        Bids bidsUtilds = new(configuration);
        ClientBids = bidsUtilds.GetClientBids(clientId);
    }
}
