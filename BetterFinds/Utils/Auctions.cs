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
            List<Dictionary<string, object>> auctions = new();

            string query = "SELECT A.AuctionId, A.ProductId, A.EndTime, P.Name AS ProductName, P.Description AS ProductDescription, P.Price AS ProductPrice " +
                           "FROM Auction A " +
                           "JOIN Product P ON A.ProductId = P.ProductId";

            if (clientId != 0)
            {
                query += " WHERE A.ClientId = @ClientId";
            }

            if (occurring)
            {
                query += (clientId != 0) ? " AND A.EndTime > GETDATE()" : " WHERE A.EndTime > GETDATE()";
            }

            switch (order)
            {
                case 0:
                    query += " ORDER BY A.EndTime";
                    break;
                case 1:
                    query += " ORDER BY P.Price";
                    break;
                case 2:
                    query += " ORDER BY P.Name";
                    break;
            }

            if (reversed)
            {
                query += " DESC";
            }

            Console.WriteLine($"{query}");

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", clientId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            double productPrice = Convert.ToDouble(reader["ProductPrice"]);
                            string formattedPrice = (productPrice / 100).ToString("0.00");

                            Dictionary<string, object> auctionRow = new Dictionary<string, object>
                            {
                                {"AuctionId", reader["AuctionId"]},
                                {"EndTime", reader["EndTime"]},
                                {"ProductName", reader["ProductName"]},
                                {"ProductDescription", reader["ProductDescription"]},
                                {"ProductPrice", formattedPrice}
                            };

                            auctions.Add(auctionRow);
                        }
                    }
                }
            }
            return auctions;
        }

        public void ParseAuctionsOptions(string sort, ref int order, ref bool reversed)
        {
            // Update order and reversed based on the selected sort option
            switch (sort)
            {
                case "date":
                    order = 0;
                    reversed = false;
                    break;

                case "dateRev":
                    order = 0;
                    reversed = true;
                    break;

                case "price":
                    order = 1;
                    reversed = false;
                    break;

                case "priceRev":
                    order = 1;
                    reversed = true;
                    break;

                case "name":
                    order = 2;
                    reversed = false;
                    break;

                case "nameRev":
                    order = 2;
                    reversed = true;
                    break;

                default:
                    // Handle the case where an invalid or no option is selected, default values are used
                    break;
            }
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
