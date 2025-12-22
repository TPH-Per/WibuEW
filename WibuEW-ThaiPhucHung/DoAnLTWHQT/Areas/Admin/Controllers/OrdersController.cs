using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class OrdersController : AdminBaseController
    {
        private readonly perwEntities db = new perwEntities();

        // Xem lịch sử bán hàng với khả năng lọc theo trạng thái
        public ActionResult Index(string status = "all")
        {
            var ordersQuery = db.purchase_orders
                .Where(o => o.deleted_at == null)
                .OrderByDescending(o => o.created_at);

            // Lọc theo trạng thái nếu không phải "all"
            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                ordersQuery = (IOrderedQueryable<purchase_orders>)ordersQuery.Where(o => o.status == status);
            }

            var orders = ordersQuery.ToList().Select(o => new OrderManagementViewModel
            {
                Id = o.id,
                OrderCode = o.order_code ?? "N/A",
                Branch = o.branch != null ? o.branch.name : "N/A",
                Customer = o.user != null ? o.user.full_name : o.shipping_recipient_name ?? "Khách lẻ",
                Status = o.status ?? "pending",
                TotalAmount = o.total_amount,
                CreatedAt = o.created_at.HasValue ? new DateTimeOffset(o.created_at.Value) : DateTimeOffset.Now,
                Lines = o.purchase_order_details.Select(d => new OrderLineViewModel
                {
                    ProductName = d.product_variants != null && d.product_variants.product != null 
                        ? d.product_variants.product.name 
                        : "N/A",
                    VariantName = d.product_variants != null 
                        ? d.product_variants.name 
                        : "N/A",
                    Quantity = d.quantity,
                    UnitPrice = d.price_at_purchase
                }).ToList(),
                Payment = o.payments.FirstOrDefault() != null ? new PaymentManagementViewModel
                {
                    Id = o.payments.FirstOrDefault().id,
                    OrderCode = o.order_code ?? "N/A",
                    Method = o.payments.FirstOrDefault().payment_methods != null 
                        ? o.payments.FirstOrDefault().payment_methods.name 
                        : "N/A",
                    Amount = o.payments.FirstOrDefault().amount,
                    Status = o.payments.FirstOrDefault().status ?? "pending",
                    CreatedAt = o.payments.FirstOrDefault().created_at.HasValue 
                        ? new DateTimeOffset(o.payments.FirstOrDefault().created_at.Value) 
                        : DateTimeOffset.Now,
                    IsDeposit = false
                } : null
            }).ToList();

            ViewBag.StatusFilter = status;
            return View(orders);
        }

        // Xem chi tiết hóa đơn
        public ActionResult Details(long id)
        {
            var order = db.purchase_orders
                .Where(o => o.id == id && o.deleted_at == null)
                .FirstOrDefault();

            if (order == null)
            {
                return HttpNotFound();
            }

            var orderViewModel = new OrderManagementViewModel
            {
                Id = order.id,
                OrderCode = order.order_code ?? "N/A",
                Branch = order.branch != null ? order.branch.name : "N/A",
                Customer = order.user != null ? order.user.full_name : order.shipping_recipient_name ?? "Khách lẻ",
                Status = order.status ?? "pending",
                TotalAmount = order.total_amount,
                CreatedAt = order.created_at.HasValue ? new DateTimeOffset(order.created_at.Value) : DateTimeOffset.Now,
                Lines = order.purchase_order_details.Select(d => new OrderLineViewModel
                {
                    ProductName = d.product_variants != null && d.product_variants.product != null 
                        ? d.product_variants.product.name 
                        : "N/A",
                    VariantName = d.product_variants != null 
                        ? d.product_variants.name 
                        : "N/A",
                    Quantity = d.quantity,
                    UnitPrice = d.price_at_purchase
                }).ToList(),
                Payment = order.payments.FirstOrDefault() != null ? new PaymentManagementViewModel
                {
                    Id = order.payments.FirstOrDefault().id,
                    OrderCode = order.order_code ?? "N/A",
                    Method = order.payments.FirstOrDefault().payment_methods != null 
                        ? order.payments.FirstOrDefault().payment_methods.name 
                        : "N/A",
                    Amount = order.payments.FirstOrDefault().amount,
                    Status = order.payments.FirstOrDefault().status ?? "pending",
                    CreatedAt = order.payments.FirstOrDefault().created_at.HasValue 
                        ? new DateTimeOffset(order.payments.FirstOrDefault().created_at.Value) 
                        : DateTimeOffset.Now,
                    IsDeposit = false
                } : null
            };

            return View(orderViewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
