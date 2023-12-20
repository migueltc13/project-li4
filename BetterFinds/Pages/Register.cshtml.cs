using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BetterFinds.Pages
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public string FullName { get; set; } = "";

        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        [BindProperty]
        public string ConfirmPassword { get; set; } = "";

        [BindProperty]
        public bool OptNewsletter { get; set; } = false;

        private readonly IConfiguration _configuration;
        public RegisterModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public Task<IActionResult> OnPostAsync()
        {
            // For debugging purposes
            Console.WriteLine($"FullName: {FullName}");                 // TODO: Remove this
            Console.WriteLine($"Username: {Username}");                 // TODO: Remove this
            Console.WriteLine($"Email: {Email}");                       // TODO: Remove this
            Console.WriteLine($"Password: {Password}");                 // TODO: Remove this
            Console.WriteLine($"ConfirmPassword: {ConfirmPassword}");   // TODO: Remove this
            Console.WriteLine($"OptNewsletter: {OptNewsletter}");       // TODO: Remove this

            // Check if passwords match
            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return Task.FromResult<IActionResult>(Page());
            }

            // Check if password is at least 8 characters long
            if (Password.Length < 8)
            {
                ModelState.AddModelError(string.Empty, "Password must be at least 8 characters long.");
                return Task.FromResult<IActionResult>(Page());
            }

            // Check if username is at least 3 characters long
            if (Username.Length < 3)
            {
                ModelState.AddModelError(string.Empty, "Username must be at least 3 characters long.");
                return Task.FromResult<IActionResult>(Page());
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            SqlConnection con = new SqlConnection(connectionString);

            try
            { 
                // TODO DUVIDA: É necessário con.Close() antes de returns?
                con.Open();

                // Check if username already exists
                string queryCheckUser = "SELECT * FROM User WHERE Username = @Username";
                SqlCommand cmdCheckUser = new SqlCommand(queryCheckUser, con);
                cmdCheckUser.Parameters.AddWithValue("@Username", Username);
                int usernameCount = Convert.ToInt32(cmdCheckUser.ExecuteScalar());
                if (usernameCount > 0)
                {
                    ModelState.AddModelError(string.Empty, "Username already exists.");
                    return Task.FromResult<IActionResult>(Page());
                }

                // Check if email already exists
                string queryCheckEmail = "SELECT * FROM User WHERE Email = @Email";
                SqlCommand cmdCheckEmail = new SqlCommand(queryCheckEmail, con);
                cmdCheckEmail.Parameters.AddWithValue("@Email", Email);
                int emailCount = Convert.ToInt32(cmdCheckEmail.ExecuteScalar());
                if (emailCount > 0)
                {
                    ModelState.AddModelError(string.Empty, "Email already exists.");
                    return Task.FromResult<IActionResult>(Page());
                }

                // Get id of last user
                string queryId = "SELECT MAX(UserId) FROM User";
                SqlCommand cmdId = new SqlCommand(queryId, con);
                int id = Convert.ToInt32(cmdId.ExecuteScalar()) + 1;

                Console.WriteLine($"Id: {id}"); // TODO: Remove this

                string query = "INSERT INTO User (UserId, FullName, Username, Email, Password, OptNewsletter) VALUES (@id, @FullName, @Username, @Email, @Password, @OptNewsletter)";
                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@FullName", FullName);
                cmd.Parameters.AddWithValue("@Username", Username);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@Password", Password);
                cmd.Parameters.AddWithValue("@OptNewsletter", OptNewsletter);

                int result = cmd.ExecuteNonQuery();
                if (result == 1)
                {
                    Console.WriteLine("User created"); // TODO: Remove this
                    return Task.FromResult<IActionResult>(RedirectToPage("/Login"));
                }
                else
                {
                    Console.WriteLine("User not created"); // TODO: Remove this
                    return Task.FromResult<IActionResult>(RedirectToPage("/Register"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Task.FromResult<IActionResult>(RedirectToPage("/Register"));
            }
            finally
            {
                con.Close();
            }   
        }
    }
}
