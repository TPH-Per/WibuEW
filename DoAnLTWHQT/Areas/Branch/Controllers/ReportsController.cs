using System.Collections.Generic;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class ReportsController : BranchBaseController
    {
        public ActionResult Index()
        {
            var cards = new List<BranchReportCardViewModel>
            {
                new BranchReportCardViewModel { Title = "Doanh thu tuần", Value = "420M", Trend = "+8%", Description = "Tăng so với tuần trước" },
                new BranchReportCardViewModel { Title = "Đơn hoàn tất", Value = "128", Trend = "+4%", Description = "Bao gồm cả POS & Online" },
                new BranchReportCardViewModel { Title = "Đơn huỷ", Value = "6", Trend = "-2%", Description = "Giảm do tồn ổn định" },
                new BranchReportCardViewModel { Title = "Pre-order hoàn tất", Value = "18", Trend = "+3%", Description = "Khách quay lại ưa thích" }
            };

            return View(cards);
        }
    }
}
