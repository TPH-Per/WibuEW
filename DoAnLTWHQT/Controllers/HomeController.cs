using System.Web.Mvc;

namespace DoAnLTWHQT.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (User?.IsInRole("admin") ?? false)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (User?.IsInRole("warehouse_manager") ?? false)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Warehouse" });
            }

            if (User?.IsInRole("branch_manager") ?? false)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Branch" });
            }

            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction("Login", "Account");
            }

            // Ðã dang nh?p nhung không thu?c các role chính th?c,
            // dua v? dang xu?t d? tránh vòng l?p chuy?n hu?ng.
            return RedirectToAction("Logout", "Account");
        }
    }
}
