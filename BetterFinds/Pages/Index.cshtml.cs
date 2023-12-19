using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace BetterFinds.Pages
{
    // [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<object>? Auctions { get; set; }

        public void OnGet()
        {
            List<object> auctions = new List<object>();

            // Define the SQL query to select ProductId and EndTime from Auction and order by EndTime
            string query = "SELECT AuctionId, ProductId, EndTime FROM Auction ORDER BY EndTime";

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
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
            Auctions = auctions;
        }
    }
}
