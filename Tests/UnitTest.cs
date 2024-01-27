using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Safari;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Tests.Utils;

[assembly: Parallelizable(ParallelScope.Fixtures)]
namespace Tests
{
    [TestFixture("Firefox", "122.0", "Windows 10", "BetterFinds Firefox122")]
    // [TestFixture("Chrome", "121.0", "Windows 10", "BetterFinds ChromeLatest")]
    // [TestFixture("Edge", "120.0", "Windows 10", "BetterFinds EdgeLatest")]
    // [TestFixture("Safari", "17.0", "macOS Big Sur", "BetterFinds Safari14")]
    public class Tests(string browser, string version, string os, string name)
    {
        public IWebDriver driver;
        private readonly string ltUserName = "REDACTED";
        private readonly string ltAppKey = "REDACTED";

        private readonly string url = "https://betterfinds.pt/";

        // input data for the register and login tests
        private static readonly string FullName = "Miguel Carvalho";
        private static readonly string Username = "miguel123";
        private static readonly string Email = "miguel123@example.com";
        private static readonly string Password = "password123";
        private static readonly string ConfirmPassword = Password;
        private static readonly string ProfilePic = "";
        private static readonly bool OptNewsletter = true;

        private readonly Dictionary<string, string> accountInput = new()
        {
            { "FullName", FullName },
            { "Username", Username },
            { "Email", Email },
            { "Password", Password },
            { "ConfirmPassword", ConfirmPassword },
            { "ProfilePic", ProfilePic },
        };

        private readonly Dictionary<string, string> editAccountInput = new()
        {
            { "FullName", FullName + " Edited" },
            { "Email", "miguel123edited@example.com" },
            { "ProfilePic", ProfilePic },
        };

        // input data for buyer account
        private static readonly string BuyerFullName = "Test Buyer";
        private static readonly string BuyerUsername = "testbuyer";
        private static readonly string BuyerEmail = "testbuyer@example.com";
        private static readonly string BuyerPassword = "password123";
        private static readonly string BuyerConfirmPassword = BuyerPassword;
        private static readonly string BuyerProfilePic = "";
        private static readonly bool BuyerOptNewsletter = false;

        private readonly Dictionary<string, string> buyerAccountInput = new()
        {
            { "FullName", BuyerFullName },
            { "Username", BuyerUsername },
            { "Email", BuyerEmail },
            { "Password", BuyerPassword },
            { "ConfirmPassword", BuyerConfirmPassword },
            { "ProfilePic", BuyerProfilePic },
        };

        private readonly Dictionary<string, string> buyerAccountInput2 = new()
        {
            { "FullName", BuyerFullName + " two" },
            { "Username", BuyerUsername + "2" },
            { "Email", "testbuyer2@example.com" },
            { "Password", BuyerPassword },
            { "ConfirmPassword", BuyerConfirmPassword },
            { "ProfilePic", BuyerProfilePic },
        };

        // input data for the create auction test
        private static readonly string Title = "Automated Test Auction";
        private static readonly string Description = "This auction was created by an automated test";
        private static readonly string Price = "150.50";
        private static readonly string MinimumBid = "1.75";
        private static readonly string EndTime = DateTime.UtcNow.AddMinutes(5).ToString("MM-dd-yyyyyy hh:mm tt");
        private static readonly string Images = ""; // TODO: add images links

        private readonly Dictionary<string, string> auctionInput = new()
        {
            { "Title", Title },
            { "Description", Description },
            { "Price", Price },
            { "MinimumBid", MinimumBid },
            { "EndTime", EndTime },
            { "Images", Images },
        };

        private readonly Dictionary<string, string> editAuctionInput = new()
        {
            { "Title", Title + " (Edited)" },
            { "Description", Description + " (Edited)" },
            { "MinimumBid", (decimal.Parse(MinimumBid) + 1).ToString() },
            { "Images", "" }, // TODO: edit images links
        };

