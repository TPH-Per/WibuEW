using System.Collections.Generic;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class PreOrdersController : BranchBaseController
    {
        public ActionResult Index()
        {
            return View(BuildPreOrders());
        }

        public ActionResult Details(long id)
        {
            var preOrder = BuildPreOrders().Find(p => p.Id == id) ?? BuildPreOrders()[0];
            return View(preOrder);
        }

        private static List<BranchPreOrderViewModel> BuildPreOrders()
        {
            return new List<BranchPreOrderViewModel>
            {
                new BranchPreOrderViewModel
                {
                    Id = 7001,
                    OrderCode = "PR2401-001",
                    Customer = "Đỗ Hiếu",
                    PickupDateLabel = "20/05 - sau 17h",
                    DepositAmount = 750000,
                    Status = "waiting_stock",
                    Lines = new List<BranchOrderLineViewModel>
                    {
                        new BranchOrderLineViewModel { ProductName = "Varsity Jacket", VariantName = "Varsity / M", Quantity = 1, UnitPrice = 1500000 }
                    }
                },
                new BranchPreOrderViewModel
                {
                    Id = 7002,
                    OrderCode = "PR2401-002",
                    Customer = "Lê Kha",
                    PickupDateLabel = "22/05 sáng",
                    DepositAmount = 500000,
                    Status = "ready",
                    Lines = new List<BranchOrderLineViewModel>
                    {
                        new BranchOrderLineViewModel { ProductName = "Sneaker Aurora", VariantName = "Aurora / 41", Quantity = 1, UnitPrice = 1200000 }
                    }
                }
            };
        }
    }
}
