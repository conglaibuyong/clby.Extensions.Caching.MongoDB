using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace clby.Tests.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            HttpContext.Session.SetString("a", "a");
            HttpContext.Session.SetString("b", "b");
            return this.Content(HttpContext.Session.GetString("a") + HttpContext.Session.GetString("b"));
        }

    }
}
