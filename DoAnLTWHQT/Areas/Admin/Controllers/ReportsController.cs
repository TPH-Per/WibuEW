using Ltwhqt.ViewModels.Admin;
using System.Collections.Generic;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class ReportsController : AdminBaseController
    {
        public ActionResult Index()
        {
            var indicators = new List<ReportIndicatorViewModel>
            {
                new ReportIndicatorViewModel { Label = "Doanh thu 7 ngày", Value = "1.24B", Trend = "+12%", Description = "Tăng so với tuần trước" },
                new ReportIndicatorViewModel { Label = "Tỷ lệ hoàn thành đơn", Value = "86%", Trend = "+4%", Description = "Bao gồm toàn bộ chi nhánh" },
                new ReportIndicatorViewModel { Label = "SKU dưới định mức", Value = "38", Trend = "-5%", Description = "Cần nhập bổ sung" },
                new ReportIndicatorViewModel { Label = "Giảm giá đã dùng", Value = "620 lượt", Trend = "+20%", Description = "Chương trình NEW50" }
            };

            return View(indicators);
        }
    }
}
