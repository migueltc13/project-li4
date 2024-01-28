using BetterFinds.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    /// <summary>
    /// Model for the **Auction** page.
    /// </summary>
    public class AuctionModel : PageModel
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
        /// Initializes a new instance of the <see cref="AuctionModel"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        /// <param name="hubContext">The IHubContext instance.</param>
        public AuctionModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            this.configuration = configuration;
            this.hubContext = hubContext;
        }

        /// <summary>
        /// The bid amount, if any.
        /// </summary>
        [BindProperty]
        public decimal? BidAmount { get; set; }

        /// <summary>
        /// The payment method, if any.
        /// </summary>
        [BindProperty]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// The early sell or terminate request, if any.
        /// </summary>
        [BindProperty]
        public string? EarlySell { get; set; }

        /// <summary>
        /// The extended end time, if any.
        /// </summary>
        [BindProperty]
        public DateTime? ExtendedEndTime { get; set; }

        /// <summary>
        /// The on post method, triggered when the user submits a form on the page.
        /// </summary>
        /// <remarks>
        /// Similar the <see cref="OnGet"/> method, this method retrieves the auction id through the request query string.
        /// <para/>
        /// Forms that can be submitted, varying on the auction state and current user role:
        /// <list cref="bullet"/>
        ///     <item cref="BidAmount">A new bid submitted by an user.</item>
        ///     <item cref="PaymentMethod">The payment method made by an user.</item>
        ///     <item cref="EarlySell">The request of a early sell/termination of the auction by the seller.</item>
        ///     <item cref="ExtendedEndTime">The extension datetime of the auction requested by the seller, if this ended with no bids.</item>
        /// </list>
        /// **Requirements**:
        /// <para/>
        /// As a requirement to submit a bid, the user must be logged in and the auction must not have ended.
        /// To a bid to be valid, the bid amount must be greater or the current price plus the minimum bid.
        /// Besides that, the user cannot bid on his own auction and cannot bid if he's already the highest bidder.
        /// <para/>
        /// To complete the payment, the user must be logged in and the auction must have ended.
        /// Only the highest bidder can complete the payment.
        /// Payment options: Credit Card, PayPal, Apple Pay or Crypto Currency.
        /// <para/>
        /// Only the seller of the auction can perform an early sell/termination of the auction.
        /// This action depends on the number of bids, being only displayed if the auction hasn't ended.
        /// If a auction has no bids, it's presented the option to terminate the auction,
        /// otherwise it's presented the option to early sell the auction.
        /// <para/>
        /// Finally, when an auction ends and there's no bids, the seller can extend the auction.
        /// This datetime inputted must be greater than the current time, if failed to do so, an error message is displayed.
        /// <para/>
        /// **Notifications**:
        /// <para/>
        /// When a user submits a valid bid, notifications are sent to all bidders except the current one,
        /// and the seller is also notified that a new bid has been placed on his auction.
        /// The bidders are determined by the bidders group created with <see cref="Utils.Bids.CreateBidderGroupAsync"/> and
        /// when a new bid is placed, the bidder is added to the group with <see cref="Utils.Bids.AddBidderToBidderGroupAsync"/>.
        /// <para/>
        /// Similar to the notifications described above, when the auction ends, the relevant bidders are notified that the auction has ended,
        /// and the winner is notified that he has won the auction and needs to complete the payment.
        /// If the seller didn't request an early sell/termination of the auction, he's also notified that his auction has ended.
        /// <para/>
        /// When the buyer completes the payment, the seller is notified that the auction has been sold.
        /// <para/>
        /// No notifications are sent when the seller requests an auction extension as there's no bidders as a requirement to this action.
        /// <para/>
        /// **Real-time updates**:
        /// <para/>
        /// When a bid is placed, the following fields are updated in real-time using SignalR:
        /// <list cref="bullet"/>
        ///     <item>Current price</item>
        ///     <item>Buyer info</item>
        ///     <item>Bid history</item>
        ///     <item>The bid amount placeholder</item>
        /// </list>
        /// Note: The bid amount placeholder is only updated if the current value is lower than the new placeholder.
        /// This prevents the placeholder from being updated when the user has a bid ready to be submitted.
        /// <para/>
        /// Upon the conclusion of the auction, the page is refreshed for all clients located on that page.
        /// </remarks>
        /// <returns>A task that represents the auction on post operation.</returns>
        public IActionResult OnPost()
        {
            if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
            {
                return NotFound();
            }

            // Get current ClientId
            var clientUtils = new Utils.Client(configuration);
            int ClientId = clientUtils.GetClientId(HttpContext, User);

            // Define connection string
            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            // Check if this post is a bid or a buy by checking if the payment method is null
            if (PaymentMethod != null)
            {
                // step 0.1 - retrieve auction info from database
                DateTime _EndTime;
                int _SellerId;
                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "SELECT EndTime, ClientId FROM Auction WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            _EndTime = reader.GetDateTime(reader.GetOrdinal("EndTime"));
                            _SellerId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }
                    con.Close();
                }

                // step 0.2 - retrieve buyer info from database
                int _BuyerId;
                string _BuyerUsername;
                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "SELECT ClientId, Username FROM Client WHERE ClientId = @ClientId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ClientId", ClientId);

                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            _BuyerId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                            _BuyerUsername = reader.GetString(reader.GetOrdinal("Username"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }
                    con.Close();
                }

                // step 1 - check if auction has ended
                if (_EndTime >= DateTime.UtcNow)
                {
                    ModelState.AddModelError(string.Empty, "This auction has not ended yet.");
                    return OnGet();
                }

                // step 2 - check if there's a buyer (if there's any bids)
                if (_BuyerId == 0)
                {
                    ModelState.AddModelError(string.Empty, "There are no bids on this auction.");
                    return OnGet();
                }

                // step 3 - check if current user is the highest bidder
                if (ClientId != _BuyerId)
                {
                    ModelState.AddModelError(string.Empty, "You are not the highest bidder.");
                    return OnGet();
                }

                // step 4 - add payment method to Auction table and mark auction IsCompleted as true
                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "UPDATE Auction SET PaymentMethod = @PaymentMethod, IsCompleted = 1 WHERE AuctionId = @AuctionId";
                    using SqlCommand cmd = new(query, con);
                    cmd.Parameters.AddWithValue("@PaymentMethod", PaymentMethod);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                // step 5 - remove auction from AuctionEndTimes list
                var auctionsUtils = new Utils.Auctions(configuration, hubContext);
                auctionsUtils.RemoveAuction(_EndTime);

                // step 6 - notify all other bidders that the auction has ended
                string message = $"The auction has ended and the product has been sold to @{_BuyerUsername}.";
                var bidsUtils = new Utils.Bids(configuration);
                List<int> bidders = bidsUtils.GetBiddersFromAuction(auctionId);

                var notificationUtils = new Utils.Notification(configuration);
                foreach (int bidder in bidders)
                {
                    if (bidder != ClientId)
                        notificationUtils.CreateNotification(bidder, auctionId, message);
                }

                // step 7 - notify the buyer that he has won the auction
                message = $"You have won the auction! The payment was made using {PaymentMethod}.";
                notificationUtils.CreateNotification(ClientId, auctionId, message);

                // step 8 - notify the seller that the auction has been sold
                message = $"Your auction has ended and the product has been sold to @{_BuyerUsername}.";
                notificationUtils.CreateNotification(_SellerId, auctionId, message);

                // Refresh notifications count for all bidders (including the buyer)
                // calculate number of unread messages for each client that had a bid on the auction
                int notificationCount;
                foreach (int bidder in bidders)
                {
                    notificationCount = notificationUtils.GetUnreadNotificationsCount(bidder);
                    hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, bidder).Wait();
                }

                // calculate number of unread messages for the seller
                notificationCount = notificationUtils.GetUnreadNotificationsCount(_SellerId);
                hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, _SellerId).Wait();

                return OnGet();
            }

            // Check if seller requested an early sell
            if (EarlySell != null && EarlySell == "true")
            {
                Console.WriteLine("Early sell requested");

                // check if current user is the seller
                int SellerIdEarlySell = 0;
                DateTime EndTimeEarlySell = DateTime.UtcNow;
                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "SELECT ClientId, EndTime FROM Auction WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            SellerIdEarlySell = reader.GetInt32(reader.GetOrdinal("ClientId"));
                            EndTimeEarlySell = reader.GetDateTime(reader.GetOrdinal("EndTime"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }
                    con.Close();
                }

                if (ClientId != SellerIdEarlySell)
                {
                    ModelState.AddModelError(string.Empty, "You are not the seller.");
                    return OnGet();
                }

                // Check if there's a buyer (if there's any bids)
                int BuyerIdEarlySell = 0;
                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "SELECT ClientId FROM Product WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            BuyerIdEarlySell = reader.GetInt32(reader.GetOrdinal("ClientId"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }
                    con.Close();
                }

                if (BuyerIdEarlySell == 0)
                {
                    // If there's no buyer, mark the auction as completed
                    using SqlConnection con = new(connectionString);
                    con.Open();
                    string query = "UPDATE Auction SET IsCompleted = 1 WHERE AuctionId = @AuctionId";
                    using SqlCommand cmd = new(query, con);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                // Mark the auction IsCheckHasEnded as true
                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "UPDATE Auction SET IsCheckHasEnded = 1 WHERE AuctionId = @AuctionId";
                    using SqlCommand cmd = new(query, con);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                // Remove auction from AuctionEndTimes list
                var auctionsUtils = new Utils.Auctions(configuration, hubContext);
                auctionsUtils.RemoveAuction(EndTimeEarlySell);

                // Update auction end time
                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "UPDATE Auction SET EndTime = @EndTime WHERE AuctionId = @AuctionId";
                    using SqlCommand cmd = new(query, con);
                    cmd.Parameters.AddWithValue("@EndTime", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                // Notify all bidders (except the buyer) that the auction has ended
                string message = "The auction has ended.";

                var bidsUtils = new Utils.Bids(configuration);
                List<int> bidders = bidsUtils.GetBiddersFromAuction(auctionId);

                var notificationUtils = new Utils.Notification(configuration);

                int notificationCount;
                foreach (int bidder in bidders)
                {
                    if (bidder != BuyerIdEarlySell)
                    {
                        notificationUtils.CreateNotification(bidder, auctionId, message);
                        notificationCount = notificationUtils.GetUnreadNotificationsCount(bidder);
                        hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, bidder).Wait();
                    }
                }

                // Notify the buyer that the auction has ended and that he had won the auction
                message = "The seller terminated the auction and you have won! Please go to the auction page to complete the payment.";
                notificationUtils.CreateNotification(BuyerIdEarlySell, auctionId, message);
                notificationCount = notificationUtils.GetUnreadNotificationsCount(BuyerIdEarlySell);
                hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, BuyerIdEarlySell).Wait();

                // Refresh auction page for all clients located that page
                hubContext.Clients.All.SendAsync("RefreshAuction", auctionId).Wait();

                return OnGet();
            }

            Console.WriteLine($"ExtendedEndTime: {ExtendedEndTime}");

            // Check if seller requested to extend the auction
            if (ExtendedEndTime != null)
            {
                // Check if current user is the seller
                // Check if auction has ended
                // Check if there's no buyer
                // Check if the auction is not set as completed (which means that the seller already requested the auction to end)
                int sellerIdExtend = 0;
                DateTime endTimeExtend = DateTime.UtcNow;
                bool isCompletedExtend = false;
                int buyerIdExtend = 0;

                using (SqlConnection con = new(connectionString))
                {
                    string query = "SELECT ClientId, EndTime, IsCompleted FROM Auction WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        con.Open();
                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            sellerIdExtend = reader.GetInt32(reader.GetOrdinal("ClientId"));
                            endTimeExtend = reader.GetDateTime(reader.GetOrdinal("EndTime"));
                            isCompletedExtend = reader.GetBoolean(reader.GetOrdinal("IsCompleted"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }

                    // Check if current user is the seller
                    if (ClientId != sellerIdExtend)
                    {
                        ModelState.AddModelError(string.Empty, "You are not the seller.");
                        return OnGet();
                    }

                    // Check if auction has ended
                    if (DateTime.UtcNow <= endTimeExtend)
                    {
                        ModelState.AddModelError(string.Empty, "This auction has not ended yet.");
                        return OnGet();
                    }

                    // Check if the auction is not set as completed
                    if (isCompletedExtend)
                    {
                        ModelState.AddModelError(string.Empty, "This auction was already marked as completed.");
                        return OnGet();
                    }

                    // Get buyerId
                    query = "SELECT ClientId FROM Product WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            buyerIdExtend = reader.GetInt32(reader.GetOrdinal("ClientId"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }

                    // Check if there's a buyer
                    if (buyerIdExtend != 0)
                    {
                        ModelState.AddModelError(string.Empty, "This auction has already been sold.");
                        return OnGet();
                    }
                }

                // Check if the new end time is greater than the current one
                if (ExtendedEndTime <= endTimeExtend)
                {
                    ModelState.AddModelError(string.Empty, "The new end time must be greater than the current one.");
                    return OnGet();
                }

                Console.WriteLine($"ExtendedEndTime: {ExtendedEndTime}");

                // Update auction end time and set IsCheckHasEnded to false
                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "UPDATE Auction SET EndTime = @EndTime, IsCheckHasEnded = 0 WHERE AuctionId = @AuctionId";
                    using SqlCommand cmd = new(query, con);
                    cmd.Parameters.AddWithValue("@EndTime", ExtendedEndTime);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                Console.WriteLine("Updated auction end time and IsCheckHasEnded");

                // Add new end time to AuctionEndTimes list
                var auctionsUtils = new Utils.Auctions(configuration, hubContext);
                auctionsUtils.AddAuction((DateTime)ExtendedEndTime);

                // Notify all users that there's a new auction
                hubContext.Clients.All.SendAsync("AuctionCreated").Wait();

                // Refresh auction page for all clients located that page
                hubContext.Clients.All.SendAsync("RefreshAuction", auctionId).Wait();

                Console.WriteLine("Done");

                return OnGet();
            }

            // Check if bid amount is greater than zero
            if (BidAmount != null)
            {
                if (BidAmount <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Your bid amount must be greater than zero.");
                    return OnGet();
                }

                decimal MinimumBid = 0;
                DateTime EndTime;
                int SellerId = 0;

                using (SqlConnection con = new(connectionString))
                {
                    con.Open();
                    string query = "SELECT MinimumBid, EndTime, ClientId FROM Auction WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            MinimumBid = reader.GetDecimal(reader.GetOrdinal("MinimumBid"));
                            EndTime = reader.GetDateTime(reader.GetOrdinal("EndTime"));
                            SellerId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }

                    decimal Price = 0;
                    int BuyerId = 0;

                    query = "SELECT Price, ClientId FROM Product WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            Price = reader.GetDecimal(reader.GetOrdinal("Price"));
                            BuyerId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }

                    // Check if auction has ended
                    if (DateTime.UtcNow > EndTime)
                    {
                        ModelState.AddModelError(string.Empty, "This auction has ended.");
                        return OnGet();
                    }

                    // Check if user is logged in
                    if (User.Identity?.IsAuthenticated == false)
                    {
                        ModelState.AddModelError(string.Empty, "You must be logged in to bid.");
                        return OnGet();
                    }

                    // Check if bid amount is less than current price plus minimum bid
                    if (Price + MinimumBid > BidAmount)
                    {
                        ModelState.AddModelError(string.Empty, "Your bid amount is too low.");
                        return OnGet();
                    }

                    // Check if current user is the seller
                    if (ClientId == SellerId)
                    {
                        ModelState.AddModelError(string.Empty, "You cannot bid on your own auction.");
                        return OnGet();
                    }

                    // Check if current user already is the highest bidder
                    if (ClientId == BuyerId)
                    {
                        ModelState.AddModelError(string.Empty, "You are already the highest bidder.");
                        return OnGet();
                    }

                    // Update Product table
                    query = "UPDATE Product SET Price = @Price, ClientId = @ClientId WHERE AuctionId = @AuctionId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Price", BidAmount);
                        cmd.Parameters.AddWithValue("@ClientId", ClientId);
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        cmd.ExecuteNonQuery();
                    }

                    // Get BidId
                    int BidId = 0;
                    query = "SELECT MAX(BidId) FROM Bid";
                    using (SqlCommand cmd = new(query, con))
                    {
                        var result = cmd.ExecuteScalar();
                        BidId = result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
                    }

                    // Update Bid table
                    DateTime BidTime = DateTime.UtcNow;
                    query = "INSERT INTO Bid (BidId, Value, Time, ClientId, AuctionId) VALUES (@BidId, @Value, @Time, @ClientId, @AuctionId)";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@BidId", BidId);
                        cmd.Parameters.AddWithValue("@Value", BidAmount);
                        cmd.Parameters.AddWithValue("@Time", BidTime);
                        cmd.Parameters.AddWithValue("@ClientId", ClientId);
                        cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                        cmd.ExecuteNonQuery();
                    }

                    // Add the bidder to the bidder group
                    var bidsUtils = new Utils.Bids(configuration);
                    bidsUtils.AddBidderToBidderGroupAsync(ClientId, auctionId).Wait();

                    // Notification message
                    string bidAmountFormatted = Utils.Currency.FormatDecimal((decimal)BidAmount) + "&euro;";
                    string message = $"A new bid has been placed on the amount of {bidAmountFormatted}";

                    // Create notification for each bidder except the current one
                    var notificationUtils = new Utils.Notification(configuration);
                    List<int> bidders = bidsUtils.GetBiddersFromAuction(auctionId);
                    foreach (int bidder in bidders)
                    {
                        if (bidder != ClientId)
                        {
                            notificationUtils.CreateNotification(bidder, auctionId, message);
                        }
                    }

                    // Refresh notifications count for all clients
                    // calculate number of unread messages for each client that is member of the bidders group
                    int notificationCount = 0;
                    foreach (int bidder in bidders)
                    {
                        notificationCount = notificationUtils.GetUnreadNotificationsCount(bidder);
                        // Console.WriteLine($"Bidder: {bidder} - Auction: {auctionId} - notificationCount: {notificationCount}");
                        hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, bidder).Wait();
                    }

                    // Create a notification for the seller
                    message = $"A new bid has been placed on your auction on the amount of {bidAmountFormatted}";
                    notificationUtils.CreateNotification(SellerId, auctionId, message);
                    notificationCount = notificationUtils.GetUnreadNotificationsCount(SellerId);
                    hubContext.Clients.All.SendAsync("UpdateNotifications", notificationCount, SellerId).Wait();

                    // Get username from database
                    string BuyerUsername = string.Empty;
                    string BuyerFullName = string.Empty;
                    query = "SELECT Username, FullName FROM Client WHERE ClientId = @ClientId";
                    using (SqlCommand cmd = new(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ClientId", ClientId);

                        using SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            BuyerUsername = reader.GetString(reader.GetOrdinal("Username"));
                            BuyerFullName = reader.GetString(reader.GetOrdinal("FullName"));
                        }
                        else
                        {
                            reader.Close();
                            con.Close();
                            return NotFound();
                        }
                        reader.Close();
                    }
                    BuyerUsername = "@" + BuyerUsername;

                    // Refresh all auction page updated data for all clients located that page
                    decimal BidValue = (decimal)BidAmount + MinimumBid;
                    string BidAmountFormatted = Utils.Currency.FormatDecimal((decimal)BidAmount) + "€";
                    string PlaceholderBidFormatted = Utils.Currency.FormatDecimal(Price + MinimumBid) + "€";
                    string BidTimeFormatted = BidTime.ToString("yyyy-MM-dd HH:mm:ss");
                    hubContext.Clients.All.SendAsync("UpdateAuction",
                        auctionId,
                        BidValue,
                        BidAmountFormatted,
                        PlaceholderBidFormatted,
                        BuyerId,
                        BuyerUsername,
                        BuyerFullName,
                        BidTimeFormatted,
                        SellerId)
                        .Wait();

                    Console.WriteLine("Sent notification to clients");

                    con.Close();
                }
            }

            return OnGet();
        }

        /// <summary>
        /// Retrieves the auction information from the database.
        /// </summary>
        /// <remarks>
        /// Retrieves the auction information from the database and displays it on the page.
        /// It also retrieves the product information and the bids history.
        /// <para/>
        /// It parses the auction id through the request query string. If the id is not found,
        /// it redirects to the not found error (404) page.
        /// <para/>
        /// The auction end time is displayed in a countdown timer with javascript.
        /// The images, if any, can be iterated in right and left mouse clicks.
        /// The bid history is displayed in a collapsible table.
        /// <para/>
        /// The seller as the option to early sell/terminate the auction, depending on the number of bids and
        /// if the auction has ended or not.
        /// <para/>
        /// When the auction ends and there isn't any bids, the seller has the option to extend the auction.
        /// <para/>
        /// The buyer has the option to bid if the auction has not ended yet.
        /// <para/>
        /// The winner of the auction will be displayed after the auction has ended, and this user will have the
        /// option to complete the payment by choosing a payment method and pressing the "Pay" button.
        /// </remarks>
        /// <returns>A task that represents the auction on get operation.</returns>
        public IActionResult OnGet()
        {
            if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
            {
                return NotFound();
            }

            Console.WriteLine($"Auction id requested: {auctionId}");

            ViewData["AuctionId"] = auctionId;

            // Get current ClientId
            var clientUtilsClientId = new Utils.Client(configuration);
            int currentClientId = clientUtilsClientId.GetClientId(HttpContext, User);
            ViewData["CurrentClientId"] = currentClientId;

            // Get the bidders group for the current auction
            var bidsUtils = new Utils.Bids(configuration);
            List<int> biddersGroup = bidsUtils.GetBiddersFromAuction(auctionId);
            ViewData["BiddersGroup"] = biddersGroup;

            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            SqlConnection con = new(connectionString);

            try
            {
                con.Open();

                string query = "SELECT StartTime, EndTime, ClientId, ProductId, MinimumBid, IsCompleted FROM Auction WHERE AuctionId = @AuctionId";
                SqlCommand cmd = new(query, con);

                cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    DateTime startTime = reader.GetDateTime(reader.GetOrdinal("StartTime"));
                    DateTime endTime = reader.GetDateTime(reader.GetOrdinal("EndTime"));
                    int clientId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                    int productId = reader.GetInt32(reader.GetOrdinal("ProductId"));
                    decimal minimumBid = reader.GetDecimal(reader.GetOrdinal("MinimumBid"));
                    bool isCompleted = reader.GetBoolean(reader.GetOrdinal("IsCompleted"));

                    // Values to be used in the cshtml page
                    ViewData["StartTime"] = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                    ViewData["EndTime"] = endTime.ToString("yyyy-MM-dd HH:mm:ss");
                    ViewData["MinimumBid"] = minimumBid;
                    ViewData["SellerId"] = clientId;
                    ViewData["IsCompleted"] = isCompleted;

                    // Check if auction has ended
                    ViewData["AuctionEnded"] = (DateTime.UtcNow >= endTime);

                    // If the current user is the seller and the auction hasn't ended yet, add edit option
                    if (currentClientId == clientId && DateTime.UtcNow < endTime)
                        ViewData["Edit"] = true;

                    reader.Close();

                    // Get seller username (NOTE no exceptions are being made)
                    string SellerFullName = string.Empty;
                    string SellerUsername = string.Empty;
                    string queryClient = "SELECT FullName, Username FROM Client WHERE ClientId = @ClientId";
                    SqlCommand cmdClient = new(queryClient, con);
                    cmdClient.Parameters.AddWithValue("@ClientId", clientId);
                    using (SqlDataReader readerClient = cmdClient.ExecuteReader())
                    {
                        if (readerClient.Read())
                        {
                            SellerFullName = readerClient.GetString(readerClient.GetOrdinal("FullName"));
                            SellerUsername = readerClient.GetString(readerClient.GetOrdinal("Username"));
                        }
                        readerClient.Close();
                    }

                    ViewData["SellerFullName"] = SellerFullName;
                    ViewData["SellerUsername"] = SellerUsername;

                    // Get Product info: name, description and price
                    string productName = string.Empty;
                    string productDesc = string.Empty;
                    decimal productPrice = 0;
                    int BuyerId = 0;

                    var imageUtils = new Utils.Images(configuration);
                    List<string> Images = [];

                    string productQuery = "SELECT Name, Description, Price, ClientId, Images FROM Product WHERE ProductId = @ProductId";
                    SqlCommand cmdProduct = new(productQuery, con);
                    cmdProduct.Parameters.AddWithValue("@ProductId", productId);
                    using (SqlDataReader readerProduct = cmdProduct.ExecuteReader())
                    {
                        if (readerProduct.Read())
                        {
                            productName = readerProduct.GetString(readerProduct.GetOrdinal("Name"));
                            productDesc = readerProduct.GetString(readerProduct.GetOrdinal("Description"));
                            productPrice = readerProduct.GetDecimal(readerProduct.GetOrdinal("Price"));
                            BuyerId = readerProduct.GetInt32(readerProduct.GetOrdinal("ClientId"));

                            // Check for null before calling GetString
                            var Imagestmp = readerProduct.IsDBNull(readerProduct.GetOrdinal("Images"))
                                ? null
                                : readerProduct.GetString(readerProduct.GetOrdinal("Images"));

                            // Check if Imagestmp is not null before parsing
                            Images = Imagestmp != null ? imageUtils.ParseImagesList(Imagestmp) : [];
                        }
                        readerProduct.Close();
                    }

                    ViewData["BuyerId"] = BuyerId;
                    ViewData["ProductName"] = productName;
                    ViewData["ProductDesc"] = productDesc;
                    ViewData["ProductPrice"] = productPrice;
                    ViewData["BidPlaceholder"] = (decimal)productPrice + minimumBid;
                    ViewData["Images"] = Images;
                    // Console.WriteLine($"Images: {string.Join(", ", Images)}");

                    // Get buyer info
                    query = "SELECT FullName, Username FROM Client WHERE ClientId = @ClientId";
                    using (SqlCommand cmdBuyer = new(query, con))
                    {
                        cmdBuyer.Parameters.AddWithValue("@ClientId", BuyerId);
                        using SqlDataReader readerBuyer = cmdBuyer.ExecuteReader();
                        if (readerBuyer.Read())
                        {
                            ViewData["BuyerFullName"] = readerBuyer.GetString(readerBuyer.GetOrdinal("FullName"));
                            ViewData["BuyerUsername"] = readerBuyer.GetString(readerBuyer.GetOrdinal("Username"));
                        }
                        readerBuyer.Close();
                    }

                    // Get clients info to avoid multiple sql queries
                    var clientUtils = new Utils.Client(configuration);
                    List<Dictionary<string, object>> clients = clientUtils.GetClients();

                    // Get bids history
                    List<Dictionary<string, object>> Bids = [];
                    query = "SELECT Value, Time, ClientId FROM Bid WHERE AuctionId = @AuctionId ORDER BY Time DESC"; // Check order by time
                    using SqlCommand cmdBids = new(query, con);
                    cmdBids.Parameters.AddWithValue("@AuctionId", auctionId);
                    using SqlDataReader readerBids = cmdBids.ExecuteReader();
                    while (readerBids.Read())
                    {
                        Dictionary<string, object> Bid = new()
                        {
                            ["Value"] = readerBids.GetDecimal(readerBids.GetOrdinal("Value")),
                            ["Time"] = readerBids.GetDateTime(readerBids.GetOrdinal("Time")).ToString("yyyy-MM-dd HH:mm:ss")
                        };

                        // Get bidder username using ClientId
                        int BidderId = readerBids.GetInt32(readerBids.GetOrdinal("ClientId"));
                        foreach (Dictionary<string, object> client in clients)
                        {
                            if ((int)client["ClientId"] == BidderId)
                            {
                                // Bid["FullName"] = client["FullName"];
                                Bid["Username"] = client["Username"];
                                Bid["BidderId"] = BidderId;
                            }
                        }

                        Bids.Add(Bid);
                    }

                    ViewData["Bids"] = Bids;
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
