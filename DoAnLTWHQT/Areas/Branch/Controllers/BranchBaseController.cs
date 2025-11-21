using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    [Authorize(Roles = "branch_manager")]
    public abstract class BranchBaseController : Controller
    {
        protected override void OnException(ExceptionContext filterContext)
        {
            // TODO: add shared branch logging/handling
            base.OnException(filterContext);
        }
    }
}
