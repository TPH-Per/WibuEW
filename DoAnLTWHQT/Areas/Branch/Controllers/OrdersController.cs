using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class OrdersController : BranchBaseController
    {
        public ActionResult Index(string status = "all")
        {
            var orders = BuildOrders();
            if (!string.Equals(status, "all", System.StringComparison.OrdinalIgnoreCase))
            {
                orders = orders.Where(o => string.Equals(o.Status, status, System.StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.StatusFilter = status;
            return View(orders);
        }

        public ActionResult Details(long id)
        {
            var order = BuildOrders().FirstOrDefault(o => o.Id == id) ?? BuildOrders().First();
            var vm = new BranchOrderDetailViewModel
            {
                Summary = order,
                Lines = new List<BranchOrderLineViewModel>
                {
                    new BranchOrderLineViewModel { ProductName = "Sneaker Aurora", VariantName = "Aurora / 39", Quantity = 1, UnitPrice = 1450000 },
                    new BranchOrderLineViewModel { ProductName = "Áo thun Essential", VariantName = "Essential / Đen M", Quantity = 1, UnitPrice = 350000 }
                },
                Payments = new List<BranchPaymentViewModel>
                {
                    new BranchPaymentViewModel { Id = 5001, Method = "Tiền mặt", Amount = 1800000, Status = "completed", CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-1) }
                },
                ShippingAddress = "45 Nguyễn Huệ, Q1"
            };
            return View(vm);
        }

        private static List<BranchOrderListItemViewModel> BuildOrders()
        {
            return new List<BranchOrderListItemViewModel>
            {
                new BranchOrderListItemViewModel { Id = 6001, OrderCode = "BR2401-089", Customer = "Phạm Vũ", Status = "processing", Channel = "Online", TotalAmount = 1800000, CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-3) },
                new BranchOrderListItemViewModel { Id = 6002, OrderCode = "BR2401-090", Customer = "Trịnh Mai", Status = "completed", Channel = "POS", TotalAmount = 1250000, CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-5) },
                new BranchOrderListItemViewModel { Id = 6003, OrderCode = "BR2401-091", Customer = "Nguyễn Nhật", Status = "pending", Channel = "POS", TotalAmount = 980000, CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-6) }
            };
        }
    }
}
