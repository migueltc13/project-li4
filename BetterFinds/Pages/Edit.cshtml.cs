using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **Edit Auction** page.
    /// This class is decorated with the Authorize attribute.
    /// </summary>
    [Authorize]
    public class EditModel : PageModel
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        public EditModel(IConfiguration configuration) =>
            this.configuration = configuration;

        /// <summary>
        /// The auction title.
        /// </summary>
        [BindProperty]
        public string Title { get; set; } = "";

        /// <summary>
        /// The auction description.
        /// </summary>
        [BindProperty]
        public string Description { get; set; } = "";

        /// <summary>
        /// The auction minimum bid.
        /// </summary>
        [BindProperty]
        public decimal MinimumBid { get; set; } = 0.01M;

        /// <summary>
        /// The auction images.
        /// </summary>
        [BindProperty]
        public string? Images { get; set; }

        /// <summary>
        /// Shows the edit auction page.
        /// </summary>
        /// <remarks>
        /// Checks if the auction exists and belongs to the client.
        /// If so, displays the auction details.
        /// Otherwise, redirects to the not found error (404) page.
        /// </remarks>
        /// <returns>A task that represents the action of loading the edit auction page.</returns>
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
                ViewData["Message"] = "Auction updated successfully.";
            }

            // Get clientId
            var clientUtils = new Utils.Client(configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Check if auction belongs to client
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Auction WHERE AuctionId = @AuctionId AND ClientId = @ClientId";
                using (SqlCommand cmd = new(query, con))
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
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT Product.Name, Product.Description, Auction.MinimumBid, Product.Images FROM Product INNER "
                    + "JOIN Auction ON Product.AuctionId = Auction.AuctionId WHERE Auction.AuctionId = @AuctionId";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    using SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        ViewData["Name"] = reader.GetString(0);
                        ViewData["Description"] = reader.GetString(1); Description = reader.GetString(1);
                        ViewData["MinimumBid"] = reader.GetDecimal(2);
                        ViewData["Images"] = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    }
                    reader.Close();
                }
                con.Close();
            }

            return Page();
        }

        /// <summary>
        /// Edits the auction if all requirements are met.
        /// </summary>
        /// <remarks>
        /// Checks if the auction exists and belongs to the client.
        /// If so, updates the auction details as long as they meet the validation requirements:
        /// <list type="bullet">
        ///     <item> Minimum bid must be greater than 0.</item>
        ///     <item> Title must be between 1 and 64 characters.</item>
        ///     <item> Description must be between 1 and 2048 characters.</item>
        ///     <item> Images must be valid.</item>
        /// </list>
        /// If the auction details are updated successfully, redirects to the edit auction page with a success message.
        /// Otherwise, redisplays the edit auction page with an error message.
        /// <returns>A task that represents the action of editing the auction.</returns>
        public IActionResult OnPost()
        {
            // get auction id from query string
            if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
            {
                return NotFound();
            }

            // Get clientId
            var clientUtils = new Utils.Client(configuration);
            int clientId = clientUtils.GetClientId(HttpContext, User);

            // Check if auction belongs to client
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Auction WHERE AuctionId = @AuctionId AND ClientId = @ClientId";
                using (SqlCommand cmd = new(query, con))
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

            // Validate data
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

            // Check if images are valid
            var imagesUtils = new Utils.Images(configuration);
            string imagesErrorMessage = "";

            if (Images != null && !imagesUtils.IsValidImages(Images: Images, errorMessage: ref imagesErrorMessage))
            {
                ModelState.AddModelError(string.Empty, imagesErrorMessage);
                return Page();
            }

            // Update auction mimimum bid
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "UPDATE Auction SET MinimumBid = @MinimumBid WHERE AuctionId = @AuctionId";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@MinimumBid", MinimumBid);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }

            // Get product id
            int productId;
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "SELECT ProductId FROM Product WHERE AuctionId = @AuctionId";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                    productId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                con.Close();
            }

            // Update product details
            using (SqlConnection con = new(connectionString))
            {
                con.Open();
                string query = "UPDATE Product SET Name = @Name, Description = @Description, Images = @Images WHERE ProductId = @ProductId";
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@Name", Title);
                    cmd.Parameters.AddWithValue("@Description", Description);
                    cmd.Parameters.AddWithValue("@Images", (Images != null && Images != "") ? Images : DBNull.Value);
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
