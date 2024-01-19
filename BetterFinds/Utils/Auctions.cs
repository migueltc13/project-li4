using BetterFinds.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Utils
{
    public class Auctions(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
    {
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
            List<Dictionary<string, object>> auctions = [];

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

            Console.WriteLine($"[Utils/Auctions.cs] (getAuctions) query: {query}");

            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new(connectionString))
            {
                con.Open();

                using SqlCommand cmd = new(query, con);
                cmd.Parameters.AddWithValue("@ClientId", clientId);

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Dictionary<string, object> auctionRow = new()
                            {
                                {"AuctionId", reader["AuctionId"]},
                                {"EndTime", reader["EndTime"]},
                                {"ProductName", reader["ProductName"]},
                                {"ProductDescription", reader["ProductDescription"]},
                                {"ProductPrice", reader["ProductPrice"]}
                            };

                    auctions.Add(auctionRow);
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

        private static readonly List<DateTime> AuctionEndTimes = [];

        public void CreateAuctionsToCheck()
        {
            try
            {
                Console.WriteLine("[Utils/Auctions.cs] Creating AuctionEndTimes list to check in auction background service...");

                DateTime currentTime = DateTime.UtcNow;

                string query = "SELECT AuctionId, EndTime, IsCompleted FROM Auction WHERE EndTime >= @CurrentTime AND IsCheckHasEnded = 0 AND IsCompleted = 0";
                string? connectionString = configuration.GetConnectionString("DefaultConnection");
                using SqlConnection con = new(connectionString);
                con.Open();
                using (SqlCommand cmd = new(query, con))
                {
                    cmd.Parameters.AddWithValue("@CurrentTime", currentTime);
                    using SqlDataReader readerProduct = cmd.ExecuteReader();
                    while (readerProduct.Read())
                    {
                        int auctionId = readerProduct.GetInt32(readerProduct.GetOrdinal("AuctionId"));
                        DateTime endTime = readerProduct.GetDateTime(readerProduct.GetOrdinal("EndTime"));
                        bool isCompleted = readerProduct.GetBoolean(readerProduct.GetOrdinal("IsCompleted"));
                        if (!isCompleted)
                            AuctionEndTimes.Add(endTime);
                    }
                }
                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Utils/Auctions.cs] Error create auctions to check: {ex.Message}");
            }
        }

        public void AddAuction(DateTime endTime) => AuctionEndTimes.Add(endTime);

        public void RemoveAuction(DateTime endTime) => AuctionEndTimes.Remove(endTime);

        public async Task CheckAuctionsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    DateTime currentTime = DateTime.UtcNow;

                    Console.WriteLine($"[Utils/Auctions.cs] AuctionEndTimes.Count: {AuctionEndTimes.Count}");
                    Console.WriteLine($"[Utils/Auctions.cs] CurrentTime: {currentTime}");
                    PrintAuctionsToCheck();

                    for (int i = 0; i < AuctionEndTimes.Count; i++)
                    {
                        Console.WriteLine($"[Utils/Auctions.cs] Checking AuctionEndTimes[{i}]: {AuctionEndTimes[i]}");

                        if (currentTime >= AuctionEndTimes[i])
                        {
                            int auctionId; // Retrieve auctionId from Auction table
                            int sellerId; // Retrieve sellerId from Auction table
                            int buyerId; // Retrieve buyerId from Product table
                            string? connectionString = configuration.GetConnectionString("DefaultConnection");
                            using (SqlConnection con = new(connectionString))
                            {
                                con.Open();
                                string query = "SELECT AuctionId, ClientId FROM Auction WHERE EndTime = @EndTime AND IsCheckHasEnded = 0";
                                using (SqlCommand cmd = new(query, con))
                                {
                                    cmd.Parameters.AddWithValue("@EndTime", AuctionEndTimes[i]);
                                    using SqlDataReader reader = cmd.ExecuteReader();
                                    if (!reader.Read())
                                    {
                                        continue;
                                    }

                                    auctionId = reader.GetInt32(reader.GetOrdinal("AuctionId"));
                                    sellerId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                                    reader.Close();

                                    Console.WriteLine($"[Utils/Auctions.cs] AuctionEndTimes[{i}] has ended at {AuctionEndTimes[i]}, auctionId: {auctionId}, sellerId: {sellerId}");
                                }

                                // Retrive buyerId from Product table
                                query = "SELECT ClientId FROM Product WHERE AuctionId = @AuctionId";
                                using (SqlCommand cmd = new(query, con))
                                {
                                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                                    using SqlDataReader reader = cmd.ExecuteReader();
                                    reader.Read();
                                    buyerId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                                    reader.Close();
                                }

                                con.Close();
                            }

                            // Notifications
                            string message;
                            var notificationUtils = new Notification(configuration);

                            var bidsUtils = new Bids(configuration);
                            List<int> bidders = bidsUtils.GetBiddersFromAuction(auctionId);
                            int notificationCount = 0;

                            // check if buyerId is == 0 meaning there's no buyer
                            if (buyerId == 0)
                            {
                                message = "Your auction has ended and no bids were made.";
                                notificationUtils.CreateNotification(sellerId, auctionId, message);
                                notificationCount = notificationUtils.GetNUnreadMessages(sellerId);
                                // Console.WriteLine($"Seller: {sellerId} - Auction: {auctionId} - notificationCount: {notificationCount}");
                                hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, sellerId).Wait();
                                hubContext.Clients.All.SendAsync("UpdateAuction", sellerId, auctionId).Wait();
                                hubContext.Clients.All.SendAsync("UpdateNotifications", sellerId).Wait();
                            }
                            else
                            {
                                // Notify seller that the auction has ended
                                message = "Your auction has ended, waiting for the buyer to complete the purchase.";
                                notificationUtils.CreateNotification(sellerId, auctionId, message);
                                notificationCount = notificationUtils.GetNUnreadMessages(sellerId);
                                // Console.WriteLine($"Seller: {sellerId} - Auction: {auctionId} - notificationCount: {notificationCount}");
                                hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, sellerId).Wait();
                                hubContext.Clients.All.SendAsync("UpdateAuction", sellerId, auctionId).Wait();
                                hubContext.Clients.All.SendAsync("UpdateNotifications", sellerId).Wait();

                                // Notify bidders except the buyer that the auction has ended
                                message = "Auction has ended.";
                                foreach (int bidder in bidders)
                                {
                                    if (bidder != buyerId)
                                    {
                                        notificationUtils.CreateNotification(bidder, auctionId, message);
                                        notificationCount = notificationUtils.GetNUnreadMessages(bidder);
                                        // Console.WriteLine($"Bidder: {bidder} - Auction: {auctionId} - notificationCount: {notificationCount}");
                                        hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, bidder).Wait();
                                        hubContext.Clients.All.SendAsync("UpdateAuction", bidder, auctionId).Wait();
                                        hubContext.Clients.All.SendAsync("UpdateNotifications", bidder).Wait();
                                    }
                                }

                                // Notify buyer that he won the auction and needs to complete the purchase
                                message = "Auction has ended and you are the winner, please go to the auction page to complete the purchase.";
                                notificationUtils.CreateNotification(buyerId, auctionId, message);
                                notificationCount = notificationUtils.GetNUnreadMessages(buyerId);
                                // Console.WriteLine($"Buyer: {buyerId} - Auction: {auctionId} - notificationCount: {notificationCount}");
                                hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, buyerId).Wait();
                                hubContext.Clients.All.SendAsync("UpdateAuction", buyerId, auctionId).Wait();
                                hubContext.Clients.All.SendAsync("UpdateNotifications", buyerId).Wait();
                            }

                            // Update notification count for the seller
                            notificationCount = notificationUtils.GetNUnreadMessages(sellerId);
                            // Console.WriteLine($"Seller: {sellerId} - Auction: {auctionId} - notificationCount: {notificationCount}");
                            hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, sellerId).Wait();

                            // Mark the auction IsCheckHasEnded as true
                            using (SqlConnection con = new(connectionString))
                            {
                                con.Open();
                                string query = "UPDATE Auction SET IsCheckHasEnded = 1 WHERE AuctionId = @AuctionId";
                                using (SqlCommand cmd = new(query, con))
                                {
                                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);
                                    cmd.ExecuteNonQuery();
                                }
                                con.Close();
                            }

                            // Remove auction from AuctionEndTimes list
                            AuctionEndTimes.RemoveAt(i);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Utils/Auctions.cs] Error checking auctions: {ex.Message}");
            }
        }

        public void PrintAuctionsToCheck()
        {
            Console.WriteLine("AuctionEndTimes list to check in auction background service:");
            foreach (DateTime endTime in AuctionEndTimes)
            {
                Console.WriteLine(endTime);
            }
        }
    }
}
