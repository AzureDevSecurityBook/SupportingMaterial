using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace YAFancyWebAppFixed.Pages
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
            if (Validate(username))
            {
                Message = "Welcome " + username + "!";
			    return Page();
            }
            else
            {
                SecurityLogger.Instance.Log("xss", username);
                return RedirectToPage("./NiceTry");
            }
        }

        private bool Validate(string username)
        {
            var regex = new Regex("^[a-zA-Z ]+$");
            return regex.IsMatch(username);
        }
	}
}
