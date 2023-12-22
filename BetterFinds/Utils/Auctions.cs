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

        /* Function to show auctions with the following parameters
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
         */
        public List<Dictionary<string, object>> GetAuctions(int clientId, int order, bool reversed)
        {
            List<Dictionary<string, object>> auctions = new List<Dictionary<string, object>>();

            // Sorting method
            string query = "SELECT AuctionId, ProductId, EndTime FROM Auction";
            string sortBy = "";

            if (clientId != 0)
                query += $" WHERE ClientId = {clientId}";

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
                            string queryProduct = "SELECT Name, Price FROM Product WHERE ProductId = @ProductId";
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
    }
}
