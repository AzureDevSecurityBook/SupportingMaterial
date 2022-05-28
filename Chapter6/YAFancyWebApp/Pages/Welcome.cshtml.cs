using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YAFancyWebApp.Pages
{
    public class WelcomeModel : PageModel
    {
        public string? Message { get; set; }

        public void OnGet()
        {
        }

		public IActionResult OnPost()
		{
            var username = Request.Form["Username"];
            Message = "Welcome " + username + "!";
			return Page();
		}
	}
}