        [SetUp]
        public void Setup()
        {
            dynamic capability = GetBrowserOptions(browser);

            Dictionary<string, object> ltOptions = new()
            {
                { "browserName", browser },
                { "version", version },
                { "platformName", os },
                { "name", name },
                { "smartUI.project", name },
                { "username", ltUserName },
                { "accessKey", ltAppKey },
                { "console", "true" },
                { "plugin", "c#-nunit" },
                { "timezone", "UTC+00:00" },
                { "geoLocation", "PT" },
                { "network", true },
                { "w3c", true },
                { "visual", true },
                { "video", true },
                { "terminal", true },
                { "tunel", false },
            };

            capability.AddAdditionalOption("LT:Options", ltOptions);

            // LambdaTest connection
            driver = new RemoteWebDriver(new Uri("https://" + ltUserName + ":" + ltAppKey + "@hub.lambdatest.com/wd/hub"), capability);

            // Selenium hub local connection
            // driver = new RemoteWebDriver(new Uri("http://localhost:4444/wd/hub"), capabilities);

            driver.Manage().Window.Maximize();
            driver.Navigate().GoToUrl(url);
        }

        [Test, Order(1)]
        public void Register()
        {
            Console.WriteLine("[Starting ***Register*** test]");

            Procedures.Register(driver, url, accountInput, OptNewsletter);

            // This doesn't effect the validity of the test
            Procedures.DeleteAccount(Username);

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the element containing the success message to be present
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement successMessageElement = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // sleep 1 second
                Task.Delay(1000).Wait();

                // Assert that the success message is present
                Assert.That(successMessageElement.Text, Does.Contain("successfully"));
            }
            catch (TimeoutException)
            {
                // Handle the case where the success message doesn't appear
                IWebElement registerMessageElement = driver.FindElement(By.CssSelector(".text-danger"));
                string registerMessage = registerMessageElement.Text;
                Assert.Fail("[FAIL ***Register*** test] Success message not found. " + registerMessage);
            }
        }

