using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly perwEntities db = new perwEntities();

        public ActionResult Index()
        {
            var now = DateTime.Now;
            var sevenDaysAgo = now.AddDays(-7);
            var previousSevenDays = sevenDaysAgo.AddDays(-7);

            // Tính doanh thu 7 ngày qua
            var revenueSevenDays = db.purchase_orders
                .Where(o => o.created_at >= sevenDaysAgo && o.deleted_at == null && o.status == "completed")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            var previousRevenue = db.purchase_orders
                .Where(o => o.created_at >= previousSevenDays && o.created_at < sevenDaysAgo && o.deleted_at == null && o.status == "completed")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            var revenueTrend = previousRevenue > 0 
                ? $"{((revenueSevenDays - previousRevenue) / previousRevenue * 100):+0.0;-0.0}%" 
                : "+0%";

            // Đếm đơn hàng
            var totalOrders = db.purchase_orders
                .Where(o => o.deleted_at == null && o.created_at >= sevenDaysAgo)
                .Count();

            var completedOrders = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "completed" && o.created_at >= sevenDaysAgo)
                .Count();

            var completionRate = totalOrders > 0 
                ? (decimal)completedOrders / totalOrders * 100 
                : 0;

            // Đếm khách hàng mới (7 ngày qua)
            var newCustomers = db.users
                .Where(u => u.created_at >= sevenDaysAgo && u.deleted_at == null)
                .Count();

            // Đếm SKU dưới định mức (giả sử reorder_level = 10)
            var lowStockCount = db.branch_inventories
                .Where(bi => bi.quantity_on_hand < bi.reorder_level)
                .Select(bi => bi.product_variant_id)
                .Distinct()
                .Count();

            // Lấy danh sách chi nhánh từ database
            var branches = db.branches
                .Select(b => new SelectOptionViewModel 
                { 
                    Value = b.id.ToString(), 
                    Label = b.name 
                })
                .ToList();
            
            branches.Insert(0, new SelectOptionViewModel { Value = "all", Label = "Tất cả chi nhánh", Selected = true });

            // Đếm đơn hàng theo trạng thái
            var pendingCount = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "pending")
                .Count();

            var processingCount = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "processing")
                .Count();

            var completedCount = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "completed")
                .Count();

            var filters = new ReportFiltersViewModel
            {
                FromDate = new DateTimeOffset(sevenDaysAgo),
                ToDate = new DateTimeOffset(now),
                BranchOptions = branches,
                StatusOptions = new List<SelectOptionViewModel>
                {
                    new SelectOptionViewModel { Label = "Pending", Value = pendingCount.ToString(), Description = "Đợi xử lý" },
                    new SelectOptionViewModel { Label = "Processing", Value = processingCount.ToString(), Description = "Đang đóng gói" },
                    new SelectOptionViewModel { Label = "Completed", Value = completedCount.ToString(), Description = "Đã giao" }
                }
            };

            // Doanh thu theo chi nhánh (cho biểu đồ)
            var revenueByBranch = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "completed")
                .GroupBy(o => o.branch.name)
                .Select(g => new BranchRevenueViewModel
                {
                    BranchName = g.Key ?? "Không xác định",
                    Revenue = g.Sum(o => o.total_amount),
                    OrderCount = g.Count()
                })
                .OrderByDescending(b => b.Revenue)
                .ToList();

            var vm = new AdminReportViewModel
            {
                Filters = filters,
                Widgets = new List<SalesReportWidgetViewModel>
                {
                    new SalesReportWidgetViewModel 
                    { 
                        Title = "Doanh thu", 
                        Subtitle = "7 ngày qua", 
                        Value = FormatCurrency(revenueSevenDays), 
                        TrendValue = revenueTrend 
                    },
                    new SalesReportWidgetViewModel 
                    { 
                        Title = "Đơn hàng", 
                        Subtitle = "Hoàn thành", 
                        Value = completedOrders.ToString(), 
                        TrendValue = $"{completionRate:0.0}%" 
                    },
                    new SalesReportWidgetViewModel 
                    { 
                        Title = "Khách mới", 
                        Subtitle = "7 ngày qua", 
                        Value = newCustomers.ToString(), 
                        TrendValue = "+9%" 
                    },
                    new SalesReportWidgetViewModel 
                    { 
                        Title = "Tồn kho", 
                        Subtitle = "SKU cảnh báo", 
                        Value = lowStockCount.ToString(), 
                        TrendValue = "-6%" 
                    }
                },
                TopOrders = db.purchase_orders
                    .Where(o => o.deleted_at == null)
                    .OrderByDescending(o => o.created_at)
                    .Take(5)
                    .ToList() // Execute query first
                    .Select(o => new PurchaseOrderListItemViewModel
                    {
                        Id = o.id,
                        OrderCode = o.order_code ?? "N/A",
                        Branch = o.branch != null ? o.branch.name : "N/A",
                        Customer = o.user != null ? o.user.full_name : o.shipping_recipient_name ?? "Khách lẻ",
                        Status = o.status ?? "pending",
                        TotalAmount = o.total_amount,
                        CreatedAt = o.created_at.HasValue ? new DateTimeOffset(o.created_at.Value) : DateTimeOffset.Now
                    })
                    .ToList(),
                BestSellers = db.purchase_order_details
                    .Where(d => d.purchase_orders.deleted_at == null && 
                                d.purchase_orders.status == "completed" &&
                                d.purchase_orders.created_at >= sevenDaysAgo)
                    .GroupBy(d => new 
                    { 
                        ProductId = d.product_variants.product.id,
                        ProductName = d.product_variants.product.name,
                        CategoryName = d.product_variants.product.category.name,
                        SupplierName = d.product_variants.product.supplier.name
                    })
                    .Select(g => new
                    {
                        g.Key.ProductId,
                        g.Key.ProductName,
                        g.Key.CategoryName,
                        g.Key.SupplierName,
                        TotalQuantity = g.Sum(d => d.quantity),
                        VariantCount = g.Select(d => d.product_variant_id).Distinct().Count()
                    })
                    .OrderByDescending(p => p.TotalQuantity)
                    .Take(5)
                    .ToList()
                    .Select(p => new ProductListItemViewModel
                    {
                        Id = p.ProductId,
                        Name = p.ProductName ?? "N/A",
                        Category = p.CategoryName ?? "N/A",
                        Supplier = p.SupplierName ?? "N/A",
                        SKUCountLabel = $"{p.VariantCount} biến thể"
                    })
                    .ToList(),
                BranchRevenues = revenueByBranch
            };

            return View(vm);
        }

        private string FormatCurrency(decimal amount)
        {
            if (amount >= 1_000_000_000)
                return $"{amount / 1_000_000_000:0.##}B";
            if (amount >= 1_000_000)
                return $"{amount / 1_000_000:0.##}M";
            if (amount >= 1_000)
                return $"{amount / 1_000:0.##}K";
            return $"{amount:0}đ";
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
