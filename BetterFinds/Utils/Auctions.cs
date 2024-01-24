using BetterFinds.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Utils
{
    /// <summary>
    /// Provides utility functions for handling auctions-related operations.
    /// </summary>
    public class Auctions
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// The IHubContext instance.
        /// </summary>
        private readonly IHubContext<NotificationHub> hubContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Auctions"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        /// <param name="hubContext">The IHubContext instance.</param>
        public Auctions(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            this.configuration = configuration;
            this.hubContext = hubContext;
        }

        /// <summary>
        /// Returns a list of auctions based on the given parameters.
        /// </summary>
        /// <remarks>
        /// Referenced by:
        /// <list type="bullet">
        ///     <item><see cref="Pages.AccountModel"/></item>
        ///     <item><see cref="Pages.IndexModel"/></item>
        ///     <item><see cref="Pages.MyAuctionsModel"/></item>
        ///     <item><see cref="Pages.SearchModel"/></item>
        /// </list>
        /// The parameter <paramref name="clientId"/> retrieves auctions with the following options:
        /// <list type="bullet">
        ///     <item>0: all clients</item>
        ///     <item>_: specific client id</item>
        /// </list>
        /// The paramater <paramref name="order"/> sorts the auction list, the options are:
        /// <list type="bullet">
        ///     <item>0: by ending time</item>
        ///     <item>1: by product price</item>
        ///     <item>2: by product name</item>
        ///     <item>_: by ending time</item>
        /// </list>
        /// Returned dictionary format:
        /// <code lang="json">
        /// {
        ///     "AuctionId": int,
        ///     "EndTime": DateTime,
        ///     "ProductName": string,
        ///     "ProductDescription": string,
        ///     "ProductPrice": decimal
        /// }
        /// </code>
        /// </remarks>
        /// <param name="clientId">The client id to retrieve auctions from. If 0, all auctions are retrieved.</param>
        /// <param name="order">The order to sort the auctions by.</param>
        /// <param name="reversed">Whether to reverse the order of the auctions.</param>
        /// <param name="occurring">Whether to retrieve only auctions that are occurring.</param>
        /// <returns>A list of auctions based on the given parameters.</returns>
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

            query += order switch
            {
                0 => " ORDER BY A.EndTime",
                1 => " ORDER BY P.Price",
                2 => " ORDER BY P.Name",
                _ => " ORDER BY A.EndTime",
            };

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

        /// <summary>
        /// Parses the given sort option and updates the order and reversed parameters accordingly.
        /// </summary>
        /// <remarks>
        /// This method is used to parse the sort option selected by the user in pages that have the
        /// option to sort the auctions.
        /// <para/>
        /// Referenced by:
        /// <list type="bullet">
        ///     <item><see cref="Pages.IndexModel"/></item>
        ///     <item><see cref="Pages.MyAuctionsModel"/></item>
        ///     <item><see cref="Pages.SearchModel"/></item>
        /// </list>
        /// The parameter <paramref name="sort"/> can be one of the following:
        /// <list type="bullet">
        ///     <item>date or dateRev</item>
        ///     <item>price or priceRev</item>
        ///     <item>name or nameRev</item>
        /// </list>
        /// The parameter <paramref name="order"/> is updated with the order to sort the auctions by.
        /// <para/>
        /// The parameter <paramref name="reversed"/> is updated with whether to reverse the order of
        /// the auctions.
        /// </remarks>
        /// <param name="sort">The sort option to parse.</param>
        /// <param name="order">The order returned by the method.</param>
        /// <param name="reversed">The reversed returned by the method.</param>
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
                    // Handle the case where an invalid or no option is selected, default values are used.
                    // This values are defined before calling this method.
                    break;
            }
        }

        /// <summary>
        /// The <c>AuctionEndTimes</c> list contains all the auction end times that need to be checked
        /// by the auction background service, initialized at the start of the application.
        /// </summary>
        /// <permission cref="AuctionEndTimes">This property can only be set from within the class.</permission>
        /// <remarks>
        /// This list is populated by the <see cref="CreateAuctionsToCheck"/> method.
        /// </remarks>
        private static readonly List<DateTime> AuctionEndTimes = [];

        /// <summary>
        /// Creates a list of auctions that need to be checked by the auction background service.
        /// </summary>
        /// <remarks>
        /// This method is called at the start of the application to populate the <see cref="AuctionEndTimes"/> list.
        /// <para/>
        /// It selects all the auctions that have not ended yet and are not completed (or checked as ended),
        /// and adds their end time to the <see cref="AuctionEndTimes"/> list.
        /// <remarks>
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
                        DateTime endTime = readerProduct.GetDateTime(readerProduct.GetOrdinal("EndTime"));
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

        /// <summary>
        /// Adds an auction end time to the <see cref="AuctionEndTimes"/> list.
        /// </summary>
        /// <remarks>
        /// Referenced by:
        /// <list type="bullet">
        ///     <item><see cref="Pages.CreateModel"/> when an auction is created.</item>
        ///     <item><see cref="Pages.AuctionModel"/> when an auction is extended by the seller.</item>
        /// </list>
        /// <param name="endTime">The auction end time to add.</param>
        public void AddAuction(DateTime endTime) => AuctionEndTimes.Add(endTime);

        /// <summary>
        /// Removes an auction end time from the <see cref="AuctionEndTimes"/> list.
        /// </summary>
        /// <remarks>
        /// Referenced by:
        /// <list type="bullet">
        ///    <item><see cref="Pages.AuctionModel"/> when an auction is completed (winner buys the product).</item>
        ///    <item><see cref="Pages.AuctionModel"/> when an auction is extended by the seller.</item>
        /// </list>
        /// </remarks>
        /// <param name="endTime">The auction end time to remove.</param>
        public void RemoveAuction(DateTime endTime) => AuctionEndTimes.Remove(endTime);

        /// <summary>
        /// Checks for auctions that ended asynchronously, notifies users and updates the database when ended auctions are found.
        /// </summary>
        /// <remarks>
        /// Referenced by:
        /// <list type="bullet">
        ///     <item><see cref="Services.AuctionBackgroundService"/> to check for ended auctions at the start of each minute.</item>
        /// </list>
        /// When an auction ends, the following notifications are created:
        /// <list type="bullet">
        ///     <item>The seller is notified that the auction has ended.</item>
        ///     <item>The bidders (except the buyer) are notified that the auction has ended, if any.</item>
        ///     <item>The buyer is notified that he won the auction and needs to complete the purchase, if any.</item>
        /// </list>
        /// The respective notification count is updated for each user, and respective auction page is refreshed for all users
        /// that are currently viewing the auction page.
        /// <para/>
        /// The auction is marked as checked in the database, to prevent it from being checked again, and it's removed from
        /// the <see cref="AuctionEndTimes"/> list.
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CheckAuctionsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    DateTime currentTime = DateTime.UtcNow;

                    Console.WriteLine($"[Utils/Auctions.cs] AuctionEndTimes.Count: {AuctionEndTimes.Count}, currentTime: {currentTime}");
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
                                notificationCount = notificationUtils.GetUnreadNotificationsCount(sellerId);
                                // Console.WriteLine($"Seller: {sellerId} - Auction: {auctionId} - notificationCount: {notificationCount}");
                                hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, sellerId).Wait();
                                hubContext.Clients.All.SendAsync("RefreshAuction", auctionId).Wait();
                            }
                            else
                            {
                                // Notify seller that the auction has ended
                                message = "Your auction has ended, waiting for the buyer to complete the purchase.";
                                notificationUtils.CreateNotification(sellerId, auctionId, message);
                                notificationCount = notificationUtils.GetUnreadNotificationsCount(sellerId);
                                // Console.WriteLine($"Seller: {sellerId} - Auction: {auctionId} - notificationCount: {notificationCount}");
                                hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, sellerId).Wait();
                                hubContext.Clients.All.SendAsync("RefreshAuction", auctionId).Wait();

                                // Notify bidders except the buyer that the auction has ended
                                message = "Auction has ended.";
                                foreach (int bidder in bidders)
                                {
                                    if (bidder != buyerId)
                                    {
                                        notificationUtils.CreateNotification(bidder, auctionId, message);
                                        notificationCount = notificationUtils.GetUnreadNotificationsCount(bidder);
                                        // Console.WriteLine($"Bidder: {bidder} - Auction: {auctionId} - notificationCount: {notificationCount}");
                                        hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, bidder).Wait();
                                        hubContext.Clients.All.SendAsync("RefreshAuction", auctionId).Wait();
                                    }
                                }

                                // Notify buyer that he won the auction and needs to complete the purchase
                                message = "Auction has ended and you are the winner, please go to the auction page to complete the purchase.";
                                notificationUtils.CreateNotification(buyerId, auctionId, message);
                                notificationCount = notificationUtils.GetUnreadNotificationsCount(buyerId);
                                // Console.WriteLine($"Buyer: {buyerId} - Auction: {auctionId} - notificationCount: {notificationCount}");
                                hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, buyerId).Wait();
                                hubContext.Clients.All.SendAsync("RefreshAuction", auctionId).Wait();
                            }

                            // Update notification count for the seller
                            notificationCount = notificationUtils.GetUnreadNotificationsCount(sellerId);
                            // Console.WriteLine($"Seller: {sellerId} - Auction: {auctionId} - notificationCount: {notificationCount}");
                            hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, sellerId).Wait();

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

        /// <summary>
        /// Prints the <see cref="AuctionEndTimes"/> list to the console.
        /// </summary>
        /// <remarks>
        /// Used for debugging purposes.
        /// <para/>
        /// Referenced by:
        /// <list type="bullet">
        ///    <item><see cref="CreateAuctionsToCheck"/> to print the list at the start of the application.</item>
        ///    <item><see cref="Pages.CreateModel"/> when an auction is created.</item>
        /// </list>
        /// </remarks>
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