        [Test, Order(2)]
        public void Login()
        {
            Console.WriteLine("[Starting ***Login*** test]");

            Console.WriteLine("Creating an account...");
            Procedures.Register(driver, url, accountInput, OptNewsletter);

            Procedures.Login(driver, url, Username, Password);

            // This doesn't effect the validity of the test
            Procedures.DeleteAccount(Username);

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the welcome message element to be present
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement welcomeElement = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // sleep 1 second
                Task.Delay(1000).Wait();

                // Assert that the welcome text is present
                Assert.That(welcomeElement.Text, Does.Contain("Welcome @" + Username));
            }
            catch (TimeoutException)
            {
                // Handle the case where the redirect doesn't happen
                IWebElement loginMessageElement = driver.FindElement(By.CssSelector(".text-danger"));
                string loginMessage = loginMessageElement.Text;
                Assert.Fail("[FAIL ***Login*** test] Redirect to welcome page did not occur. " + loginMessage);
            }
        }

        // TODO logout test

        [TestCase(5), Order(3)]
        public void CreateAuction(int minutesToEnd)
        {
            Console.WriteLine("[Starting ***Create Auction*** test]");

            Console.WriteLine("Creating an account...");
            Procedures.Register(driver, url, accountInput, OptNewsletter);

            Console.WriteLine("Create auction requires logging in...");
            Procedures.Login(driver, url, Username, Password);

            Console.WriteLine("Creating an auction...");
            Procedures.CreateAuction(driver, url, auctionInput, minutesToEnd);

            // This doesn't effect the validity of the test
            Console.WriteLine("Deleting the account created...");
            Procedures.DeleteAccount(Username);

            Console.WriteLine("Deleting the auction created...");
            Procedures.DeleteAuction(Sql.GetLastAuctionId());

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the element containing the auction title to be present
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement auctionTitleElement = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // sleep 1 second
                Task.Delay(1000).Wait();

                // Assert that the auction title is present
                Assert.That(auctionTitleElement.Text, Does.Contain(auctionInput["Title"]));
            }
            catch (TimeoutException)
            {
                // Handle the case where the redirect doesn't happen
                IWebElement createAuctionMessageElement = driver.FindElement(By.CssSelector(".text-danger"));
                string createAuctionMessage = createAuctionMessageElement.Text;
                Assert.Fail("[FAIL ***Create Auction*** test] Redirect to the new auction page did not occur. " + createAuctionMessage);
            }
        }

        [Test, Order(4)]
        public void ViewHomepage()
        {
            Console.WriteLine("[Starting ***View Homepage*** test]");

            // Go to the homepage
            driver.Navigate().GoToUrl(url);

            // sleep 1 second
            Task.Delay(1000).Wait();

            Assert.Pass();
        }

        [Test, Order(5)]
        public void EditAuction()
        {
            // Edit auction requires logging in and creating an auction
            Console.WriteLine("Create seller account and auction...");
            Procedures.Register(driver, url, accountInput, OptNewsletter);
            Procedures.Login(driver, url, Username, Password);
            Procedures.CreateAuction(driver, url, auctionInput, 5);

            Procedures.EditAuction(driver, url, editAuctionInput);

            Console.WriteLine("Deleting the account created...");
            Procedures.DeleteAccount(Username);

            Console.WriteLine("Deleting the auction created...");
            Procedures.DeleteAuction(Sql.GetLastAuctionId());

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the element containing the success message to be present
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
                IWebElement successMessageElement = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // sleep 1 second
                Task.Delay(1000).Wait();

                // Assert that the success message is present
                Assert.That(successMessageElement.Text, Does.Contain("successfully"));
            }
            catch (TimeoutException)
            {
                // Handle the case where the success message doesn't appear
                IWebElement editAuctionMessageElement = driver.FindElement(By.CssSelector(".text-danger"));
                string editAuctionMessage = editAuctionMessageElement.Text;
                Assert.Fail("[FAIL ***Edit Auction*** test] Success message not found. " + editAuctionMessage);
            }
        }

        [Test, Order(6)]
        public void SearchAuction()
        {
            Console.WriteLine("[Starting ***Search Auction*** test]");

            Console.WriteLine("Creating an account...");
            Procedures.Register(driver, url, accountInput, OptNewsletter);

            Console.WriteLine("Create auction requires logging in...");
            Procedures.Login(driver, url, Username, Password);

            Console.WriteLine("Creating an auction...");
            Procedures.CreateAuction(driver, url, auctionInput, 5);

            Procedures.Search(driver, url, query: "Test Auction");

            Console.WriteLine("Deleting the account created...");
            Procedures.DeleteAccount(Username); 

            Console.WriteLine("Deleting the auction created...");
            Procedures.DeleteAuction(Sql.GetLastAuctionId());

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the element indicating that we are on the search page
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement searchResults = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // sleep 1 second
                Task.Delay(1000).Wait();

                // Assert that the header text contains "results"
                Assert.That(searchResults.Text, Does.Contain("results"));
            }
            catch (TimeoutException)
            {
                // Handle the case where the redirect doesn't happen
                Assert.Fail("[FAIL ***Search Auction*** test] No redirect to the search page");
            }
        }

        [Test, Order(7)]
        public void TerminateAuction()
        {
            Console.WriteLine("[Starting ***Terminate Auction*** test]");

            Console.WriteLine("Creating an account as a seller...");
            Procedures.Register(driver, url, accountInput, OptNewsletter);

            Console.WriteLine("Create auction requires logging in...");
            Procedures.Login(driver, url, Username, Password);

            // sleep 1 second
            Task.Delay(1000).Wait();

            Console.WriteLine("Creating an auction...");
            Procedures.CreateAuction(driver, url, auctionInput, 5);

            // sleep 1 second
            Task.Delay(1000).Wait();

            Console.WriteLine("Terminating the auction...");
            Procedures.TerminateAuction(driver, url);

            Console.WriteLine("Deleting the account created...");
            Procedures.DeleteAccount(Username);

            Console.WriteLine("Deleting the auction created...");
            Procedures.DeleteAuction(Sql.GetLastAuctionId());

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the element indicating that we are on the search page
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement searchResults = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // sleep 1 second
                Task.Delay(1000).Wait();

                // Assert that body contains "Auction has ended"
                Assert.That(searchResults.Text, Does.Contain("Auction has ended"));
            }
            catch (TimeoutException)
            {
                Assert.Fail("[FAIL ***Terminate Auction*** test] Auction wasn't terminated");
            }
        }

        [TestCase(2.55), Order(8)]
        public void MakeBid(decimal bidIncrement)
        {
            // NOTE: bidIncrement must be greater than the minimum bid to make a bid
            Console.WriteLine("[Starting ***Make Bid*** test]");

            Console.WriteLine("Creating an account as a seller...");
            Procedures.Register(driver, url, accountInput, OptNewsletter);

            Console.WriteLine("Create auction requires logging in...");
            Procedures.Login(driver, url, Username, Password);

            Console.WriteLine("Creating an auction...");
            Procedures.CreateAuction(driver, url, auctionInput, 5);

            Console.WriteLine("Logging out...");
            Procedures.Logout(driver, url);

            Console.WriteLine("Creating an account as a bidder...");
            Procedures.Register(driver, url, buyerAccountInput, BuyerOptNewsletter);

            Console.WriteLine("Login as a bidder...");
            Procedures.Login(driver, url, BuyerUsername, BuyerPassword);

            Console.WriteLine("Making a bid...");
            decimal bidAmount = decimal.Parse(auctionInput["Price"]) + bidIncrement;
            Procedures.MakeBid(driver, url, bidAmount);

            Console.WriteLine("Deleting the accounts and auction created...");
            Procedures.DeleteAccount(Username);
            Procedures.DeleteAccount(BuyerUsername);
            Procedures.DeleteAuction(Sql.GetLastAuctionId());

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the element indicating that we are on the search page
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement searchResults = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // Assert that body contains $"Buyer info: @{BuyerUsername}"
                Assert.That(searchResults.Text, Does.Contain($"Buyer info: @{BuyerUsername}"));
            }
            catch (TimeoutException)
            {
                Assert.Fail("[FAIL ***Make Bid*** test] Bid wasn't made or page didn't update the buyer username");
            }
        }

        [Test, Order(9)]
        public void EarlySellAuction()
        {
            Console.WriteLine("[Starting ***Early Sell Auction*** test]");

            Console.WriteLine("Creating seller account, buyer account and the auction...");
            Procedures.Register(driver, url, accountInput, OptNewsletter);
            Procedures.Register(driver, url, buyerAccountInput, BuyerOptNewsletter);
            Procedures.Login(driver, url, Username, Password);
            Procedures.CreateAuction(driver, url, auctionInput, 5);
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, BuyerUsername, BuyerPassword);
            Procedures.MakeBid(driver, url, decimal.Parse(auctionInput["Price"]) + 1);
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, Username, Password);

            Console.WriteLine("Early selling the auction...");
            Procedures.TerminateAuction(driver, url);

            Console.WriteLine("Deleting the accounts and auction created...");
            Procedures.DeleteAccount(Username);
            Procedures.DeleteAccount(BuyerUsername);
            Procedures.DeleteAuction(Sql.GetLastAuctionId());

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the element indicating that we are on the search page
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement searchResults = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // Assert that body contains "Auction has ended"
                Assert.That(searchResults.Text, Does.Contain("Auction has ended"));
            }
            catch (TimeoutException)
            {
                Assert.Fail("[FAIL ***Early Sell Auction*** test] Auction wasn't early sold as it's not ended");
            }
        }

        [Test, Order(10)]
        public void MainTest() {
            // Creating accounts
            Procedures.Register(driver, url, buyerAccountInput, BuyerOptNewsletter);
            Procedures.Register(driver, url, buyerAccountInput2, BuyerOptNewsletter);
            Procedures.Register(driver, url, accountInput, OptNewsletter);

            // The seller edits his account and shows both the public and private account
            Procedures.Login(driver, url, Username, Password);
            Procedures.EditAccount(driver, url, editAccountInput);
            Task.Delay(2000).Wait();
            driver.Navigate().GoToUrl(url + "account?id=" + Sql.GetClientIdByUsername(Username));
            Task.Delay(1000).Wait();
            Procedures.Logout(driver, url);

            // First scenario: Seller creates an auction then terminates it without any bids
            // it also showcases the search and sort by options
            Procedures.Login(driver, url, Username, Password);
            Procedures.CreateAuction(driver, url, auctionInput, 5);
            Procedures.EditAuction(driver, url, editAuctionInput);
            Procedures.Search(driver, url, query: "Test Auction");
            Task.Delay(2000).Wait();
            // Procedures.SortBy(driver, 2, 1); // 2: Product name (A-Z); 1: all auctions
            Procedures.TerminateAuction(driver, url);
            Procedures.DeleteAuction(Sql.GetLastAuctionId());
            Procedures.Logout(driver, url);

            // Second scenario: Seller creates an auction then terminates it with bids (early sell)
            // it also showcases the seller notifications, marking one and then all as read and buyer payment
            Procedures.Login(driver, url, Username, Password);
            Procedures.CreateAuction(driver, url, auctionInput, 5);
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, BuyerUsername, BuyerPassword);
            Procedures.MakeBid(driver, url, 0);
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, buyerAccountInput2["Username"], buyerAccountInput2["Password"]);
            Procedures.MakeBid(driver, url, 0);
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, Username, Password);
            Procedures.TerminateAuction(driver, url);
            Procedures.MarkTopNotificationAsRead(driver, url);
            Procedures.MarkAllNotificationsAsRead(driver, url);
            Procedures.ShowAllNotifications(driver, url); // Show seller notifications
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, buyerAccountInput2["Username"], buyerAccountInput2["Password"]);
            Procedures.MakePayment(driver, url, paymentMethodIndex: 0);
            Procedures.DeleteAuction(Sql.GetLastAuctionId());
            Procedures.Logout(driver, url);

            // Third scenario: Seller creates an auction then waits for it to end (without bids)
            // it also showcases the seller extend auction option
            Procedures.Login(driver, url, Username, Password);
            Procedures.CreateAuction(driver, url, auctionInput, 1);
            Task.Delay(60 * 1000 - DateTime.UtcNow.Second * 1000).Wait(); // sleep until the beggining of the next minute
            Procedures.ExtendAuction(driver, url, 5);
            Task.Delay(2000).Wait();
            Procedures.DeleteAuction(Sql.GetLastAuctionId());
            Procedures.Logout(driver, url);

            // Fourth scenario: Seller creates an auction then waits for it to end (with bids)
            // it also showcases the buyer notifications and again buyer payment
            Procedures.Login(driver, url, Username, Password);
            Procedures.CreateAuction(driver, url, auctionInput, 1);
            DateTime timeStart = DateTime.UtcNow;
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, BuyerUsername, BuyerPassword);
            Procedures.MakeBid(driver, url, 0);
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, buyerAccountInput2["Username"], buyerAccountInput2["Password"]);
            Procedures.MakeBid(driver, url, 0);
            Procedures.Logout(driver, url);
            Procedures.Login(driver, url, BuyerUsername, BuyerPassword);
            Procedures.MakeBid(driver, url, 0);
            Procedures.ShowAllNotifications(driver, url); // Show buyer notifications
            Task.Delay(1000).Wait();
            driver.Navigate().GoToUrl(url + "auction?id=" + Sql.GetLastAuctionId());
            TimeSpan timeToWait = DateTime.UtcNow - timeStart;
            int delayDuration = (int)(timeToWait.TotalMilliseconds < 0 ? 0 : timeToWait.TotalMilliseconds);
            Task.Delay(delayDuration).Wait();
            Procedures.MakePayment(driver, url, paymentMethodIndex: 3);
            Procedures.DeleteAuction(Sql.GetLastAuctionId());

            // clean up
            Procedures.DeleteAccount(Username);
            Procedures.DeleteAccount(BuyerUsername);
            Procedures.DeleteAccount(buyerAccountInput2["Username"]);
            Assert.Pass();
        }

        [TearDown]
        public void TearDown()
        {
            driver?.Dispose();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            driver?.Dispose();
        }

        private static dynamic GetBrowserOptions(string browserName)
        {
            dynamic options = browserName switch
            {
                "Firefox" => new FirefoxOptions(),
                "Chrome" => new ChromeOptions(),
                "Edge" => new EdgeOptions(),
                "Safari" => new SafariOptions(),
                _ => new ChromeOptions(),
            };
            return options;
        }
    }
}
