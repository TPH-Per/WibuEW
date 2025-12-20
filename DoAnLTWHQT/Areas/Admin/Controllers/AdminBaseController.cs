using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public abstract class AdminBaseController : Controller
    {
        protected override void OnException(ExceptionContext filterContext)
        {
            // TODO: log l?i chung cho admin
            base.OnException(filterContext);
        }
    }
}
