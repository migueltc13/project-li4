using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Text;
using BetterFinds.Utils;

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
            // Get ClientId from session
            int clientId = 0; 
            try
            {
                clientId = int.Parse(HttpContext.Session.GetString("ClientId") ?? "");
            }
            catch (FormatException)
            {
                // Unable to parse the string to an integer, set default value: 0
                clientId = 0;
            }

            Console.WriteLine($"ClientId: {clientId}"); // TODO: Remove this
            if (clientId == 0)
            {
                Console.WriteLine("ClientId is empty");
                // TODO get ClientId from database with User.Identity.Name
                return;
            }

            var auctionsUtils = new Auctions(_configuration);

            // TODO get order/reverse methods on the frontend
            MyAuctions = auctionsUtils.GetAuctions(clientId: clientId, order: 0, reversed: false);
        }
    }
}
