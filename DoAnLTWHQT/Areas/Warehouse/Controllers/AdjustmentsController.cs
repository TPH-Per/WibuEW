using System.Collections.Generic;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Warehouse;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class AdjustmentsController : WarehouseBaseController
    {
        public ActionResult Index()
        {
            return View(BuildAdjustments());
        }

        // GET: Warehouse/Adjustments/Create
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

        // POST: Warehouse/Adjustments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(WarehouseAdjustmentViewModel model)
        {
            try
            {
                // Validation
                if (model.Adjustment == 0)
                {
                    return Json(new { success = false, message = "Số lượng điều chỉnh không được bằng 0" });
                }

                if (string.IsNullOrWhiteSpace(model.Reason))
                {
                    return Json(new { success = false, message = "Vui lòng nhập lý do điều chỉnh" });
                }

                // TODO: Thực hiện logic điều chỉnh tồn kho ở đây
                // - Cập nhật inventory table
                // - Tạo inventory_transaction record
                // - Log adjustment

                System.Diagnostics.Debug.WriteLine($"[Adjustment] Variant: {model.VariantId}, Adjustment: {model.Adjustment}, Reason: {model.Reason}");

                // Giả lập thành công
                return Json(new { 
                    success = true, 
                    message = $"Đã điều chỉnh {(model.Adjustment > 0 ? "+" : "")}{model.Adjustment} cho variant {model.VariantId}" 
                });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Adjustment Error] {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
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
