using Ltwhqt.ViewModels.Admin;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class InventoriesController : AdminBaseController
    {
        public ActionResult Index()
        {
            return View(BuildInventory());
        }

        public ActionResult Adjust(long variantId)
        {
            var variant = BuildInventory().FirstOrDefault(v => v.VariantId == variantId) ?? BuildInventory().First();
            ViewBag.Title = "Điều chỉnh tồn kho";
            return View(new InventoryAdjustmentViewModel
            {
                VariantId = variantId,
                VariantName = variant.VariantName,
                CurrentQuantity = variant.QuantityOnHand
            });
        }

        private static IList<InventorySnapshotViewModel> BuildInventory()
        {
            return new List<InventorySnapshotViewModel>
            {
                new InventorySnapshotViewModel { VariantId = 101, VariantName = "Sneaker Aurora / 39", WarehouseName = "Kho trung tâm", QuantityOnHand = 52, QuantityReserved = 6, ReorderLevel = 20 },
                new InventorySnapshotViewModel { VariantId = 202, VariantName = "Áo khoác Varsity / L", WarehouseName = "Kho trung tâm", QuantityOnHand = 18, QuantityReserved = 10, ReorderLevel = 25 },
                new InventorySnapshotViewModel { VariantId = 303, VariantName = "Balo Transit / Đen", WarehouseName = "Kho trung tâm", QuantityOnHand = 8, QuantityReserved = 2, ReorderLevel = 10 }
            };
        }
    }
}
