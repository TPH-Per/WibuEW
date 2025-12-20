using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class DashboardController : BranchBaseController
    {
        // GET: Branch/Dashboard
        public ActionResult Index()
        {
            // Lay chi nhanh cua user dang dang nhap
            var currentBranch = GetCurrentBranch();
            
            if (currentBranch == null)
            {
                // User khong duoc gan chi nhanh nao
                TempData["ErrorMessage"] = "Ban chua duoc phan cong quan ly chi nhanh nao.";
                return View("Error");
            }

            var branchId = currentBranch.id;

            // Thong ke ton kho chi nhanh
            var totalInventory = _db.branch_inventories
                .Where(bi => bi.branch_id == branchId)
                .Sum(bi => (int?)bi.quantity_on_hand) ?? 0;

            var reservedInventory = _db.branch_inventories
                .Where(bi => bi.branch_id == branchId)
                .Sum(bi => (int?)bi.quantity_reserved) ?? 0;

            // Don hang hom nay tai chi nhanh
            var today = DateTime.Today;
            var ordersToday = _db.purchase_orders
                .Count(o => o.branch_id == branchId && 
                           o.created_at.HasValue && 
                           System.Data.Entity.DbFunctions.TruncateTime(o.created_at) == today);

            // Doanh thu hom nay
            var revenueToday = _db.purchase_orders
                .Where(o => o.branch_id == branchId && 
                           o.created_at.HasValue && 
                           System.Data.Entity.DbFunctions.TruncateTime(o.created_at) == today &&
                           o.status != "cancelled")
                .Sum(o => (decimal?)o.total_amount) ?? 0;

            // Don hang gan day
            var recentOrders = _db.purchase_orders
                .Where(o => o.branch_id == branchId)
                .OrderByDescending(o => o.created_at)
                .Take(5)
                .ToList()
                .Select(o => new PurchaseOrderListItemViewModel
                {
                    Id = o.id,
                    OrderCode = string.Format("ORD-{0:D6}", o.id),
                    Customer = o.user != null ? (o.user.full_name ?? o.user.name) : "Khach le",
                    Status = o.status ?? "pending",
                    TotalAmount = o.total_amount,
                    CreatedAt = o.created_at.HasValue 
                        ? new DateTimeOffset(o.created_at.Value) 
                        : DateTimeOffset.Now
                })
                .ToList();

            // Phieu chuyen kho dang cho nhan
            var pendingTransfers = _db.warehouse_transfers
                .Count(wt => wt.to_branch_id == branchId && wt.status == "Shipping");

            var vm = new BranchDashboardViewModel
            {
                BranchName = currentBranch.name,
                Widgets = new List<StatisticCardViewModel>
                {
                    new StatisticCardViewModel 
                    { 
                        Label = "Ton kho", 
                        Value = totalInventory.ToString("N0"), 
                        SubLabel = string.Format("Dang giu: {0}", reservedInventory),
                        Icon = "bi-box-seam"
                    },
                    new StatisticCardViewModel 
                    { 
                        Label = "Don hom nay", 
                        Value = ordersToday.ToString(), 
                        SubLabel = string.Format("Doanh thu: {0:N0}d", revenueToday),
                        Icon = "bi-cart-check"
                    },
                    new StatisticCardViewModel 
                    { 
                        Label = "Cho nhan hang", 
                        Value = pendingTransfers.ToString(), 
                        SubLabel = "Phieu chuyen kho",
                        Icon = "bi-truck"
                    }
                },
                RecentOrders = recentOrders
            };

            return View(vm);
        }
    }
}
