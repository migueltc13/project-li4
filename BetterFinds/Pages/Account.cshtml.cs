using BetterFinds.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{

    /// <summary>
    /// Model for the **Account** page.
    /// </summary>
    public class AccountModel : PageModel
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
         private readonly IConfiguration configuration;

        /// <summary>
        /// The IHubContext instance.
        /// </summary>
        private readonly IHubContext<NotificationHub> hubContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        /// <param name="hubContext">The IHubContext instance.</param>
        public AccountModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            this.configuration = configuration;
            this.hubContext = hubContext;
        }

        /// <summary>
        /// The list of auctions of the account to display.
        /// </summary>
        public List<Dictionary<string, object>>? AuctionsList { get; set; }

        /// <summary>
        /// The action that occurs when the user visits the account page.
        /// </summary>
        /// <remarks>
        /// Checks if the user is the owner of the account, if so add edit option by redirecting to the current user my account page.
        /// <para/>
        /// Checks if the client exists, if not, redirects to the not found error (404) page.
        /// <para/>
        /// Displays the following information about the client:
        /// <list type="bullet">
        ///     <item>Personal Information</item>
        ///     <item>Profile Picture</item>
        ///     <item>List of Auctions</item>
        ///     <item>Number of Bids</item>
        /// </list>
        /// </remarks>
        /// <returns>A task that represents the action of loading the account page.</returns>
        public IActionResult OnGet()
        {
            if (!int.TryParse(HttpContext.Request.Query["id"], out int clientId))
            {
                return NotFound();
            }

            // Get current client id to check if the user can edit the account,
            // if so add edit option by redirecting to the current user my account page
            var clientUtils = new Utils.Client(configuration);
            int CurrentClientId = clientUtils.GetClientId(HttpContext, User);
            if (CurrentClientId == clientId)
                ViewData["Edit"] = true;

            // Check if client exists
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            string query = "SELECT COUNT(*) AS NumberOfClients FROM Client WHERE ClientId = @ClientId";
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    SqlDataReader reader = cmd.ExecuteReader();
                    
                    if (reader.Read() && reader.GetInt32(reader.GetOrdinal("NumberOfClients")) == 0)
                        return NotFound();
                    
                    reader.Close();
                }
                con.Close();
            }

            // Get client info: full name, username and profile picture
            query = "SELECT FullName, Username, ProfilePic FROM Client WHERE ClientId = @ClientId";
            using (SqlConnection con = new(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new(query, con))
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
            var auctionsUtils = new Utils.Auctions(configuration, hubContext);
            AuctionsList = auctionsUtils.GetAuctions(clientId: clientId, order: 0, reversed: false, occurring: false);

            // Get number of bids
            query = "SELECT COUNT(*) AS NumberOfBids FROM Bid WHERE ClientId = @ClientId";
            using (SqlConnection con = new(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new(query, con))
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