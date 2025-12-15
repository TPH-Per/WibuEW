using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    [Authorize(Roles = "warehouse_manager")]
    public abstract class WarehouseBaseController : Controller
    {
        protected override void OnException(ExceptionContext filterContext)
        {
            // TODO: add shared warehouse logging/handling
            base.OnException(filterContext);
        }
    }
}
