using BetterFinds.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages;

public class AuctionModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext) : PageModel
{
    [BindProperty]
    public decimal BidAmount { get; set; }

    [BindProperty]
    public string? PaymentMethod { get; set; }

    [BindProperty]
    public string? EarlySell { get; set; }

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
                notificationCount = notificationUtils.GetNUnreadMessages(bidder);
                hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, bidder).Wait();
            }

            // calculate number of unread messages for the seller
            notificationCount = notificationUtils.GetNUnreadMessages(_SellerId);
            hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, _SellerId).Wait();

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
                    notificationCount = notificationUtils.GetNUnreadMessages(bidder);
                    hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, bidder).Wait();
                }
            }

            // Notify the buyer that the auction has ended and that he had won the auction
            message = "The seller terminated the auction and you have won! Please go to the auction page to complete the payment.";
            notificationUtils.CreateNotification(BuyerIdEarlySell, auctionId, message);
            notificationCount = notificationUtils.GetNUnreadMessages(BuyerIdEarlySell);
            hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, BuyerIdEarlySell).Wait();

            // Refresh auction page for all clients located that page
            hubContext.Clients.All.SendAsync("UpdateAuction", ClientId, auctionId).Wait();

            // Refresh notifications page for all clients located that page
            hubContext.Clients.All.SendAsync("UpdateNotifications", ClientId).Wait();

            return OnGet();
        }

        // Check if bid amount is greater than zero
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
            if (User.Identity != null && User.Identity.IsAuthenticated == false)
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
            query = "INSERT INTO Bid (BidId, Value, Time, ClientId, AuctionId) VALUES (@BidId, @Value, @Time, @ClientId, @AuctionId)";
            using (SqlCommand cmd = new(query, con))
            {
                cmd.Parameters.AddWithValue("@BidId", BidId);
                cmd.Parameters.AddWithValue("@Value", BidAmount);
                cmd.Parameters.AddWithValue("@Time", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@ClientId", ClientId);
                cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                cmd.ExecuteNonQuery();
            }

            // Add the bidder to the bidder group
            var bidsUtils = new Utils.Bids(configuration);
            bidsUtils.AddBidderToBidderGroupAsync(ClientId, auctionId).Wait();

            // Notification message
            string message = $"A new bid has been placed on the amount of {Utils.Currency.FormatDecimal(BidAmount)}€";

            // Create notification for each bidder except the current one
            var notificationUtils = new Utils.Notification(configuration);
            List<int> bidders = bidsUtils.GetBiddersFromAuction(auctionId);
            foreach (int bidder in bidders)
            {
                if (bidder != ClientId)
                {
                    notificationUtils.CreateNotification(bidder, auctionId, message);

                    // Refresh notifications page for all clients located that page
                    hubContext.Clients.All.SendAsync("UpdateNotifications", bidder).Wait();
                }
            }

            // Refresh notifications count for all clients
            // calculate number of unread messages for each client that is member of the bidders group
            int notificationCount = 0;
            foreach (int bidder in bidders)
            {
                notificationCount = notificationUtils.GetNUnreadMessages(bidder);
                // Console.WriteLine($"Bidder: {bidder} - Auction: {auctionId} - notificationCount: {notificationCount}");
                hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, bidder).Wait();

                // Refresh auction page for all clients located that page
                hubContext.Clients.All.SendAsync("UpdateAuction", bidder, auctionId).Wait();
            }

            // Create a notification for the seller
            message = $"A new bid has been placed on your auction on the amount of {Utils.Currency.FormatDecimal(BidAmount)}€";
            notificationUtils.CreateNotification(SellerId, auctionId, message);
            notificationCount = notificationUtils.GetNUnreadMessages(SellerId);
            hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, SellerId).Wait();
            hubContext.Clients.All.SendAsync("UpdateAuction", SellerId, auctionId).Wait();
            hubContext.Clients.All.SendAsync("UpdateNotifications", SellerId).Wait();

            Console.WriteLine("Sent notification to clients");

            con.Close();
        }

        return OnGet();
    }
    public IActionResult OnGet()
    {
        if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
        {
            return NotFound();
        }

        // Set current page to update auction page with SignalR
        ViewData["CurrentPage"] = "Auction";

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
