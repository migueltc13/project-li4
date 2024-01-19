using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace BetterFinds.Pages;

public partial class RegisterModel(IConfiguration configuration) : PageModel
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
    public string? ProfilePic { get; set; }

    [BindProperty]
    public bool OptNewsletter { get; set; } = false;

    public IActionResult OnGet()
    {
        // If user is already logged in, redirect to index page
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectToPage("index");

        return Page();
    }

    public IActionResult OnPost()
    {
        // For debugging purposes
        Console.WriteLine($"FullName: {FullName}");
        Console.WriteLine($"Username: {Username}");
        Console.WriteLine($"Email: {Email}");
        Console.WriteLine($"Password: {Password}");
        Console.WriteLine($"ConfirmPassword: {ConfirmPassword}");
        Console.WriteLine($"ProfilePic: {ProfilePic}");
        Console.WriteLine($"OptNewsletter: {OptNewsletter}");

        // Check if username is at least 3 characters long
        if (Username.Length < 3)
        {
            ModelState.AddModelError(string.Empty, "Username must be at least 3 characters long.");
            return Page();
        }

        // Check if username is 32 characters or fewer
        if (Username.Length > 32)
        {
            ModelState.AddModelError(string.Empty, "Username must be 32 characters or fewer.");
            return Page();
        }

        // Check if username contains only alphanumeric characters
        if (!UsernameRegex().IsMatch(Username))
        {
            ModelState.AddModelError(string.Empty, "Username must contain only alphanumeric characters.");
            return Page();
        }

        // Check if passwords match
        if (Password != ConfirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return Page();
        }

        // Check if password is at least 8 characters long
        if (Password.Length < 8)
        {
            ModelState.AddModelError(string.Empty, "Password must be at least 8 characters long.");
            return Page();
        }

        // Check if password is 64 characters or fewer
        if (Password.Length > 64)
        {
            ModelState.AddModelError(string.Empty, "Password must be 64 characters or fewer.");
            return Page();
        }

        // Check if email is at least 5 characters long
        if (Email.Length < 5)
        {
            ModelState.AddModelError(string.Empty, "Email must be at least 5 characters long.");
            return Page();
        }

        // Check if email is 320 characters or fewer
        if (Email.Length > 320)
        {
            ModelState.AddModelError(string.Empty, "Email must be 320 characters or fewer.");
            return Page();
        }

        // Check if full name is at least 3 characters long
        if (FullName.Length < 3)
        {
            ModelState.AddModelError(string.Empty, "Full name must be at least 3 characters long.");
            return Page();
        }

        // Check if full name is 64 characters or fewer
        if (FullName.Length > 64)
        {
            ModelState.AddModelError(string.Empty, "Full name must be 64 characters or fewer.");
            return Page();
        }

        // Check if full name contains only alphabetical characters (and spaces)
        if (!FullNameRegex().IsMatch(FullName))
        {
            ModelState.AddModelError(string.Empty, "Full name must contain only alphabetical characters.");
            return Page();
        }

        // Check if profile picture is 256 characters or fewer (if not null)
        if (ProfilePic != null && ProfilePic.Length > 256)
        {
            ModelState.AddModelError(string.Empty, "Profile picture must be 256 characters or fewer.");
            return Page();
        }

        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        SqlConnection con = new(connectionString);

        try
        {
            con.Open();

            // Check if username already exists
            string queryCheckUser = "SELECT * FROM Client WHERE Username = @Username";
            SqlCommand cmdCheckUser = new(queryCheckUser, con);
            cmdCheckUser.Parameters.AddWithValue("@Username", Username);
            int usernameCount = Convert.ToInt32(cmdCheckUser.ExecuteScalar());
            if (usernameCount > 0)
            {
                ModelState.AddModelError(string.Empty, "Username already exists.");
                con.Close();
                return Page();
            }

            // Check if email already exists
            string queryCheckEmail = "SELECT * FROM Client WHERE Email = @Email";
            SqlCommand cmdCheckEmail = new(queryCheckEmail, con);
            cmdCheckEmail.Parameters.AddWithValue("@Email", Email);
            int emailCount = Convert.ToInt32(cmdCheckEmail.ExecuteScalar());
            if (emailCount > 0)
            {
                ModelState.AddModelError(string.Empty, "Email already exists.");
                con.Close();
                return Page();
            }

            // Get clientId
            string queryId = "SELECT MAX(ClientId) FROM Client";
            SqlCommand cmdId = new(queryId, con);
            var result = cmdId.ExecuteScalar();
            int id = result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;

            Console.WriteLine($"Id: {id}");

            // Insert new user into database
            string query = "INSERT INTO Client (ClientId, FullName, Username, Email, Password, ProfilePic, OptNewsletter) VALUES (@id, @FullName, @Username, @Email, @Password, @ProfilePic, @OptNewsletter)";
            SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@FullName", FullName);
            cmd.Parameters.AddWithValue("@Username", Username);
            cmd.Parameters.AddWithValue("@Email", Email);
            cmd.Parameters.AddWithValue("@Password", Password);
            cmd.Parameters.AddWithValue("@ProfilePic", ProfilePic != null ? ProfilePic : DBNull.Value);
            cmd.Parameters.AddWithValue("@OptNewsletter", OptNewsletter);

            int result = cmd.ExecuteNonQuery();

            Console.WriteLine("User created");
            ViewData["RegisterMessage"] = "Account created successfully.";
            con.Close();
            return RedirectToPage("login", new { success = 1, username = Username });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return RedirectToPage("register");
        }
        finally
        {
            con.Close();
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9]+$")]
    private static partial Regex UsernameRegex();

    [GeneratedRegex(@"^[a-zA-Z ]+$")]
    private static partial Regex FullNameRegex();
}
