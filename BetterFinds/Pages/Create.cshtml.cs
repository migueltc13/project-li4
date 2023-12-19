using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFinds.Pages
{
    [Authorize]
    public class CreateModel : PageModel
    {
        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Descrition { get; set; } = "";

        [BindProperty]
        public double Price { get; set; } = 0;

        [BindProperty]
        public string Date { get; set; } = "";

        private readonly IConfiguration _configuration;
        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine($"Title: {Title}");
            Console.WriteLine($"Descrition: {Descrition}");
            Console.WriteLine($"Price: {Price}");
            Console.WriteLine($"Date: {Date}");

            // TODO

            return Task.FromResult<IActionResult>(Page());
        }
    }
}
