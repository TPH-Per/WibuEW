using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class OrdersController : AdminBaseController
    {
        public ActionResult Index(string status = "all")
        {
            var orders = BuildOrders();
            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                orders = orders.Where(o => string.Equals(o.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.StatusFilter = status;
            return View(orders);
        }

        public ActionResult Details(long id)
        {
            var order = BuildOrders().FirstOrDefault(o => o.Id == id) ?? BuildOrders().First();
            return View(order);
        }

        private static List<OrderManagementViewModel> BuildOrders()
        {
            return new List<OrderManagementViewModel>
            {
                new OrderManagementViewModel
                {
                    Id = 9001,
                    OrderCode = "PO24001",
                    Branch = "Chi nhánh Quận 1",
                    Customer = "Nguyễn An",
                    Status = "processing",
                    TotalAmount = 2450000,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-3),
                    Lines = new List<OrderLineViewModel>
                    {
                        new OrderLineViewModel { ProductName = "Sneaker Aurora", VariantName = "Aurora / 39", Quantity = 1, UnitPrice = 1450000 },
                        new OrderLineViewModel { ProductName = "Áo khoác Varsity", VariantName = "Varsity / L", Quantity = 1, UnitPrice = 1000000 }
                    },
                    Payment = new PaymentManagementViewModel { Id = 1, OrderCode = "PO24001", Method = "COD", Amount = 2450000, Status = "pending", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) }
                },
                new OrderManagementViewModel
                {
                    Id = 9002,
                    OrderCode = "PO24002",
                    Branch = "Chi nhánh Hà Đông",
                    Customer = "Trần Bình",
                    Status = "completed",
                    TotalAmount = 1750000,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    Lines = new List<OrderLineViewModel>
                    {
                        new OrderLineViewModel { ProductName = "Balo Transit", VariantName = "Transit / Đen", Quantity = 1, UnitPrice = 1750000 }
                    },
                    Payment = new PaymentManagementViewModel { Id = 2, OrderCode = "PO24002", Method = "VNPay", Amount = 1750000, Status = "completed", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
                },
                new OrderManagementViewModel
                {
                    Id = 9003,
                    OrderCode = "PO24003",
                    Branch = "Chi nhánh Thủ Đức",
                    Customer = "Lê Chi",
                    Status = "pending",
                    TotalAmount = 960000,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-5),
                    Lines = new List<OrderLineViewModel>
                    {
                        new OrderLineViewModel { ProductName = "Sneaker Aurora", VariantName = "Aurora / 41", Quantity = 1, UnitPrice = 960000 }
                    },
                    Payment = new PaymentManagementViewModel { Id = 3, OrderCode = "PO24003", Method = "Momo", Amount = 480000, Status = "deposit", IsDeposit = true, CreatedAt = DateTimeOffset.UtcNow.AddHours(-5) }
                }
            };
        }
    }
}
