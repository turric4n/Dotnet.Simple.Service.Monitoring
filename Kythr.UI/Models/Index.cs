using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kythr.UI.Models
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            // SPA shell — all data is fetched client-side via SignalR and REST API
        }
    }
}