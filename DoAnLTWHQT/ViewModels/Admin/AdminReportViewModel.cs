using Ltwhqt.ViewModels.Shared;
using System;
using System.Collections.Generic;

namespace Ltwhqt.ViewModels.Admin
{
    public class AdminReportViewModel
    {
        public ReportFiltersViewModel Filters { get; set; } = new ReportFiltersViewModel();

        public IList<SalesReportWidgetViewModel> Widgets { get; set; } = new List<SalesReportWidgetViewModel>();

        public IList<PurchaseOrderListItemViewModel> TopOrders { get; set; } = new List<PurchaseOrderListItemViewModel>();

        public IList<ProductListItemViewModel> BestSellers { get; set; } = new List<ProductListItemViewModel>();
    }

    public class ReportFiltersViewModel
    {
        public DateTimeOffset? FromDate { get; set; }

        public DateTimeOffset? ToDate { get; set; }

        public IList<SelectOptionViewModel> BranchOptions { get; set; } = new List<SelectOptionViewModel>();

        public IList<SelectOptionViewModel> StatusOptions { get; set; } = new List<SelectOptionViewModel>();
    }

    public class SalesReportWidgetViewModel
    {
        public string Title { get; set; } = string.Empty;

        public string Subtitle { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string TrendValue { get; set; } = string.Empty;
    }

    public class PurchaseOrderListItemViewModel
    {
        public long Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public string Branch { get; set; } = string.Empty;

        public string Customer { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class ProductListItemViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Supplier { get; set; } = string.Empty;

        public string SKUCountLabel { get; set; } = string.Empty;
    }
}
