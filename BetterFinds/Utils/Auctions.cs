using Microsoft.Data.SqlClient;

namespace BetterFinds.Utils
{
    public class Auctions
    {
        private readonly IConfiguration _configuration;
        public Auctions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /* Function to show auctions with the following parameters options
         * 
         * int clientId:
         *     - 0    -> All clients auctions
         *     - else -> All clientId auctions
         * 
         * int order: Sorts the auction list
         *     - 0 -> by ending time
         *     - 1 -> by product price
         *     - 2 -> by product name
         *     
         * bool reversed: Reverses the auction list if true
         * 
         * bool ocurring: Shows only auctions that are ocurring if true
         */
        public List<Dictionary<string, object>> GetAuctions(int clientId, int order, bool reversed, bool occurring)
        {
            List<Dictionary<string, object>> auctions = new List<Dictionary<string, object>>();

            // Sorting method
            string query = "SELECT AuctionId, ProductId, EndTime FROM Auction";
            string sortBy = "";

            if (clientId != 0)
                query += $" WHERE ClientId = {clientId}";

            if (occurring)
            {
                if (clientId != 0)
                    query += " AND";
                else
                    query += " WHERE";

                query += " EndTime > GETDATE()";
            }

            switch (order) {
                case 0:
                    query += " ORDER BY EndTime";
                    break;
                case 1:
                    sortBy = "ProductPrice";
                    break;
                case 2:
                    sortBy = "ProductName";
                    break;
            }

            if (reversed)
                query += " DESC";

            Console.WriteLine($"{query}");

            // Retrieve auctions and rescpective products from database
            string? conString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(conString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Get product name and price
                            int productId = reader.GetInt32(reader.GetOrdinal("ProductId"));
                            string queryProduct = "SELECT Name, Description, Price FROM Product WHERE ProductId = @ProductId";
                            using (SqlConnection conProduct = new SqlConnection(conString))
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
                                                {"ProductDescription", readerProduct["Description"]},
                                                {"ProductPrice", formattedPrice}
                                            };
                                            auctions.Add(auctionRow);
                                        }
                                    }
                                }
                                conProduct.Close();
                            }
                        }
                    }
                    con.Close();
                }
            }

            if (order != 0)
            {
                auctions = auctions.OrderBy(auction => auction[sortBy]).ToList();
                if (reversed)
                    auctions.Reverse();
            }

            return auctions;
        }

        private static List<DateTime> auctionEndTimes = new();

        public void CreateAuctionsToCheck()
        {
            try
            {
                Console.WriteLine("Creating auctionEndTimes list to check in auction background service...");

                string query = "SELECT AuctionId, EndTime, IsCompleted FROM Auction";
                string? conString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection con = new SqlConnection(conString))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader readerProduct = cmd.ExecuteReader())
                        {
                            while (readerProduct.Read())
                            {
                                int auctionId = readerProduct.GetInt32(readerProduct.GetOrdinal("AuctionId"));
                                DateTime endTime = readerProduct.GetDateTime(readerProduct.GetOrdinal("EndTime"));
                                bool isCompleted = readerProduct.GetBoolean(readerProduct.GetOrdinal("IsCompleted"));
                                if (!isCompleted)
                                    auctionEndTimes.Add(endTime);
                            }
                        }
                    }
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error create auctions to check: {ex.Message}");
            }
        }

        public void AddAuction(DateTime endTime) => auctionEndTimes.Add(endTime);

        // TODO: Remove auction from auctionEndTimes list when the auction is completed
        public void RemoveAuction(DateTime endTime) => auctionEndTimes.Remove(endTime);

        public async Task CheckAuctionsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    DateTime currentTime = DateTime.UtcNow;

                    for (int i = 0; i < auctionEndTimes.Count; i++)
                    {
                        if (currentTime >= auctionEndTimes[i])
                        {
                            Console.WriteLine($"auctionEndTimes[{i}] has ended");
                            // TODO: If there's an auction buyer
                            // then notify buyer to buy the product (when buyer completes the purchase notify the seller)
                            // else notify the seller
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking auctions: {ex.Message}");
            }
        }
    }
}
