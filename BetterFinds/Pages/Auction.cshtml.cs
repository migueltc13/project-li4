using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetterFinds.Pages
{
    public class AuctionModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public AuctionModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult OnGet()
        {
            if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
            {
                return NotFound();
            }

            // debug
            Console.WriteLine($"Auction id requested: {auctionId}");

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            SqlConnection con = new SqlConnection(connectionString);

            try
            {
                con.Open();

                string query = "SELECT * FROM Auction WHERE AuctionId = @AuctionId";
                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        DateTime startTime = reader.GetDateTime(reader.GetOrdinal("StartTime"));
                        DateTime endTime = reader.GetDateTime(reader.GetOrdinal("EndTime"));
                        int clientId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                        int productId = reader.GetInt32(reader.GetOrdinal("ProductId"));

                        // Values to be used in the cshtml page
                        ViewData["StartTime"] = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                        ViewData["EndTime"] = endTime.ToString("yyyy-MM-dd HH:mm:ss");

                        reader.Close();

                        // Get seller username (NOTE no exections are being made)
                        string fullName = string.Empty;
                        string username = string.Empty;
                        string queryClient = "SELECT FullName, Username FROM Client WHERE ClientId = @ClientId";
                        SqlCommand cmdClient = new SqlCommand(queryClient, con);
                        cmdClient.Parameters.AddWithValue("@ClientId", clientId);
                        using (SqlDataReader readerClient = cmdClient.ExecuteReader())
                        {
                            if (readerClient.Read())
                            {
                                fullName = readerClient.GetString(readerClient.GetOrdinal("FullName"));
                                username = readerClient.GetString(readerClient.GetOrdinal("Username"));
                            }
                            readerClient.Close();
                        }

                        ViewData["FullName"] = fullName;
                        ViewData["Username"] = username;

                        // Get Product info: name, description and price
                        string productName = string.Empty;
                        string productDesc = string.Empty;
                        int productPrice = 0;
                        string productQuery = "SELECT Name, Description, Price FROM Product WHERE ProductId = @ProductId";
                        SqlCommand cmdProduct = new SqlCommand(productQuery, con);
                        cmdProduct.Parameters.AddWithValue("@ProductId", productId);
                        using (SqlDataReader readerProduct = cmdProduct.ExecuteReader())
                        {
                            if (readerProduct.Read())
                            {
                                productName = readerProduct.GetString(readerProduct.GetOrdinal("Name"));
                                productDesc = readerProduct.GetString(readerProduct.GetOrdinal("Description"));
                                productPrice = readerProduct.GetInt32(readerProduct.GetOrdinal("Price"));
                            }
                            readerProduct.Close();
                        }

                        ViewData["ProductName"] = productName;
                        ViewData["ProductDesc"] = productDesc;
                        ViewData["ProductPrice"] = ((double)productPrice / 100).ToString("0.00");

                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // return RedirectToPage("/auction");
                return Page();
            }
            finally
            {
                con.Close();
            }


            return Page();
        }
    }
}
