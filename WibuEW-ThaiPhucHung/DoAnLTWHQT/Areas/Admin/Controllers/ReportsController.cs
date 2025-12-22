using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class ReportsController : AdminBaseController
    {
        private readonly perwEntities db = new perwEntities();

        // Báo cáo thống kê từ database
        public ActionResult Index()
        {
            var now = DateTime.Now;
            var sevenDaysAgo = now.AddDays(-7);

            // Tính doanh thu 7 ngày qua
            var revenueSevenDays = db.purchase_orders
                .Where(o => o.created_at >= sevenDaysAgo && o.deleted_at == null && o.status == "completed")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            var previousSevenDays = sevenDaysAgo.AddDays(-7);
            var previousRevenue = db.purchase_orders
                .Where(o => o.created_at >= previousSevenDays && o.created_at < sevenDaysAgo && o.deleted_at == null && o.status == "completed")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            var revenueTrend = previousRevenue > 0 
                ? $"{((revenueSevenDays - previousRevenue) / previousRevenue * 100):+0.0;-0.0}%" 
                : "+0%";

            // Tính tỷ lệ hoàn thành đơn
            var totalOrders = db.purchase_orders
                .Where(o => o.deleted_at == null && o.created_at >= sevenDaysAgo)
                .Count();

            var completedOrders = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "completed" && o.created_at >= sevenDaysAgo)
                .Count();

            var completionRate = totalOrders > 0 
                ? (decimal)completedOrders / totalOrders * 100 
                : 0;

            // Đếm số đơn hàng theo trạng thái
            var pendingCount = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "pending")
                .Count();

            var processingCount = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "processing")
                .Count();

            var completedCount = db.purchase_orders
                .Where(o => o.deleted_at == null && o.status == "completed")
                .Count();

            // Tính tổng số đơn hàng
            var totalOrdersCount = db.purchase_orders
                .Where(o => o.deleted_at == null)
                .Count();

            var indicators = new List<ReportIndicatorViewModel>
            {
                new ReportIndicatorViewModel 
                { 
                    Label = "Doanh thu 7 ngày", 
                    Value = FormatCurrency(revenueSevenDays), 
                    Trend = revenueTrend, 
                    Description = "So với 7 ngày trước" 
                },
                new ReportIndicatorViewModel 
                { 
                    Label = "Tỷ lệ hoàn thành đơn", 
                    Value = $"{completionRate:0.0}%", 
                    Trend = $"{completedOrders}/{totalOrders}", 
                    Description = "7 ngày qua" 
                },
                new ReportIndicatorViewModel 
                { 
                    Label = "Tổng số đơn hàng", 
                    Value = totalOrdersCount.ToString(), 
                    Trend = $"Pending: {pendingCount}", 
                    Description = $"Processing: {processingCount}, Completed: {completedCount}" 
                },
                new ReportIndicatorViewModel 
                { 
                    Label = "Giá trị TB đơn hàng", 
                    Value = totalOrdersCount > 0 ? FormatCurrency(revenueSevenDays / totalOrdersCount) : "0đ", 
                    Trend = "7 ngày qua", 
                    Description = "Đơn hàng hoàn thành" 
                }
            };

            return View(indicators);
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
