using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    [Authorize]
    public class CreateModel : PageModel
    {
        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Descrition { get; set; } = "";

        [BindProperty]
        public double Price { get; set; } = 0;

        [BindProperty]
        public decimal MinimumBid { get; set; } = 0;

        [BindProperty]
        public string EndTime { get; set; } = "";

        private readonly IConfiguration _configuration;
        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // For debugging purposes
            Console.WriteLine($"Title: {Title}");
            Console.WriteLine($"Descrition: {Descrition}");
            Console.WriteLine($"Price: {Price}");
            Console.WriteLine($"MinimumBid: {MinimumBid}");
            Console.WriteLine($"EndTime: {EndTime}");

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
            if (Descrition.Length > 2048)
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

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";

            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int ClientId = clientUtils.GetClientId(HttpContext, User);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Get AuctionId from database
                string queryAuctionId = "SELECT MAX(AuctionId) FROM Auction";
                SqlCommand cmdId = new SqlCommand(queryAuctionId, con);
                int AuctionId = Convert.ToInt32(cmdId.ExecuteScalar()) + 1;

                // Get ProductId from database
                string queryProductId = "SELECT MAX(ProductId) FROM Product";
                SqlCommand cmdProductId = new SqlCommand(queryProductId, con);
                int ProductId = Convert.ToInt32(cmdProductId.ExecuteScalar()) + 1;

                // Insert into Auction table
                string queryAuction = "INSERT INTO Auction (AuctionId, StartTime, EndTime, ClientId, ProductId, MinimumBid, IsCompleted) VALUES (@AuctionId, @StartTime, @EndTime, @ClientId, @ProductId, @MinimumBid, 0)";
                using (SqlCommand cmd = new SqlCommand(queryAuction, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", AuctionId);
                    cmd.Parameters.AddWithValue("@StartTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EndTime", DateTime.Parse(EndTime));
                    cmd.Parameters.AddWithValue("@ClientId", ClientId);
                    cmd.Parameters.AddWithValue("@ProductId", ProductId);
                    cmd.Parameters.AddWithValue("@MinimumBid", MinimumBid * 100);
                    // IsCompleted => 0: default value auction is not completed
                    cmd.ExecuteNonQuery();
                }

                // Insert into Product table
                string queryProduct = "INSERT INTO Product (ProductId, Name, Description, Price, AuctionId, ClientId) VALUES (@ProductId, @Name, @Description, @Price, @AuctionId, 0)";
                using (SqlCommand cmdProduct = new SqlCommand(queryProduct, con))
                {
                    cmdProduct.Parameters.AddWithValue("@ProductId", ProductId);
                    cmdProduct.Parameters.AddWithValue("@Name", Title);
                    cmdProduct.Parameters.AddWithValue("@Description", Descrition);
                    cmdProduct.Parameters.AddWithValue("@Price", Price * 100);
                    cmdProduct.Parameters.AddWithValue("@AuctionId", AuctionId);
                    // ClientId => 0: default value no buyer
                    cmdProduct.ExecuteNonQuery();
                }

                con.Close();

                // Add auction to background service to check for ending
                var auctionsUtils = new Utils.Auctions(_configuration);
                auctionsUtils.AddAuction(DateTime.Parse(EndTime));

                // Display success message
                ViewData["Success"] = "Auction created successfully: ";
                ViewData["AuctionId"] = AuctionId;
                ViewData["AuctionTitle"] = Title;
            }
            return Page();
        }
    }
}
