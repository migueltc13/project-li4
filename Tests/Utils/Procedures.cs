using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Tests.Utils
{
    internal class Procedures
    {
        /***************************************************
         **           Account related functions           **
         ***************************************************/
        public static void DeleteAccount(string username)
        {
            string query = "DELETE FROM Client WHERE Username = '" + username + "'";
            Sql.ExecuteQuery(query);
        }

        public static void Register(IWebDriver driver, string url, Dictionary<string, string> input, bool optNewsletter)
        {
            // Go to the register page
            Console.WriteLine("Going to the register page");
            driver.Navigate().GoToUrl(url + "register");

            // Find elements by their "id" attribute value
            Console.WriteLine("Finding elements by their ID attribute value and filling in the form");
            // NOTE: asp-for is converted to id by the framework
            foreach (KeyValuePair<string, string> entry in input)
            {
                IWebElement inputElement = driver.FindElement(By.CssSelector("input[id='" + entry.Key + "']"));
                inputElement.SendKeys(entry.Value);
            }
            IWebElement optNewsletterCheckbox = driver.FindElement(By.CssSelector("input[id='OptNewsletter']"));
            if (optNewsletterCheckbox.Selected != optNewsletter)
                optNewsletterCheckbox.Click();

            // Submit the form
            Console.WriteLine("Submitting the form");
            IWebElement registerButton = driver.FindElement(By.CssSelector("button.btn"));
            registerButton.Click();
        }

        public static void Login(IWebDriver driver, string url, string username, string password)
        {
            // Go to the login page
            Console.WriteLine("Going to the login page");

            driver.Navigate().GoToUrl(url + "login");

            // Sleep until the page is loaded
            Console.WriteLine("Sleeping until the page is loaded");
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.FindElement(By.TagName("body")));
            Task.Delay(1000).Wait();

            // Find elements by their "id" attribute value
            Console.WriteLine("Finding elements by their ID attribute value");

            IWebElement usernameInput = driver.FindElement(By.CssSelector("input[id='Username']"));
            IWebElement passwordInput = driver.FindElement(By.CssSelector("input[id='Password']"));

            // Fill in the form
            Console.WriteLine("Filling in the form");

            usernameInput.Clear();
            usernameInput.SendKeys(username);
            passwordInput.SendKeys(password);

            // Submit the form
            Console.WriteLine("Submitting the form");

            passwordInput.Submit();
        }

        public static void Logout(IWebDriver driver, string url)
        {
            // Go to the logout page
            Console.WriteLine("Going to the logout page");

            driver.Navigate().GoToUrl(url + "logout");
        }

        public static void EditAccount(IWebDriver driver, string url, Dictionary<string, string> input)
        {
            // Go to the edit account page
            Console.WriteLine("Going to the my account page");
            driver.Navigate().GoToUrl(url + "myaccount");

            // Sleep until the page is loaded
            Console.WriteLine("Sleeping until the page is loaded");
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.FindElement(By.TagName("body")));
            Task.Delay(1000).Wait();

            // Open the edit account collapsible
            Console.WriteLine("Opening the edit account collapsible");
            IWebElement details = driver.FindElement(By.TagName("details"));
            details.Click();

            // Find elements by their "id" attribute value
            Console.WriteLine("Finding elements by their ID attribute value and filling in the form");
            foreach (KeyValuePair<string, string> entry in input)
            {
                IWebElement inputElement = driver.FindElement(By.CssSelector("input[id='" + entry.Key + "']"));
                inputElement.Clear();
                inputElement.SendKeys(entry.Value);
            }

            // Submit the form
            Console.WriteLine("Submitting the form");
            IWebElement submitButton = driver.FindElement(By.CssSelector("button.btn"));
            submitButton.Click();
        }

        /***************************************************
         **           Auction related functions           **
         ***************************************************/
        public static void DeleteAuction(int auctionId)
        {
            string query = "DELETE FROM Auction WHERE AuctionId = " + auctionId;
            Sql.ExecuteQuery(query);

            query = "DELETE FROM Product WHERE AuctionId = " + auctionId;
            Sql.ExecuteQuery(query);

            query = "DELETE FROM Bid WHERE AuctionId = " + auctionId;
            Sql.ExecuteQuery(query);

            query = "DELETE FROM Notification WHERE AuctionId = " + auctionId;
            Sql.ExecuteQuery(query);
        }

        public static void CreateAuction(IWebDriver driver, string url, Dictionary<string, string> input, int auctionEndTimeMinutesFromNow)
        {
            // Redefine the auction end time
            input["EndTime"] = DateTime.UtcNow.AddMinutes(auctionEndTimeMinutesFromNow).ToString("MM-dd-yyyyyy hh:mm tt");

            // Go to the create auction page
            Console.WriteLine("Going to the create auction page");
            driver.Navigate().GoToUrl(url + "create");

            // Sleep until the page is loaded
            Console.WriteLine("Sleeping until the page is loaded");
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.FindElement(By.TagName("body")));
            Task.Delay(1000).Wait();

            // Find elements by their "id" attribute value
            Console.WriteLine("Finding elements by their ID attribute value and filling in the form");
            // NOTE: asp-for is converted to id by the framework
            foreach (KeyValuePair<string, string> entry in input)
            {
                IWebElement inputElement;
                if (entry.Key == "Description")
                    inputElement = driver.FindElement(By.CssSelector("textarea[id='" + entry.Key + "']"));
                else
                    inputElement = driver.FindElement(By.CssSelector("input[id='" + entry.Key + "']"));

                inputElement.Clear();
                inputElement.SendKeys(entry.Value);
            }

            // Submit the form
            Console.WriteLine("Submitting the form");
            IWebElement createButton = driver.FindElement(By.CssSelector("button.btn"));
            createButton.Click();
        }

        public static void EditAuction(IWebDriver driver, string url, Dictionary<string, string> input)
        {
            // Go to the edit auction page
            Console.WriteLine("Going to the edit auction page");

            int AuctionId = Sql.GetLastAuctionId();
            driver.Navigate().GoToUrl(url + "edit?id=" + AuctionId);

            Console.WriteLine("Finding elements by their ID and filling in the form");
            foreach (KeyValuePair<string, string> entry in input)
            {
                IWebElement inputElement;
                if (entry.Key == "Description")
                    inputElement = driver.FindElement(By.CssSelector("textarea[id='" + entry.Key + "']"));
                else
                    inputElement = driver.FindElement(By.CssSelector("input[id='" + entry.Key + "']"));

                inputElement.Clear();
                inputElement.SendKeys(entry.Value);
            }

            // Submit the form
            Console.WriteLine("Submitting the form");
            IWebElement submitButton = driver.FindElement(By.CssSelector("button.btn"));
            submitButton.Click();
        }

        // Both used to terminate an auction and perform an early sell an auction
        // depending on the ammount of bids
        public static void TerminateAuction(IWebDriver driver, string url)
        {
            // Get the last auction id
            int AuctionId = Sql.GetLastAuctionId();

            // Go to the auction page
            Console.WriteLine("Going to the auction page");
            driver.Navigate().GoToUrl(url + "auction?id=" + AuctionId);

            // Sleep until the page is loaded
            Console.WriteLine("Sleeping until the page is loaded");
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.FindElement(By.TagName("body")));
            Task.Delay(1000).Wait();

            // Find terminate/sell button
            Console.WriteLine("Pressing the terminate/sell button");
            IWebElement secondFormButton = driver.FindElements(By.CssSelector("button.btn"))[1];
            secondFormButton.Click();
        }

        public static void ExtendAuction(IWebDriver driver, string url, int extendTimeMinutes)
        {
            // Get the last auction id
            int AuctionId = Sql.GetLastAuctionId();

            // Go to the auction page
            Console.WriteLine("Going to the auction page");
            driver.Navigate().GoToUrl(url + "auction?id=" + AuctionId);

            // Sleep until the page is loaded
            Console.WriteLine("Sleeping until the page is loaded");
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.FindElement(By.TagName("body")));
            Task.Delay(1000).Wait();

            // Find ExtendedEndTime input
            Console.WriteLine("Finding the ExtendedEndTime input");
            IWebElement extendedEndTimeInput = driver.FindElement(By.CssSelector("input[id='ExtendedEndTime']"));

            // Redefine the auction end time
            string extendedEndTime = DateTime.UtcNow.AddMinutes(extendTimeMinutes).ToString("MM-dd-yyyyyy hh:mm tt");
            extendedEndTimeInput.Clear();
            extendedEndTimeInput.SendKeys(extendedEndTime);

            // Find extend button
            Console.WriteLine("Pressing the extend button");
            IWebElement extendButton = driver.FindElement(By.CssSelector("button.btn"));
            extendButton.Click();
        }

        // int paymentMethodIndex:
        // 0: CreditCard
        // 1: PayPal
        // 2: ApplePay
        // 3: CryptoCurrency
        public static void MakePayment(IWebDriver driver, string url, int paymentMethodIndex)
        {
            // Get the last auction id
            int AuctionId = Sql.GetLastAuctionId();

            // Go to the auction page
            Console.WriteLine("Going to the auction page");
            driver.Navigate().GoToUrl(url + "auction?id=" + AuctionId);

            // Sleep until the page is loaded
            Console.WriteLine("Sleeping until the page is loaded");
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.FindElement(By.TagName("body")));
            Task.Delay(1000).Wait();

            // Select the payment method
            Console.WriteLine("Selecting the payment method");
            IWebElement paymentMethod = driver.FindElement(By.Id("PaymentMethod"));
            SelectElement select = new SelectElement(paymentMethod);
            // Select options
            select.SelectByIndex(paymentMethodIndex); // Select by index (0 based)
            // select.SelectByText("PayPal"); // Select by text
            // select.SelectByValue("PayPal"); // Select by value

            // Find pay button
            Console.WriteLine("Pressing the pay button");
            IWebElement payButton = driver.FindElement(By.CssSelector("button.btn"));
            payButton.Click();
        }

        /***************************************************
         **             Bid related functions             **
         ***************************************************/
        public static void MakeBid(IWebDriver driver, string url, decimal bidAmount)
        {
            // Go to the auction page
            int auctionId = Sql.GetLastAuctionId();
            Console.WriteLine("Going to the auction page");
            driver.Navigate().GoToUrl(url + "auction?id=" + auctionId);

            // Entering the bid ammount if it isn't 0 (otherwise we just click the submit button using the default bid value)
            if (bidAmount != 0)
            {
                // NOTE: asp-for is converted to id by the framework in this case the id was already used to use javascript
                // in order to update bid button value functionality so we use the name attribute as a fallback from the asp-for attribute
                IWebElement bidInput = driver.FindElement(By.CssSelector("input[name='BidAmount']"));
                Console.WriteLine("Entering the bid ammount");
                bidInput.Clear();
                bidInput.SendKeys(bidAmount.ToString());
            }

            // Submitting the bid
            IWebElement submitButton = driver.FindElement(By.CssSelector("button.btn"));
            Console.WriteLine("Submitting the bid");
            submitButton.Click();
        }

        /***************************************************
         **       Search and sort related functions       **
         ***************************************************/
        public static void Search(IWebDriver driver, string url, string query)
        {
            // Go to the homepage page
            Console.WriteLine("Going to the homepage page");
            driver.Navigate().GoToUrl(url);

            // Find elements by their "id" attribute value
            Console.WriteLine("Finding search bar by its ID attribute value and filling it in");
            IWebElement searchInput = driver.FindElement(By.CssSelector("input[id='searchBar']"));
            searchInput.Clear();
            searchInput.SendKeys(query);

            // Submit the form
            Console.WriteLine("Submitting the form");
            // searchInput.Submit(); // FIX: by sending enter key instead of submit method
            searchInput.SendKeys(Keys.Enter);
        }

        // int sortByIndex:
        // 0: Ending time (Ascending)
        // 1: Ending time (Descending)
        // 2: Product name (A-Z)
        // 3: Product name (Z-A)
        // 4: Product Price (Lowest first)
        // 5: Product Price (Highest first)
        //
        // int occurringIndex:
        // 0: Occurring
        // 1: All
        public static void SortBy(IWebDriver driver, int sortByIndex, int occurringIndex)
        {
            // Find the sort by dropdown
            Console.WriteLine("Finding the sort by dropdown");
            IWebElement sortByDropdown = driver.FindElement(By.Id("sort"));

            // Find the occurring dropdown
            Console.WriteLine("Finding the occurring dropdown");
            IWebElement occurringDropdown = driver.FindElement(By.Id("occurring"));

            // Select the sort by option
            Console.WriteLine("Selecting the sort by option");
            SelectElement selectSortBy = new SelectElement(sortByDropdown);
            selectSortBy.SelectByIndex(sortByIndex);

            // Sleep until the page is loaded
            Console.WriteLine("Sleeping until the page is loaded");
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
            wait.Until(driver => driver.FindElement(By.TagName("body")));
            Task.Delay(1000).Wait();

            // Select the occurring option
            Console.WriteLine("Selecting the occurring option");
            SelectElement selectOccurring = new SelectElement(occurringDropdown);
            selectOccurring.SelectByIndex(occurringIndex);
        }

        /***************************************************
         **        Notifications related functions        **
         ***************************************************/
        public static void ViewNotifications(IWebDriver driver, string url)
        {
            // Go to the notifications page
            Console.WriteLine("Going to the notifications page");
            driver.Navigate().GoToUrl(url + "notifications");
        }

        public static void MarkTopNotificationAsRead(IWebDriver driver, string url)
        {
            // Go to the notifications page
            Console.WriteLine("Going to the notifications page");
            driver.Navigate().GoToUrl(url + "notifications");

            // Mark the top notification as read
            Console.WriteLine("Marking the top notification as read");
            IWebElement markAsReadTag = driver.FindElements(By.CssSelector("p.notification-unread a"))[1];
            markAsReadTag.Click();
        }

        public static void MarkAllNotificationsAsRead(IWebDriver driver, string url)
        {
            // Go to the notifications page
            Console.WriteLine("Going to the notifications page");
            driver.Navigate().GoToUrl(url + "notifications");

            // Find the mark all as read button
            Console.WriteLine("Finding the mark all as read button");
            IWebElement markAllAsReadButton = driver.FindElements(By.CssSelector("button.btn"))[1];
            markAllAsReadButton.Click();
        }

        public static void ShowAllNotifications(IWebDriver driver, string url)
        {
            // Go to the notifications page
            Console.WriteLine("Going to the notifications page");
            driver.Navigate().GoToUrl(url + "notifications");

            // Find the show all notifications button
            Console.WriteLine("Finding the show all notifications button");
            IWebElement showAllNotificationsButton = driver.FindElements(By.CssSelector("button.btn"))[0];
            showAllNotificationsButton.Click();

            // Scroll slowly to the bottom of the page
            Console.WriteLine("Scrolling slowly to the bottom of the page");
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            for (int i = 0; i < 10; i++)
            {
                js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight)");
                Task.Delay(500).Wait();
            }
        }
    }
}
