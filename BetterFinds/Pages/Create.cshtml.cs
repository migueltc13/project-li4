using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    [Authorize]
    public class CreateModel : PageModel
    {
        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public decimal Price { get; set; } = 0;

        [BindProperty]
        public decimal MinimumBid { get; set; } = 0;

        [BindProperty]
        public string EndTime { get; set; } = "";

        [BindProperty]
        public string? Images { get; set; }

        private readonly IConfiguration _configuration;

        private readonly IHubContext<NotificationHub> _hubContext;
        public CreateModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            _configuration = configuration;
            _hubContext = hubContext;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // For debugging purposes
            Console.WriteLine($"Title: {Title}");
            Console.WriteLine($"Descrition: {Description}");
            Console.WriteLine($"Price: {Price}");
            Console.WriteLine($"MinimumBid: {MinimumBid}");
            Console.WriteLine($"EndTime: {EndTime}");
            Console.WriteLine($"Images: {Images}");

            // Check price >= 0
            if (Price < 0)
            {
                ModelState.AddModelError(string.Empty, "Starting price must be greater than or equal to 0.");
                return Page();
            }

            // Check minimum bid >= 0
            if (MinimumBid < 0)
            {
                ModelState.AddModelError(string.Empty, "Minimum bid must be greater than or equal to 0.");
                return Page();
            }

            // Check if title is 64 characters or less
            if (Title.Length > 64)
            {
                ModelState.AddModelError(string.Empty, "Title must be 64 characters or less.");
                return Page();
            }

            // Check if description is 2048 characters or less
            if (Description.Length > 2048)
            {
                ModelState.AddModelError(string.Empty, "Description must be 2048 characters or less.");
                return Page();
            }

            // Check if EndTime is greater than current time
            if (DateTime.Parse(EndTime) < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "End time must be greater than current time.");
                return Page();
            }

            // Check if images are valid
            var imagesUtils = new Utils.Images(_configuration);
            string imagesErrorMessage = "";

            if (Images != null && !imagesUtils.IsValidImages(Images: Images, errorMessage: ref imagesErrorMessage))
            {
                ModelState.AddModelError(string.Empty, imagesErrorMessage);
                return Page();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";

            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int ClientId = clientUtils.GetClientId(HttpContext, User);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Get AuctionId from database
                    string queryAuctionId = "SELECT MAX(AuctionId) FROM Auction";
                    SqlCommand cmdId = new SqlCommand(queryAuctionId, con);
                    int AuctionId = cmdId.ExecuteScalar() == DBNull.Value ? 1 : Convert.ToInt32(cmdId.ExecuteScalar()) + 1;

                    // Get ProductId from database
                    string queryProductId = "SELECT MAX(ProductId) FROM Product";
                    SqlCommand cmdProductId = new SqlCommand(queryProductId, con);
                    int ProductId = cmdProductId.ExecuteScalar() == DBNull.Value ? 1 : Convert.ToInt32(cmdProductId.ExecuteScalar()) + 1;

                    // Insert into Auction table
                    string queryAuction = "INSERT INTO Auction (AuctionId, StartTime, EndTime, ClientId, ProductId, MinimumBid, IsCompleted) VALUES (@AuctionId, @StartTime, @EndTime, @ClientId, @ProductId, @MinimumBid, 0)";
                    using (SqlCommand cmd = new SqlCommand(queryAuction, con))
                    {
                        cmd.Parameters.AddWithValue("@AuctionId", AuctionId);
                        cmd.Parameters.AddWithValue("@StartTime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EndTime", DateTime.Parse(EndTime));
                        cmd.Parameters.AddWithValue("@ClientId", ClientId);
                        cmd.Parameters.AddWithValue("@ProductId", ProductId);
                        cmd.Parameters.AddWithValue("@MinimumBid", MinimumBid);
                        // IsCompleted => 0: default value auction is not completed
                        cmd.ExecuteNonQuery();
                    }

                    // Insert into Product table
                    string queryProduct = "INSERT INTO Product (ProductId, Name, Description, Price, AuctionId, ClientId, Images) VALUES (@ProductId, @Name, @Description, @Price, @AuctionId, 0, @Images)";
                    using (SqlCommand cmdProduct = new SqlCommand(queryProduct, con))
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
                    var auctionsUtils = new Utils.Auctions(_configuration, _hubContext);
                    auctionsUtils.AddAuction(DateTime.Parse(EndTime));

                    // Redirect to new auction page
                    return RedirectToPage("/Auction", new { id = AuctionId });
                }
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
