using Ltwhqt.ViewModels.Warehouse;
using System.Collections.Generic;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class AdjustmentsController : WarehouseBaseController
    {
        public ActionResult Index()
        {
            return View(BuildAdjustments());
        }

        public ActionResult Create(long variantId = 0)
        {
            var variant = new WarehouseAdjustmentViewModel
            {
                VariantId = variantId == 0 ? 101 : variantId,
                Variant = "Sneaker Aurora / 39",
                CurrentQuantity = 48
            };
            return View(variant);
        }

        private static IList<WarehouseAdjustmentViewModel> BuildAdjustments()
        {
            return new List<WarehouseAdjustmentViewModel>
            {
                new WarehouseAdjustmentViewModel { VariantId = 101, Variant = "Aurora / 39", CurrentQuantity = 48, Adjustment = -2, Reason = "Hư hỏng khi kiểm hàng", CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-6), CreatedBy = "warehouse.thuy" },
                new WarehouseAdjustmentViewModel { VariantId = 202, Variant = "Varsity / L", CurrentQuantity = 6, Adjustment = +5, Reason = "Kiểm kê bổ sung", CreatedAt = System.DateTimeOffset.UtcNow.AddDays(-1), CreatedBy = "warehouse.admin" }
            };
        }
    }
}
