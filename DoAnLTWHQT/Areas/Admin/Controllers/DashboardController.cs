using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly perwEntities _db = new perwEntities();

        public ActionResult Index()
        {
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            var fourteenDaysAgo = DateTime.Now.AddDays(-14);

            // === DOANH THU 7 NGAY ===
            var revenueSevenDays = _db.purchase_orders
                .Where(o => o.created_at >= sevenDaysAgo && o.deleted_at == null && o.status == "completed")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            var revenuePreviousSevenDays = _db.purchase_orders
                .Where(o => o.created_at >= fourteenDaysAgo && o.created_at < sevenDaysAgo && o.deleted_at == null && o.status == "completed")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            var revenueTrend = revenuePreviousSevenDays > 0
                ? (revenueSevenDays - revenuePreviousSevenDays) / revenuePreviousSevenDays * 100
                : 0;

            // === DON HANG HOAN THANH ===
            var totalOrders = _db.purchase_orders
                .Count(o => o.created_at >= sevenDaysAgo && o.deleted_at == null);

            var completedOrders = _db.purchase_orders
                .Count(o => o.created_at >= sevenDaysAgo && o.deleted_at == null && o.status == "completed");

            var completionRate = totalOrders > 0
                ? (decimal)completedOrders / totalOrders * 100
                : 0;

            // === KHACH HANG MOI ===
            var newCustomers = _db.users
                .Where(u => u.created_at >= sevenDaysAgo && u.deleted_at == null)
                .Count();

            // === TON KHO CANH BAO ===
            // Logic: Kho Tong va Chi nhanh la 2 he thong RIENG BIET
            // - branch_inventories: Ton kho tai cac chi nhanh
            // - inventories: Ton kho tai kho tong (warehouse)
            var lowStockCount = _db.branch_inventories
                .Where(bi => bi.quantity_on_hand < bi.reorder_level)
                .Select(bi => bi.product_variant_id)
                .Distinct()
                .Count();

            // === TOP 5 DON HANG MOI NHAT ===
            var topOrdersRaw = _db.purchase_orders
                .Where(o => o.deleted_at == null)
                .OrderByDescending(o => o.created_at)
                .Take(5)
                .ToList();

            var topOrders = topOrdersRaw.Select(o => new PurchaseOrderListItemViewModel
                {
                    Id = o.id,
                    OrderCode = o.order_code,
                    Branch = o.branch?.name ?? "N/A",
                    Customer = o.user?.full_name ?? o.shipping_recipient_name ?? "Khach vang lai",
                    Status = o.status,
                    TotalAmount = o.total_amount,
                    CreatedAt = o.created_at.HasValue ? new DateTimeOffset(o.created_at.Value) : DateTimeOffset.Now
                })
                .ToList();

            // === TOP 5 SAN PHAM BAN CHAY ===
            var bestSellers = _db.purchase_order_details
                .Where(d => d.purchase_orders.deleted_at == null 
                    && d.purchase_orders.status == "completed"
                    && d.purchase_orders.created_at >= sevenDaysAgo)
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
                    TotalQuantity = g.Sum(x => x.quantity)
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
                    SKUCountLabel = $"{p.TotalQuantity} da ban"
                })
                .ToList();

            // === DOANH THU THEO CHI NHANH (cho bieu do) ===
            var revenueByBranch = _db.purchase_orders
                .Where(o => o.deleted_at == null 
                    && o.status == "completed"
                    && o.created_at >= sevenDaysAgo)
                .GroupBy(o => o.branch.name)
                .Select(g => new BranchRevenueViewModel
                {
                    BranchName = g.Key ?? "Khong xac dinh",
                    Revenue = g.Sum(o => o.total_amount),
                    OrderCount = g.Count()
                })
                .OrderByDescending(b => b.Revenue)
                .ToList();

            // === WIDGETS ===
            var widgets = new List<SalesReportWidgetViewModel>
            {
                new SalesReportWidgetViewModel
                {
                    Title = "Doanh thu 7 ngay",
                    Subtitle = "So voi 7 ngay truoc",
                    Value = FormatCurrency(revenueSevenDays),
                    TrendValue = revenueTrend >= 0 ? $"+{revenueTrend:F1}%" : $"{revenueTrend:F1}%"
                },
                new SalesReportWidgetViewModel
                {
                    Title = "Don hang hoan thanh",
                    Subtitle = "7 ngay qua",
                    Value = completedOrders.ToString(),
                    TrendValue = $"{completionRate:F1}% hoan thanh"
                },
                new SalesReportWidgetViewModel
                {
                    Title = "Khach hang moi",
                    Subtitle = "7 ngay qua",
                    Value = newCustomers.ToString(),
                    TrendValue = ""
                },
                new SalesReportWidgetViewModel
                {
                    Title = "Ton kho canh bao",
                    Subtitle = "SKU duoi muc ton",
                    Value = lowStockCount.ToString(),
                    TrendValue = ""
                }
            };

            var vm = new AdminReportViewModel
            {
                Widgets = widgets,
                TopOrders = topOrders,
                BestSellers = bestSellers,
                BranchRevenues = revenueByBranch
            };

            return View(vm);
        }

        private string FormatCurrency(decimal amount)
        {
            if (amount >= 1_000_000_000) return $"{amount / 1_000_000_000:0.##}B";
            if (amount >= 1_000_000) return $"{amount / 1_000_000:0.##}M";
            if (amount >= 1_000) return $"{amount / 1_000:0.##}K";
            return $"{amount:0}d";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
