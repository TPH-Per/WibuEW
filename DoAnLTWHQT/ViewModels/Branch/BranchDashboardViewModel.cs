using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;
using System.Collections.Generic;

namespace Ltwhqt.ViewModels.Branch
{
    public class BranchDashboardViewModel
    {
        public string BranchName { get; set; } = string.Empty;

        public IList<StatisticCardViewModel> Widgets { get; set; } = new List<StatisticCardViewModel>();

        public IList<PurchaseOrderListItemViewModel> RecentOrders { get; set; } = new List<PurchaseOrderListItemViewModel>();
    }
}
