using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.SignalR;

namespace BetterFinds.Pages
{
    public class AuctionModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;
        public AuctionModel(IConfiguration configuration, IHubContext<NotificationHub> hubContext)
        {
            _configuration = configuration;
            _hubContext = hubContext;
        }
        
        [BindProperty]
        public int BidAmount { get; set; } = 0;
        public IActionResult OnPostAsync()
        {
            if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
            {
                return NotFound();
            }
            
            // Check if bid amount is greater than zero
            if (BidAmount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Your bid amount must be greater than zero.");
                return OnGet();
            }

            int MinimumBid = 0;
            DateTime EndTime;
            int SellerId = 0;

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT MinimumBid, EndTime, ClientId FROM Auction WHERE AuctionId = @AuctionId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            MinimumBid = reader.GetInt32(reader.GetOrdinal("MinimumBid"));
                            EndTime = reader.GetDateTime(reader.GetOrdinal("EndTime"));
                            SellerId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }

                int Price = 0;
                int BuyerId = 0;

                query = "SELECT Price, ClientId FROM Product WHERE AuctionId = @AuctionId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Price = reader.GetInt32(reader.GetOrdinal("Price"));
                            BuyerId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }

                // Check if auction has ended
                if (DateTime.Now > EndTime)
                {
                    ModelState.AddModelError(string.Empty, "This auction has ended.");
                    return OnGet();
                }

                // Check if user is logged in
                if (User.Identity != null && !User.Identity.IsAuthenticated)
                {
                    ModelState.AddModelError(string.Empty, "You must be logged in to bid.");
                    return OnGet();
                }

                // Check if bid amount is less than current price plus minimum bid
                if (Price + MinimumBid > BidAmount * 100)
                {
                    ModelState.AddModelError(string.Empty, "Your bid amount is too low.");
                    return OnGet();
                }

                // Get current ClientId
                var clientUtils = new Utils.Client(_configuration);
                int ClientId = clientUtils.GetClientId(HttpContext, User);

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
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Price", BidAmount * 100);
                    cmd.Parameters.AddWithValue("@ClientId", ClientId);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    cmd.ExecuteNonQuery();
                }

                // Get BidId
                int BidId = 0;
                query = "SELECT MAX(BidId) FROM Bid";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                        BidId = Convert.ToInt32(result) + 1;
                    else
                        BidId = 1;
                }

                // Update Bid table
                query = "INSERT INTO Bid (BidId, Value, Time, ClientId, AuctionId) VALUES (@BidId, @Value, @Time, @ClientId, @AuctionId)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@BidId", BidId);
                    cmd.Parameters.AddWithValue("@Value", BidAmount * 100);
                    cmd.Parameters.AddWithValue("@Time", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ClientId", ClientId);
                    cmd.Parameters.AddWithValue("@AuctionId", auctionId);

                    cmd.ExecuteNonQuery();
                }

                // Add the bidder to the bidder group
                var bidsUtils = new Utils.Bids(_configuration);
                bidsUtils.AddBidderToBidderGroup(ClientId, auctionId).Wait();

                // Notification message
                string message = $"A new bid has been placed on the amount of {BidAmount}�";

                // Create notification for each bidder except the current one
                // Refresh notifications count for all these clients
                var notificationUtils = new Utils.Notification(_configuration);
                List<int> bidders = bidsUtils.GetBiddersFromAuction(auctionId);
                int notificationCount = 0;
                for (int i = 0; i < bidders.Count; i++)
                {
                    if (bidders[i] != ClientId)
                    {
                        notificationUtils.CreateNotification(bidders[i], auctionId, message);
                        notificationCount = notificationUtils.GetNUnreadMessages(bidders[i]);
                        _hubContext.Clients.All.SendAsync("ReceiveNotificationCount", notificationCount, bidders[i]).Wait();
                    }
                }

                // Refresh auction page for all clients located that page
                _hubContext.Clients.All.SendAsync("UpdatePrice", ClientId, auctionId, message).Wait();

                Console.WriteLine("Send notification to clients");
            }

            return OnGet();
        }
        public IActionResult OnGet()
        {
            if (!int.TryParse(HttpContext.Request.Query["id"], out int auctionId))
            {
                return NotFound();
            }

            // Get current ClientId
            var clientUtilsClientId = new Utils.Client(_configuration);
            int currentClientId = clientUtilsClientId.GetClientId(HttpContext, User);
            ViewData["CurrentClientId"] = currentClientId;

            // Get the bidders group for the current auction
            var bidsUtils = new Utils.Bids(_configuration);
            List<int> biddersGroup = bidsUtils.GetBiddersFromAuction(auctionId);
            ViewData["BiddersGroup"] = biddersGroup;

            Console.WriteLine($"Auction id requested: {auctionId}"); // TODO remove this

            ViewData["AuctionId"] = auctionId;

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
                        int minimumBid = reader.GetInt32(reader.GetOrdinal("MinimumBid"));


                        // Values to be used in the cshtml page
                        ViewData["StartTime"] = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                        ViewData["EndTime"] = endTime.ToString("yyyy-MM-dd HH:mm:ss");
                        ViewData["MinimumBid"] = ((int)minimumBid / 100).ToString("0.00");
                        ViewData["SellerId"] = clientId;

                        // Check if auction has ended
                        ViewData["AuctionEnded"] = (DateTime.Now >= endTime);

                        reader.Close();

                        // Get seller username (NOTE no exceptions are being made)
                        string SellerFullName = string.Empty;
                        string SellerUsername = string.Empty;
                        string queryClient = "SELECT FullName, Username FROM Client WHERE ClientId = @ClientId";
                        SqlCommand cmdClient = new SqlCommand(queryClient, con);
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
                        int productPrice = 0;
                        int BuyerId = 0;

                        var imageUtils = new Utils.Images(_configuration);
                        List<string> Images = new List<string>();

                        string productQuery = "SELECT Name, Description, Price, ClientId, Images FROM Product WHERE ProductId = @ProductId";
                        SqlCommand cmdProduct = new SqlCommand(productQuery, con);
                        cmdProduct.Parameters.AddWithValue("@ProductId", productId);
                        using (SqlDataReader readerProduct = cmdProduct.ExecuteReader())
                        {
                            if (readerProduct.Read())
                            {
                                productName = readerProduct.GetString(readerProduct.GetOrdinal("Name"));
                                productDesc = readerProduct.GetString(readerProduct.GetOrdinal("Description"));
                                productPrice = readerProduct.GetInt32(readerProduct.GetOrdinal("Price"));
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
                        ViewData["ProductPrice"] = ((decimal)productPrice / 100).ToString("0.00");
                        ViewData["BidPlaceholder"] = ((decimal)(productPrice + minimumBid) / 100).ToString("0.00");
                        ViewData["Images"] = Images;
                        // Console.WriteLine($"Images: {string.Join(", ", Images)}");

                        // Get buyer info
                        query = "SELECT FullName, Username FROM Client WHERE ClientId = @ClientId";
                        using (SqlCommand cmdBuyer = new SqlCommand(query, con))
                        {
                            cmdBuyer.Parameters.AddWithValue("@ClientId", BuyerId);
                            using (SqlDataReader readerBuyer = cmdBuyer.ExecuteReader())
                            {
                                if (readerBuyer.Read())
                                {
                                    ViewData["BuyerFullName"] = readerBuyer.GetString(readerBuyer.GetOrdinal("FullName"));
                                    ViewData["BuyerUsername"] = readerBuyer.GetString(readerBuyer.GetOrdinal("Username"));
                                }
                                readerBuyer.Close();
                            }
                        }

                        // Get clients info to avoid multiple sql queries
                        var clientUtils = new Utils.Client(_configuration);
                        List<Dictionary<string, object>> clients = clientUtils.GetClients();

                        // Get bids history
                        List<Dictionary<string, object>> Bids = new List<Dictionary<string, object>>();
                        query = "SELECT Value, Time, ClientId FROM Bid WHERE AuctionId = @AuctionId ORDER BY Time DESC"; // Check order by time
                        using (SqlCommand cmdBids = new SqlCommand(query, con))
                        {
                            cmdBids.Parameters.AddWithValue("@AuctionId", auctionId);
                            using (SqlDataReader readerBids = cmdBids.ExecuteReader())
                            {
                                while (readerBids.Read())
                                {
                                    Dictionary<string, object> Bid = new Dictionary<string, object>();
                                    Bid["Value"] = ((decimal)readerBids.GetInt32(readerBids.GetOrdinal("Value")) / 100).ToString("0.00");
                                    Bid["Time"] = readerBids.GetDateTime(readerBids.GetOrdinal("Time")).ToString("yyyy-MM-dd HH:mm:ss");

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
                        }
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
