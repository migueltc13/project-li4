using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    [Authorize]
    public class MyAccountModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string FullName { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        [BindProperty]
        public string ConfirmPassword { get; set; } = "";

        [BindProperty]
        public bool OptNewsletter { get; set; } = false;

        private readonly IConfiguration _configuration;
        public MyAccountModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            // pass message to page via action parameter: success=1
            if (Request.Query.ContainsKey("success"))
            {
                if (Request.Query["success"] == "1")
                    ViewData["Message"] = "Account updated successfully.";
            }

            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int ClientId = clientUtils.GetClientId(HttpContext, User);
            ViewData["ClientId"] = ClientId;

            // Get user info from database
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            string query = "SELECT * FROM Client WHERE ClientId = @ClientId";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", ClientId);

                    SqlDataReader reader = cmd.ExecuteReader();
                    reader.Read();
                    ViewData["Username"] = reader.GetString(reader.GetOrdinal("Username"));
                    ViewData["FullName"] = reader.GetString(reader.GetOrdinal("FullName"));
                    ViewData["Email"] = reader.GetString(reader.GetOrdinal("Email"));
                    ViewData["OptNewsletter"] = reader.GetBoolean(reader.GetOrdinal("OptNewsletter")) ? "Yes" : "No";
                    ViewData["SubscribeMessage"] = reader.GetBoolean(reader.GetOrdinal("OptNewsletter")) ? "Unsubscribe from our newsletter" : "Subscribe to our newsletter";
                    reader.Close();
                }
                con.Close();
            }

            // Get number of auctions
            query = "SELECT COUNT(*) AS NumberOfAuctions FROM Auction WHERE ClientId = @ClientId";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", ClientId);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                        ViewData["NumAuctions"] = reader.GetInt32(reader.GetOrdinal("NumberOfAuctions"));
                    reader.Close();
                }
                con.Close();
            }

            // Get number of bids
            query = "SELECT COUNT(*) AS NumberOfBids FROM Bid WHERE ClientId = @ClientId";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClientId", ClientId);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                        ViewData["NumBids"] = reader.GetInt32(reader.GetOrdinal("NumberOfBids"));
                    reader.Close();
                }
                con.Close();
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            Console.WriteLine("Username: " + Username);
            Console.WriteLine("FullName: " + FullName);
            Console.WriteLine("Email: " + Email);
            Console.WriteLine("Password: " + Password);
            Console.WriteLine("ConfirmPassword: " + ConfirmPassword);
            Console.WriteLine("OptNewsletter: " + OptNewsletter);

            ModelState.Clear();

            // update values status
            bool updateUsername = false;
            bool updateFullName = false;
            bool updateEmail = false;
            bool updatePassword = false;
            bool updateOptNewsletter = false;

            // Check if username is at least 3 characters long and 32 characters or fewer
            if (Username == null || Username == "")
            {
                updateUsername = false;
            }
            else if (Username.Length < 3 || Username.Length > 32)
            {
                ModelState.AddModelError(string.Empty, "Username must be at least 3 characters long and 32 characters or fewer.");
                return Page();
            }
            else
                updateUsername = true;

            // Check if full name is at least 3 characters long and 64 characters or fewer
            if (FullName == null || FullName == "")
            {
                updateFullName = false;
            }
            else if (FullName.Length < 3 || FullName.Length > 64)
            {
                ModelState.AddModelError(string.Empty, "Full name must be at least 3 characters long and 64 characters or fewer.");
                return Page();
            }
            else
                updateFullName = true;

            // Check if email is at least 5 characters long and 320 characters or fewer
            if (Email == null || Email == "")
            {
                updateEmail = false;
            }
            else if (Email.Length < 5 || Email.Length > 320)
            {
                ModelState.AddModelError(string.Empty, "Email must be at least 3 characters long and 320 characters or fewer.");
                return Page();
            }
            else
                updateEmail = true;

            // Check if passwords match
            if ((Password == null && ConfirmPassword == null) || (Password == "" && ConfirmPassword == ""))
            {
                updatePassword = false;
            }
            else if (Password != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return Page();
            }
            else
                updatePassword = true;

            // Check if password is at least 8 characters long and 64 characters or fewer
            if (Password == null || Password == "")
            {
                updatePassword = false;
            }
            else if (Password.Length < 8 || Password.Length > 64)
            {
                ModelState.AddModelError(string.Empty, "Password must be at least 8 characters long and 64 characters or fewer.");
                return Page();
            }
            else if (!updatePassword)
                updatePassword = true;

            // Check if optNewsletter needs to be updated
            if (OptNewsletter)
                updateOptNewsletter = true;

            // Get ClientId
            var clientUtils = new Utils.Client(_configuration);
            int ClientId = clientUtils.GetClientId(HttpContext, User);

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Check if username already exists
                if (updateUsername)
                {
                    string queryCheckUser = "SELECT * FROM Client WHERE Username = @Username AND ClientId != @ClientId";
                    using (SqlCommand cmdCheckUser = new SqlCommand(queryCheckUser, con))
                    {
                        cmdCheckUser.Parameters.AddWithValue("@Username", Username);
                        cmdCheckUser.Parameters.AddWithValue("@ClientId", ClientId);
                        SqlDataReader reader = cmdCheckUser.ExecuteReader();
                        if (reader.Read())
                        {
                            ModelState.AddModelError(string.Empty, "Username already exists.");
                            return Page();
                        }
                        reader.Close();
                    }
                }

                // Check if email already exists
                if (updateEmail)
                {
                    string queryCheckEmail = "SELECT * FROM Client WHERE Email = @Email AND ClientId != @ClientId";
                    using (SqlCommand cmdCheckEmail = new SqlCommand(queryCheckEmail, con))
                    {
                        cmdCheckEmail.Parameters.AddWithValue("@Email", Email);
                        cmdCheckEmail.Parameters.AddWithValue("@ClientId", ClientId);
                        SqlDataReader reader = cmdCheckEmail.ExecuteReader();
                        if (reader.Read())
                        {
                            ModelState.AddModelError(string.Empty, "Email already exists.");
                            return Page();
                        }
                        reader.Close();
                    }
                }

                // Get current optNewsletter value
                bool currentOptNewsletter = false;
                if (updateOptNewsletter)
                {
                    string queryGetOptNewsletter = "SELECT OptNewsletter FROM Client WHERE ClientId = @ClientId";
                    using (SqlCommand cmdGetOptNewsletter = new SqlCommand(queryGetOptNewsletter, con))
                    {
                        cmdGetOptNewsletter.Parameters.AddWithValue("@ClientId", ClientId);
                        SqlDataReader reader = cmdGetOptNewsletter.ExecuteReader();
                        reader.Read();
                        currentOptNewsletter = reader.GetBoolean(reader.GetOrdinal("OptNewsletter"));
                        reader.Close();
                    }
                }

                // Conscruct query
                string query = "UPDATE Client SET ";
                if (updateUsername)
                    query += "Username = @Username, ";
                if (updateFullName)
                    query += "FullName = @FullName, ";
                if (updateEmail)
                    query += "Email = @Email, ";
                if (updatePassword)
                    query += "Password = @Password, ";
                if (updateOptNewsletter)
                    query += "OptNewsletter = @OptNewsletter, ";
                query = query.Remove(query.Length - 2); // Remove last comma and space
                query += " WHERE ClientId = @ClientId";

                Console.WriteLine(query);
                Console.WriteLine("updateUsername: " + updateUsername);
                Console.WriteLine("updateFullName: " + updateFullName);
                Console.WriteLine("updateEmail: " + updateEmail);
                Console.WriteLine("updatePassword: " + updatePassword);
                Console.WriteLine("updateOptNewsletter: " + updateOptNewsletter);

                // Update user info
                using (SqlCommand cmdUpdateUser = new SqlCommand(query, con))
                {
                    if (updateUsername)
                        cmdUpdateUser.Parameters.AddWithValue("@Username", Username);
                    if (updateFullName)
                        cmdUpdateUser.Parameters.AddWithValue("@FullName", FullName);
                    if (updateEmail)
                        cmdUpdateUser.Parameters.AddWithValue("@Email", Email);
                    if (updatePassword)
                        cmdUpdateUser.Parameters.AddWithValue("@Password", Password);
                    if (updateOptNewsletter) // Todo toggle newsletter if OptNewsletter is true
                        cmdUpdateUser.Parameters.AddWithValue("@OptNewsletter", !currentOptNewsletter);
                    cmdUpdateUser.Parameters.AddWithValue("@ClientId", ClientId);
                    cmdUpdateUser.ExecuteNonQuery();
                }

                con.Close();
            }

            // pass message to page via action parameter: success=1
            return RedirectToPage("MyAccount", new { success = 1 });
        }
    }
}
