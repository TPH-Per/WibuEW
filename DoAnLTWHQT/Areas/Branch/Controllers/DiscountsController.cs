using Ltwhqt.ViewModels.Branch;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class DiscountsController : BranchBaseController
    {
        [HttpGet]
        public ActionResult Index(string code = "")
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return View(new BranchDiscountCheckViewModel());
            }

            var vm = new BranchDiscountCheckViewModel
            {
                Code = code.ToUpperInvariant(),
                IsValid = code.ToUpperInvariant() == "NEW50",
                Message = code.ToUpperInvariant() == "NEW50" ? "Mã hợp lệ - giảm 50%" : "Mã không tồn tại hoặc hết hạn",
                StartAt = System.DateTimeOffset.UtcNow.AddDays(-5),
                EndAt = System.DateTimeOffset.UtcNow.AddDays(10),
                MinOrderAmount = 500000,
                UsedCount = 120,
                MaxUses = 200
            };

            return View(vm);
        }
    }
}
