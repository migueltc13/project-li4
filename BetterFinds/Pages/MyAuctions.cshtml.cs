using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Text;

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

        public List<object>? MyAuctions { get; set; }

        public void OnGet()
        {
            List<object> auctions = new List<object>();

            // Get ClientId from session
            string clientId = HttpContext.Session.GetString("ClientId") ?? "";

            Console.WriteLine($"ClientId: {clientId}"); // TODO: Remove this
            if (clientId == "")
            {
                Console.WriteLine("ClientId is empty");
                return;
            }

            // Define the SQL query to select ProductId and EndTime from Auction and order by EndTime
            string query = "SELECT AuctionId, ProductId, EndTime FROM Auction WHERE ClientId = @ClientId ORDER BY EndTime";

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int productId = reader.GetInt32(reader.GetOrdinal("ProductId"));

                            // Get product name and price
                            string queryProduct = "SELECT Name, Price FROM Product WHERE ProductId = @ProductId";

                            using (SqlConnection conProduct = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                            {
                                conProduct.Open();

                                using (SqlCommand cmdProduct = new SqlCommand(queryProduct, conProduct))
                                {
                                    cmdProduct.Parameters.AddWithValue("@ProductId", productId);

                                    using (SqlDataReader readerProduct = cmdProduct.ExecuteReader())
                                    {
                                        if (readerProduct.Read())
                                        {
                                            // Convert and format the price
                                            double productPrice = Convert.ToDouble(readerProduct["Price"]);
                                            string formattedPrice = ((double)productPrice / 100).ToString("0.00");

                                            // Create a dictionary to store each row
                                            Dictionary<string, object> auctionRow = new Dictionary<string, object>
                                            {
                                                {"AuctionId", reader["AuctionId"]},
                                                {"EndTime", reader["EndTime"]},
                                                {"ProductName", readerProduct["Name"]},
                                                {"ProductPrice", formattedPrice}
                                            };
                                            auctions.Add(auctionRow);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // TODO con.Close(); and conProduct.Close()
            MyAuctions = auctions;
        }
    }
}
