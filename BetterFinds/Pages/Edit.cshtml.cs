using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    [Authorize]
    public class EditModel : PageModel
    {
        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Description { get; set; } = "";

        [BindProperty]
        public decimal MinimumBid { get; set; } = 0;

        [BindProperty]
        public string? Images { get; set; }

        private readonly IConfiguration _configuration;
        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            // get auction id from query string
            if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
            {
                return NotFound();
            }

            ViewData["AuctionId"] = auctionId;

            // pass message to page via action parameter: success=1
            if (Request.Query.ContainsKey("success"))
            {
                if (Request.Query["success"] == "1")
                    ViewData["Message"] = "Auction updated successfully.";
            }

            // Get clientId
            var clientUtils = new Utils.Client(_configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Check if auction belongs to client
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Auction WHERE AuctionId = @AuctionId AND ClientId = @ClientId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count == 0)
                    {
                        return NotFound();
                    }
                }
                con.Close();
            }

            // Get auction details
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT Product.Name, Product.Description, Auction.MinimumBid, Product.Images FROM Product INNER " 
                    + "JOIN Auction ON Product.AuctionId = Auction.AuctionId WHERE Auction.AuctionId = @AuctionId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ViewData["Name"] = reader.GetString(0);
                            ViewData["Description"] = reader.GetString(1); Description = reader.GetString(1);
                            ViewData["MinimumBid"] = reader.GetDecimal(2);
                            ViewData["Images"] = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        }
                        reader.Close();
                    }
                }
                con.Close();
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            // get auction id from query string
            if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
            {
                return NotFound();
            }

            // Get clientId
            var clientUtils = new Utils.Client(_configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Check if auction belongs to client
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Auction WHERE AuctionId = @AuctionId AND ClientId = @ClientId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    cmd.Parameters.AddWithValue("@ClientId", clientId);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count == 0)
                    {
                        return NotFound();
                    }
                }
                con.Close();
            }

            // Update auction mimimum bid
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "UPDATE Auction SET MinimumBid = @MinimumBid WHERE AuctionId = @AuctionId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@MinimumBid", MinimumBid);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }

            // Get product id
            int productId;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT ProductId FROM Product WHERE AuctionId = @AuctionId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    productId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                con.Close();
            }

            // Update product details
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "UPDATE Product SET Name = @Name, Description = @Description, Images = @Images WHERE ProductId = @ProductId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Name", Title);
                    cmd.Parameters.AddWithValue("@Description", Description);
                    cmd.Parameters.AddWithValue("@Images", (Images != null || Images != "") ? Images : DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }

            // pass message to page via action parameter: id=auctionId&success=1
            return RedirectToPage("edit", new { id = auctionId, success = 1 });
        }
    }
}
