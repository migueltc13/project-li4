using BetterFinds.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the Create page.
    /// This class is decorated with the Authorize attribute.
    /// </summary>
    [Authorize]
    public class CreateModel : PageModel
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
            /// Initializes a new instance of the <see cref="CreateModel"/> class.
            /// </summary>
            /// <param name="configuration">The IConfiguration instance.</param>
            /// <param name="hubContext">The IHubContext instance.</param>
            public CreateModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
            {
                this.configuration = configuration;
                this.hubContext = hubContext;
            }

        /// <summary>
        /// The title of the auction.
        /// </summary>
        [BindProperty]
        public string Title { get; set; } = "";

        /// <summary>
        /// The description of the auction.
        /// </summary>
        [BindProperty]
        public string Description { get; set; } = "";

        /// <summary>
        /// The starting price of the auction.
        /// </summary>
        [BindProperty]
        public decimal Price { get; set; } = 0.01M;

        /// <summary>
        /// The minimum bid of the auction.
        /// </summary>
        [BindProperty]
        public decimal MinimumBid { get; set; } = 0.01M;

        /// <summary>
        /// The end time of the auction.
        /// </summary>
        [BindProperty]
        public string EndTime { get; set; } = "";

        /// <summary>
        /// The images of the auction.
        /// </summary>
        [BindProperty]
        public string? Images { get; set; }

        /// <summary>
        /// The OnGet action.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Creates a new auction if all requirements are met.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///     <item>The starting price must be greater than 0.</item>
        ///     <item>The minimum bid must be greater than 0.</item>
        ///     <item>The title must be between 1 and 64 characters long.</item>
        ///     <item>The description must be between 1 and 2048 characters long.</item>
        ///     <item>The end time must be a valid date and time and greater than the current time.</item>
        ///     <item>The images must be valid, if any.</item>
        /// </list>
        /// If all requirements are met, the auction is created and the user is redirected to the new auction page.
        /// Otherwise, the user stays on the page and an error message is displayed.
        /// <para/>
        /// The users on the <see cref="IndexModel"/> or <see cref="SearchModel"/> pages are notified that there's updates.
        /// The auction is added to the background service to check for its ending.
        /// </remarks>
        /// <returns>A task that represents the action of creating a new auction.</returns>
        public IActionResult OnPost()
        {
            // For debugging purposes
            Console.WriteLine($"Title: {Title}");
            Console.WriteLine($"Description: {Description}");
            Console.WriteLine($"Price: {Price}");
            Console.WriteLine($"MinimumBid: {MinimumBid}");
            Console.WriteLine($"EndTime: {EndTime}");
            Console.WriteLine($"Images: {Images}");

            // Check price > 0
            if (Price <= 0)
            {
                ModelState.AddModelError(string.Empty, "Starting price must be greater than 0.");
                return Page();
            }

            // Check minimum bid > 0
            if (MinimumBid <= 0)
            {
                ModelState.AddModelError(string.Empty, "Minimum bid must be greater than 0.");
                return Page();
            }

            // Check if title is at least 1 character long
            if (Title.Length < 1)
            {
                ModelState.AddModelError(string.Empty, "Title must be at least 1 character long.");
                return Page();
            }

            // Check if title is 64 characters or fewer
            if (Title.Length > 64)
            {
                ModelState.AddModelError(string.Empty, "Title must be 64 characters or less.");
                return Page();
            }

            // Check if description is at least 1 character long
            if (Description.Length < 1)
            {
                ModelState.AddModelError(string.Empty, "Description must be at least 1 character long.");
                return Page();
            }

            // Check if description is 2048 characters or fewer
            if (Description.Length > 2048)
            {
                ModelState.AddModelError(string.Empty, "Description must be 2048 characters or less.");
                return Page();
            }

            // Check if EndTime is parseable
            try { DateTime.Parse(EndTime); }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "End time must be a valid date and time.");
                return Page();
            }

            // Check if EndTime is greater than current time
            if (DateTime.Parse(EndTime) < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "End time must be greater than current time.");
                return Page();
            }

            // Check if images are valid
            var imagesUtils = new Utils.Images(configuration);
            string imagesErrorMessage = "";

            if (Images != null && !imagesUtils.IsValidImages(Images: Images, errorMessage: ref imagesErrorMessage))
            {
                ModelState.AddModelError(string.Empty, imagesErrorMessage);
                return Page();
            }

            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            // Get ClientId
            var clientUtils = new Utils.Client(configuration);
            int ClientId = clientUtils.GetClientId(HttpContext, User);

            try
            {
                using SqlConnection con = new(connectionString);
                con.Open();

                // Get AuctionId from database
                string queryAuctionId = "SELECT MAX(AuctionId) FROM Auction";
                SqlCommand cmdId = new(queryAuctionId, con);
                var result = cmdId.ExecuteScalar();
                int AuctionId = result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;

                // Get ProductId from database
                string queryProductId = "SELECT MAX(ProductId) FROM Product";
                SqlCommand cmdProductId = new(queryProductId, con);
                result = cmdProductId.ExecuteScalar();
                int ProductId = result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;

                // Insert into Auction table
                string queryAuction = "INSERT INTO Auction (AuctionId, StartTime, EndTime, ClientId, ProductId, MinimumBid) VALUES (@AuctionId, @StartTime, @EndTime, @ClientId, @ProductId, @MinimumBid)";
                using (SqlCommand cmd = new(queryAuction, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", AuctionId);
                    cmd.Parameters.AddWithValue("@StartTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@EndTime", DateTime.Parse(EndTime));
                    cmd.Parameters.AddWithValue("@ClientId", ClientId);
                    cmd.Parameters.AddWithValue("@ProductId", ProductId);
                    cmd.Parameters.AddWithValue("@MinimumBid", MinimumBid);
                    // IsCompleted => 0: default value auction is not completed
                    cmd.ExecuteNonQuery();
                }

                // Insert into Product table
                string queryProduct = "INSERT INTO Product (ProductId, Name, Description, Price, AuctionId, ClientId, Images) VALUES (@ProductId, @Name, @Description, @Price, @AuctionId, 0, @Images)";
                using (SqlCommand cmdProduct = new(queryProduct, con))
                {
                    cmdProduct.Parameters.AddWithValue("@ProductId", ProductId);
                    cmdProduct.Parameters.AddWithValue("@Name", Title);
                    cmdProduct.Parameters.AddWithValue("@Description", Description);
                    cmdProduct.Parameters.AddWithValue("@Price", Price);
                    cmdProduct.Parameters.AddWithValue("@AuctionId", AuctionId);
                    cmdProduct.Parameters.AddWithValue("@Images", Images != null ? Images : DBNull.Value);
                    // ClientId => 0: default value no buyer
                    cmdProduct.ExecuteNonQuery();
                }

                con.Close();

                // Add auction to background service to check for ending
                var auctionsUtils = new Utils.Auctions(configuration, hubContext);
                auctionsUtils.AddAuction(DateTime.Parse(EndTime));

                Console.WriteLine($"AuctionId: {AuctionId}");
                Console.WriteLine($"Add end time to background service: {DateTime.Parse(EndTime)}");

                auctionsUtils.PrintAuctionsToCheck();

                // Notify users in the /Index or /Search page that there's updates
                hubContext.Clients.All.SendAsync("AuctionCreated").Wait();

                // Redirect to new auction page
                return RedirectToPage("auction", new { id = AuctionId });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ModelState.AddModelError(string.Empty, "Failed to create auction.");
            }

            return Page();
        }
    }
}
