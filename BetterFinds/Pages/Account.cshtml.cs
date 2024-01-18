using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    public class AccountModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AccountModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            _configuration = configuration;
            _hubContext = hubContext;
        }

        public List<Dictionary<string, object>>? AuctionsList { get; set; }

        public IActionResult OnGet()
        {
            if (!int.TryParse(HttpContext.Request.Query["id"], out int clientId))
            {
                return NotFound();
            }

            // Get current client id to check if the user can edit the account,
            // if so add edit option by redirecting to the current user my account page
            var clientUtils = new Utils.Client(_configuration);
            int CurrentClientId = clientUtils.GetClientId(HttpContext, User);
            if (CurrentClientId == clientId)
                ViewData["Edit"] = true;

            // Check if client exists
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            string query = "SELECT COUNT(*) AS NumberOfClients FROM Client WHERE ClientId = @ClientId";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        if (reader.GetInt32(reader.GetOrdinal("NumberOfClients")) == 0)
                            return NotFound();
                    }
                    reader.Close();
                }
                con.Close();
            }

            // Get client info: full name, username and profile picture
            query = "SELECT FullName, Username, ProfilePic FROM Client WHERE ClientId = @ClientId";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        ViewData["FullName"] = reader.GetString(reader.GetOrdinal("FullName"));
                        ViewData["Username"] = reader.GetString(reader.GetOrdinal("Username"));
                        ViewData["ProfilePic"] = reader.IsDBNull(reader.GetOrdinal("ProfilePic")) ? null : reader.GetString(reader.GetOrdinal("ProfilePic"));
                    }
                    reader.Close();
                }
                con.Close();
            }

            // Get list of auctions
            var auctionsUtils = new Utils.Auctions(_configuration, _hubContext);
            AuctionsList = auctionsUtils.GetAuctions(clientId: clientId, order: 0, reversed: false, occurring: false);

            // Get number of bids
            query = "SELECT COUNT(*) AS NumberOfBids FROM Bid WHERE ClientId = @ClientId";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                        ViewData["NumBids"] = reader.GetInt32(reader.GetOrdinal("NumberOfBids"));
                    reader.Close();
                }
                con.Close();
            }

            return Page();
        }
    }
}
