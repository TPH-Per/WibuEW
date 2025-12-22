using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class ReportsController : AdminBaseController
    {
        private readonly perwEntities _db = new perwEntities();

        public ActionResult Index(string period = "30days")
        {
            // Determine date range based on period
            var today = DateTime.Today;
            DateTime startDate;
            DateTime endDate = today.AddDays(1); // End of today
            DateTime previousStartDate;
            DateTime previousEndDate;
            int chartDays = 7;

            switch (period.ToLower())
            {
                case "7days":
                    startDate = today.AddDays(-7);
                    previousStartDate = today.AddDays(-14);
                    previousEndDate = today.AddDays(-7);
                    chartDays = 7;
                    break;
                case "month":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    previousStartDate = startDate.AddMonths(-1);
                    previousEndDate = startDate;
                    chartDays = DateTime.DaysInMonth(today.Year, today.Month);
                    break;
                case "quarter":
                    int currentQuarter = (today.Month - 1) / 3;
                    startDate = new DateTime(today.Year, currentQuarter * 3 + 1, 1);
                    previousStartDate = startDate.AddMonths(-3);
                    previousEndDate = startDate;
                    chartDays = 12; // Show weeks
                    break;
                case "year":
                    startDate = new DateTime(today.Year, 1, 1);
                    previousStartDate = startDate.AddYears(-1);
                    previousEndDate = startDate;
                    chartDays = 12; // Show months
                    break;
                default: // 30days
                    startDate = today.AddDays(-30);
                    previousStartDate = today.AddDays(-60);
                    previousEndDate = today.AddDays(-30);
                    chartDays = 30;
                    break;
            }

            // === REVENUE DATA ===
            var revenueThisPeriod = _db.purchase_orders
                .Where(o => o.created_at >= startDate && o.created_at < endDate && o.status != "cancelled")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            var revenuePreviousPeriod = _db.purchase_orders
                .Where(o => o.created_at >= previousStartDate && o.created_at < previousEndDate && o.status != "cancelled")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            var revenueTrend = revenuePreviousPeriod > 0 
                ? Math.Round((revenueThisPeriod - revenuePreviousPeriod) / revenuePreviousPeriod * 100, 1) 
                : 0;

            // === ORDER DATA ===
            var totalOrders = _db.purchase_orders.Count(o => o.created_at >= startDate && o.created_at < endDate);
            var completedOrders = _db.purchase_orders.Count(o => o.created_at >= startDate && o.created_at < endDate && o.status == "completed");
            var completionRate = totalOrders > 0 ? Math.Round((double)completedOrders / totalOrders * 100, 1) : 0;

            // === INVENTORY DATA ===
            var lowStockCount = _db.inventories
                .Where(i => i.quantity_on_hand <= i.reorder_level)
                .Count();

            // === DISCOUNT DATA ===
            var discountUsedCount = _db.discounts
                .Where(d => d.is_active)
                .Sum(d => (int?)d.used_count) ?? 0;

            // === DAILY REVENUE FOR CHART ===
            List<object> dailyRevenue;
            if (period == "year")
            {
                // Show by month
                dailyRevenue = Enumerable.Range(1, 12)
                    .Select(month => new
                    {
                        Date = new DateTime(today.Year, month, 1).ToString("MM/yyyy"),
                        Revenue = _db.purchase_orders
                            .Where(o => o.created_at.HasValue && o.created_at.Value.Year == today.Year && o.created_at.Value.Month == month && o.status != "cancelled")
                            .Sum(o => (decimal?)o.total_amount) ?? 0
                    }).Cast<object>().ToList();
            }
            else if (period == "quarter")
            {
                // Show by week (last 12 weeks)
                dailyRevenue = Enumerable.Range(0, 12)
                    .Select(i =>
                    {
                        var weekStart = startDate.AddDays(i * 7);
                        var weekEnd = weekStart.AddDays(7);
                        return new
                        {
                            Date = "W" + (i + 1),
                            Revenue = _db.purchase_orders
                                .Where(o => o.created_at >= weekStart && o.created_at < weekEnd && o.status != "cancelled")
                                .Sum(o => (decimal?)o.total_amount) ?? 0
                        };
                    }).Cast<object>().ToList();
            }
            else
            {
                // Show by day
                var daysToShow = Math.Min(chartDays, 14); // Max 14 days for readability
                dailyRevenue = Enumerable.Range(0, daysToShow)
                    .Select(i =>
                    {
                        var date = today.AddDays(-daysToShow + 1 + i);
                        return new
                        {
                            Date = date.ToString("dd/MM"),
                            Revenue = _db.purchase_orders
                                .Where(o => DbFunctions.TruncateTime(o.created_at) == date && o.status != "cancelled")
                                .Sum(o => (decimal?)o.total_amount) ?? 0
                        };
                    }).Cast<object>().ToList();
            }

            // === ORDER STATUS FOR PIE CHART ===
            var orderStatusData = _db.purchase_orders
                .Where(o => o.created_at >= startDate && o.created_at < endDate)
                .GroupBy(o => o.status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            // === TOP PRODUCTS ===
            var topProducts = _db.purchase_order_details
                .Include(d => d.product_variants.product)
                .Where(d => d.purchase_orders.created_at >= startDate && d.purchase_orders.created_at < endDate)
                .GroupBy(d => new { d.product_variants.product.name })
                .Select(g => new { ProductName = g.Key.name, TotalQty = g.Sum(x => x.quantity) })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToList();

            // === BRANCH REVENUE ===
            var branchRevenue = _db.purchase_orders
                .Include(o => o.branch)
                .Where(o => o.created_at >= startDate && o.created_at < endDate && o.status != "cancelled" && o.branch_id != null)
                .GroupBy(o => o.branch.name)
                .Select(g => new { BranchName = g.Key, Revenue = g.Sum(x => (decimal?)x.total_amount) ?? 0 })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Pass data to View
            ViewBag.Period = period;
            ViewBag.PeriodLabel = GetPeriodLabel(period);
            ViewBag.StartDate = startDate.ToString("dd/MM/yyyy");
            ViewBag.EndDate = today.ToString("dd/MM/yyyy");
            
            ViewBag.RevenueThisMonth = revenueThisPeriod;
            ViewBag.RevenueTrend = revenueTrend;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.CompletionRate = completionRate;
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.DiscountUsedCount = discountUsedCount;

            // Chart data as JSON
            ViewBag.DailyRevenueLabels = Newtonsoft.Json.JsonConvert.SerializeObject(
                dailyRevenue.Select(x => (string)x.GetType().GetProperty("Date").GetValue(x)).ToList());
            ViewBag.DailyRevenueData = Newtonsoft.Json.JsonConvert.SerializeObject(
                dailyRevenue.Select(x => (decimal)x.GetType().GetProperty("Revenue").GetValue(x)).ToList());

            ViewBag.OrderStatusLabels = Newtonsoft.Json.JsonConvert.SerializeObject(orderStatusData.Select(x => x.Status ?? "Không xác định").ToList());
            ViewBag.OrderStatusData = Newtonsoft.Json.JsonConvert.SerializeObject(orderStatusData.Select(x => x.Count).ToList());

            ViewBag.TopProductLabels = Newtonsoft.Json.JsonConvert.SerializeObject(topProducts.Select(x => x.ProductName ?? "N/A").ToList());
            ViewBag.TopProductData = Newtonsoft.Json.JsonConvert.SerializeObject(topProducts.Select(x => x.TotalQty).ToList());

            ViewBag.BranchLabels = Newtonsoft.Json.JsonConvert.SerializeObject(branchRevenue.Select(x => x.BranchName ?? "N/A").ToList());
            ViewBag.BranchData = Newtonsoft.Json.JsonConvert.SerializeObject(branchRevenue.Select(x => x.Revenue).ToList());

            return View();
        }

        private string GetPeriodLabel(string period)
        {
            switch (period.ToLower())
            {
                case "7days": return "7 ngày qua";
                case "month": return "Tháng này";
                case "quarter": return "Quý này";
                case "year": return "Năm nay";
                default: return "30 ngày qua";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
