using Ltwhqt.ViewModels.Admin;
using System.Collections.Generic;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class BranchInventoriesController : AdminBaseController
    {
        public ActionResult Index()
        {
            return View(BuildBranchInventory());
        }

        private static IList<BranchInventoryViewModel> BuildBranchInventory()
        {
            return new List<BranchInventoryViewModel>
            {
                new BranchInventoryViewModel { BranchName = "Chi nhánh Quận 1", Variant = "Aurora / 40", QuantityOnHand = 18, QuantityReserved = 3 },
                new BranchInventoryViewModel { BranchName = "Chi nhánh Quận 1", Variant = "Varsity / L", QuantityOnHand = 6, QuantityReserved = 1 },
                new BranchInventoryViewModel { BranchName = "Chi nhánh Hà Đông", Variant = "Aurora / 39", QuantityOnHand = 12, QuantityReserved = 2 }
            };
        }
    }
}
