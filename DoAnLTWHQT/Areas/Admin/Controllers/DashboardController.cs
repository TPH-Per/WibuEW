using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        public ActionResult Index()
        {
            var now = DateTimeOffset.Now;
            var filters = new ReportFiltersViewModel
            {
                FromDate = now.AddDays(-7),
                ToDate = now,
                BranchOptions = new List<SelectOptionViewModel>
                {
                    new SelectOptionViewModel { Value = "all", Label = "T?t c? chi nhánh", Selected = true },
                    new SelectOptionViewModel { Value = "1", Label = "Qu?n 1" },
                    new SelectOptionViewModel { Value = "2", Label = "Hà Ðông" }
                },
                StatusOptions = new List<SelectOptionViewModel>
                {
                    new SelectOptionViewModel { Label = "Pending", Value = "24", Description = "Ð?i x? lý" },
                    new SelectOptionViewModel { Label = "Processing", Value = "12", Description = "Ðang dóng gói" },
                    new SelectOptionViewModel { Label = "Completed", Value = "86", Description = "Ðã giao" }
                }
            };

            var vm = new AdminReportViewModel
            {
                Filters = filters,
                Widgets = new List<SalesReportWidgetViewModel>
                {
                    new SalesReportWidgetViewModel { Title = "Doanh thu", Subtitle = "7 ngày qua", Value = "1.24B", TrendValue = "+12%" },
                    new SalesReportWidgetViewModel { Title = "Ðon hàng", Subtitle = "Hoàn thành", Value = "428", TrendValue = "+4%" },
                    new SalesReportWidgetViewModel { Title = "Khách m?i", Subtitle = "Omni-channel", Value = "92", TrendValue = "+9%" },
                    new SalesReportWidgetViewModel { Title = "T?n kho", Subtitle = "SKU c?nh báo", Value = "38", TrendValue = "-6%" }
                },
                TopOrders = new List<PurchaseOrderListItemViewModel>
                {
                    new PurchaseOrderListItemViewModel { Id = 1, OrderCode = "PO24001", Branch = "Qu?n 1", Customer = "Nguy?n An", Status = "processing", TotalAmount = 1250000, CreatedAt = now.AddHours(-4) },
                    new PurchaseOrderListItemViewModel { Id = 2, OrderCode = "PO24002", Branch = "Hà Ðông", Customer = "Tr?n Bình", Status = "pending", TotalAmount = 980000, CreatedAt = now.AddHours(-2) },
                    new PurchaseOrderListItemViewModel { Id = 3, OrderCode = "PO24003", Branch = "Th? Ð?c", Customer = "Lê Chi", Status = "completed", TotalAmount = 3250000, CreatedAt = now.AddDays(-1) }
                },
                BestSellers = new List<ProductListItemViewModel>
                {
                    new ProductListItemViewModel { Id = 11, Name = "Áo khoác Varsity", Category = "Outerwear", Supplier = "PERW Garment", SKUCountLabel = "5 bi?n th?" },
                    new ProductListItemViewModel { Id = 12, Name = "Sneaker Aurora", Category = "Footwear", Supplier = "SneakerHub", SKUCountLabel = "3 bi?n th?" },
                    new ProductListItemViewModel { Id = 13, Name = "Qu?n jogger Flex", Category = "Bottom", Supplier = "PERW Garment", SKUCountLabel = "4 bi?n th?" },
                    new ProductListItemViewModel { Id = 14, Name = "Balo Transit", Category = "Accessories", Supplier = "CarryOn", SKUCountLabel = "2 bi?n th?" },
                    new ProductListItemViewModel { Id = 15, Name = "Áo thun Essential", Category = "Top", Supplier = "PERW Basics", SKUCountLabel = "6 bi?n th?" }
                }
            };

            return View(vm);
        }
    }
}
