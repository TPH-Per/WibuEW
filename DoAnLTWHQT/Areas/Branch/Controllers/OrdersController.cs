using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    /// <summary>
    /// Controller quản lý đơn hàng của chi nhánh
    /// Branch manager chỉ thấy đơn hàng thuộc chi nhánh mình quản lý
    /// </summary>
    public class OrdersController : BranchBaseController
    {
        /// <summary>
        /// Danh sách đơn hàng của chi nhánh
        /// </summary>
        public ActionResult Index(string status = "all")
        {
            var branchId = GetCurrentBranchId();
            if (branchId == null)
            {
                TempData["Error"] = "Không tìm thấy chi nhánh của bạn.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Lấy đơn hàng thuộc chi nhánh này
            var query = _db.purchase_orders
                .Include(o => o.user)
                .Where(o => o.branch_id == branchId);

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(o => o.status == status);
            }

            var orders = query.OrderByDescending(o => o.created_at).ToList();

            ViewBag.StatusFilter = status;
            ViewBag.BranchId = branchId;
            return View(orders);
        }

        /// <summary>
        /// Chi tiết đơn hàng
        /// </summary>
        public ActionResult Details(long id)
        {
            var branchId = GetCurrentBranchId();
            if (branchId == null)
            {
                TempData["Error"] = "Không tìm thấy chi nhánh của bạn.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Lấy đơn hàng kèm thông tin liên quan
            var order = _db.purchase_orders
                .Include("user")
                .Include("purchase_order_details")
                .Include("purchase_order_details.product_variants")
                .Include("purchase_order_details.product_variants.product")
                .Include("payments")
                .Include("payments.payment_methods")
                .Include("branch")
                .FirstOrDefault(o => o.id == id && o.branch_id == branchId);

            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại hoặc không thuộc chi nhánh của bạn.";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(long id, string newStatus)
        {
            try
            {
                var branchId = GetCurrentBranchId();
                if (branchId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy chi nhánh của bạn." }, JsonRequestBehavior.AllowGet);
                }

                var order = _db.purchase_orders.FirstOrDefault(o => o.id == id && o.branch_id == branchId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại hoặc không thuộc chi nhánh của bạn." }, JsonRequestBehavior.AllowGet);
                }

                // Validate trạng thái hợp lệ
                var validStatuses = new[] { "pending", "processing", "shipping", "completed", "cancelled" };
                if (string.IsNullOrEmpty(newStatus) || !validStatuses.Contains(newStatus.ToLower()))
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ." }, JsonRequestBehavior.AllowGet);
                }

                // Cập nhật trạng thái
                order.status = newStatus.ToLower();
                order.updated_at = DateTime.Now;
                _db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
