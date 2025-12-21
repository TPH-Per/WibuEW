using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
// Thêm dòng này để dùng lệnh .Include()
using System.Data.Entity;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class OrdersController : AdminBaseController
    {
        private readonly perwEntities _db = new perwEntities();

        public ActionResult Index(string status = "all")
        {
            var query = _db.purchase_orders.AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(o => o.status == status);
            }

            var orders = query.OrderByDescending(o => o.created_at).ToList();

            ViewBag.StatusFilter = status;
            return View(orders);
        }

        // --- SỬA ĐOẠN NÀY ---
        public ActionResult Details(long id)
        {
            // Thay vì dùng .Find(id), ta dùng câu lệnh dài hơn để lấy kèm dữ liệu
            var order = _db.purchase_orders
                // 1. Lấy kèm danh sách sản phẩm và thông tin chi tiết sp
                .Include("purchase_order_details")
                .Include("purchase_order_details.product_variants")
                .Include("purchase_order_details.product_variants.product")

                // 2. Lấy kèm thông tin thanh toán và tên phương thức (COD/Banking)
                .Include("payments")
                .Include("payments.payment_methods")

                // 3. Điều kiện tìm kiếm
                .FirstOrDefault(o => o.id == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }
        // --------------------

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}