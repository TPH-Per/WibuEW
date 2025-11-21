using Ltwhqt.ViewModels.Branch;
using Ltwhqt.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class DashboardController : BranchBaseController
    {
        public ActionResult Index()
        {
            var vm = new BranchDashboardViewModel
            {
                BranchName = "Chi nhánh Qu?n 1",
                Widgets = new List<StatisticCardViewModel>
                {
                    new StatisticCardViewModel { Label = "Doanh thu hôm nay", Value = "120.5M", SubLabel = "Bao g?m POS + Online", Icon = "bi-currency-exchange", Context = "success", Trend = "+8%" },
                    new StatisticCardViewModel { Label = "Ðon POS", Value = "36", SubLabel = "Khách t?i c?a hàng", Icon = "bi-receipt", Context = "primary", Trend = "+5%" },
                    new StatisticCardViewModel { Label = "Ðon Pre-order", Value = "12", SubLabel = "Ðang ch? hàng", Icon = "bi-truck", Context = "warning", Trend = "2 giao hôm nay" },
                    new StatisticCardViewModel { Label = "SKU c?n nh?p", Value = "9", SubLabel = "Du?i d?nh m?c", Icon = "bi-bag", Context = "danger", Trend = "G?i yêu c?u" }
                },
                RecentOrders = new List<Ltwhqt.ViewModels.Admin.PurchaseOrderListItemViewModel>
                {
                    new Ltwhqt.ViewModels.Admin.PurchaseOrderListItemViewModel { Id = 101, OrderCode = "BR2401-089", Customer = "Ph?m Vu", Status = "completed", TotalAmount = 2450000, Branch = "Qu?n 1", CreatedAt = DateTimeOffset.Now.AddHours(-1) },
                    new Ltwhqt.ViewModels.Admin.PurchaseOrderListItemViewModel { Id = 102, OrderCode = "BR2401-090", Customer = "Tr?nh Mai", Status = "processing", TotalAmount = 1750000, Branch = "Qu?n 1", CreatedAt = DateTimeOffset.Now.AddHours(-2) },
                    new Ltwhqt.ViewModels.Admin.PurchaseOrderListItemViewModel { Id = 103, OrderCode = "BR2401-091", Customer = "Nguy?n Nh?t", Status = "pending", TotalAmount = 960000, Branch = "Qu?n 1", CreatedAt = DateTimeOffset.Now.AddHours(-3) }
                }
            };

            return View(vm);
        }
    }
}
