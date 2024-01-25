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
        private static readonly string ConfirmPassword = "password123";
        private static readonly string ProfilePic = "";
        private static readonly bool OptNewsletter = true;

        private readonly Dictionary<string, string> accountInput = new()
        {
            { "FullName", FullName },
            { "Username", Username },
            { "Email", Email },
            { "Password", Password },
            { "ConfirmPassword", Password },
            { "ProfilePic", ProfilePic },
        };

        // input data for the create auction test
        private static readonly string Title = "Automated Test Auction";
        private static readonly string Description = "This auction was created by an automated test";
        private static readonly string Price = "150.50";
        private static readonly string MinimumBid = "1.75";
        private static readonly string EndDate = DateTime.Now.AddMinutes(5).ToString("yyyy-MM-dd hh:mm");
        private static readonly string Images = ""; // TODO: add images links

        private readonly Dictionary<string, string> auctionInput = new()
        {
            { "Title", Title },
            { "Description", Description },
            { "Price", Price },
            { "MinimumBid", MinimumBid },
            { "EndDate", EndDate },
            { "Images", Images },
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

            // Selenium hub local connection
            // driver = new RemoteWebDriver(new Uri("http://localhost:4444/wd/hub"), capabilities);

            // LambdaTest connection
            driver = new RemoteWebDriver(new Uri("https://" + ltUserName + ":" + ltAppKey + "@hub.lambdatest.com/wd/hub"), capability);

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

            try
            {
                // Wait for the element containing the success message to be present
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement successMessageElement = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

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

            try
            {
                // Wait for the welcome message element to be present
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                IWebElement welcomeElement = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // Assert that the welcome text is present
                Assert.That(welcomeElement.Text, Does.Contain("Welcome"));
            }
            catch (TimeoutException)
            {
                // Handle the case where the redirect doesn't happen
                IWebElement loginMessageElement = driver.FindElement(By.CssSelector(".text-danger"));
                string loginMessage = loginMessageElement.Text;
                Assert.Fail("[FAIL ***Login*** test] Redirect to welcome page did not occur. " + loginMessage);
            }

            Assert.Pass();
        }

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

            Assert.Pass();
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
            Console.WriteLine("Edit auction requires logging in...");
            Procedures.Login(driver, url, Username, Password);

            Console.WriteLine("Creating an auction...");
            Procedures.CreateAuction(driver, url, auctionInput, 5);

            Dictionary<string, string> input = new()
            {
                { "Title", Title + " (Edited)" },
                { "Description", Description + " (Edited)" },
                { "MinimumBid", (int.Parse(MinimumBid) + 1).ToString() },
                { "Images", "" }, // TODO: edit images links
            };

            Procedures.EditAuction(driver, url, input);

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

            string searchQuery = "Test Auction";

            // Go to the homepage page
            Console.WriteLine("Going to the homepage page");

            driver.Navigate().GoToUrl(url);

            // Find elements by their "id" attribute value
            Console.WriteLine("Finding search bar by its ID attribute value");

            IWebElement searchInput = driver.FindElement(By.CssSelector("input[id='searchBar']"));
            searchInput.Clear();
            searchInput.SendKeys(searchQuery);

            // Submit the form
            Console.WriteLine("Submitting the form");

            // searchInput.Submit(); // FIX this by sending enter key
            searchInput.SendKeys(Keys.Enter);

            // sleep 1 second
            Task.Delay(1000).Wait();

            try
            {
                // Wait for the element indicating that we are on the search page
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
                IWebElement searchResults = wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

                // Assert that the header text contains "earch results"
                Assert.That(searchResults.Text, Does.Contain("earch results"));
            }
            catch (TimeoutException)
            {
                // Handle the case where the redirect doesn't happen
                Assert.Fail("[FAIL ***Search Auction*** test] No redirect to the search page");
            }

            Assert.Pass();
        }

        /*
        [Test, Order(7)]
        public void MainTest() {
            Procedures.Register(driver, url, accountInput, OptNewsletter);
            Procedures.Login(driver, url, Username, Password);
            // EditAccount(); TODO
            // ViewPrivateAccount(); TODO
            // ViewPublicAccount(); TODO
            Procedures.CreateAuction(driver, url, auctionInput, 5);
            EditAuction();
            SearchAuction();
            // SortByOptions(); TODO
            // TerminateAuction(); TODO
            // EarlySellAuction(); TODO
            // ExtendAuction(); TODO
            // MakeBid(); TODO
            // Payment(); TODO
            // Notifications related tests TODO
            // - MarkOneAsRead(); TODO
            // - MarkAllAsRead(); TODO
            Assert.Pass();
        }
        */

        [TearDown]
        public void TearDown()
        {
            driver?.Dispose();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Console.WriteLine("Deleting the account created...");
            Procedures.DeleteAccount(Username);

            Console.WriteLine("Deleting the auction created...");
            int auctionId = Sql.GetLastAuctionId();
            Sql.ExecuteQuery("DELETE FROM Auction WHERE AuctionId = '" + auctionId + "'");
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
