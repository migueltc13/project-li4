using OpenQA.Selenium;

namespace Tests.Utils
{
    internal class Procedures
    {
        /***************************************************
         **           Account related functions           **
         ***************************************************/
        public static void DeleteAccount(string username)
        {
            string query = "DELETE FROM Users WHERE Username = '" + username + "'";
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

            // Find elements by their "id" attribute value
            Console.WriteLine("Finding elements by their ID attribute value");

            IWebElement usernameInput = driver.FindElement(By.CssSelector("input[id='Username']"));
            IWebElement passwordInput = driver.FindElement(By.CssSelector("input[id='Password']"));

            // Fill in the form
            Console.WriteLine("Filling in the form");

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

        /***************************************************
         **           Auction related functions           **
         ***************************************************/
        public static void DeleteAuction(int auctionId)
        {
            string query = "DELETE FROM Auctions WHERE AuctionId = " + auctionId;
            Sql.ExecuteQuery(query);
        }

        public static void CreateAuction(IWebDriver driver, string url, Dictionary<string, string> input, int auctionEndTimeMinutesFromNow)
        {
            // Redefine the auction end time
            input["EndTime"] = DateTime.Now.AddMinutes(auctionEndTimeMinutesFromNow).ToString("yyyy-MM-dd HH:mm:ss");

            // Go to the create auction page
            Console.WriteLine("Going to the create auction page");
            driver.Navigate().GoToUrl(url + "create");

            // Find elements by their "id" attribute value
            Console.WriteLine("Finding elements by their ID attribute value and filling in the form");
            // NOTE: asp-for is converted to id by the framework
            foreach (KeyValuePair<string, string> entry in input)
            {
                IWebElement inputElement = driver.FindElement(By.CssSelector("input[id='" + entry.Key + "']"));
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
         **             Bid related functions             **
         ***************************************************/

        /***************************************************
         **           Search related functions            **
         ***************************************************/

        /***************************************************
         **          Sort by related functions            **
         ***************************************************/

        /***************************************************
         **        Notifications related functions        **
         ***************************************************/
    }
}
