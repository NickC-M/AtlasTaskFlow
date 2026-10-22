using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace NickCM_AtlasTaskFlow.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
