using System.Collections.Generic;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace Ltwhqt.ViewModels.Branch
{
    public class BranchDashboardViewModel
    {
        public string BranchName { get; set; } = string.Empty;

        public IList<StatisticCardViewModel> Widgets { get; set; } = new List<StatisticCardViewModel>();

        public IList<PurchaseOrderListItemViewModel> RecentOrders { get; set; } = new List<PurchaseOrderListItemViewModel>();
    }
}
