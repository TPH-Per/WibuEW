using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class BranchesController : AdminBaseController
    {
        public ActionResult Index()
        {
            return View(BuildBranches());
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Tạo chi nhánh";
            return View("Form", BuildBranchForm());
        }

        public ActionResult Edit(long id)
        {
            var branch = BuildBranches().FirstOrDefault(b => b.Id == id) ?? BuildBranches().First();
            var form = BuildBranchForm(id);
            form.Name = branch.Name;
            form.Location = branch.Location;
            form.ManagerUserId = id;
            form.WarehouseId = 1;
            ViewBag.Title = "Cập nhật chi nhánh";
            return View("Form", form);
        }

        private static IList<BranchViewModel> BuildBranches()
        {
            return new List<BranchViewModel>
            {
                new BranchViewModel { Id = 1, Name = "Chi nhánh Quận 1", Location = "45 Nguyễn Huệ, Q1", Manager = "Nguyễn An", SourceWarehouse = "Kho trung tâm" },
                new BranchViewModel { Id = 2, Name = "Chi nhánh Hà Đông", Location = "12 Văn Phú, Hà Đông", Manager = "Trần Bình", SourceWarehouse = "Kho trung tâm" },
                new BranchViewModel { Id = 3, Name = "Chi nhánh Thủ Đức", Location = "88 Võ Văn Ngân, Thủ Đức", Manager = "Lê Chi", SourceWarehouse = "Kho trung tâm" }
            };
        }

        private static BranchFormViewModel BuildBranchForm(long? id = null)
        {
            return new BranchFormViewModel
            {
                Id = id,
                ManagerOptions = BuildManagerOptions(),
                WarehouseOptions = BuildWarehouseOptions()
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildManagerOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "2", Label = "Nguyễn An" },
                new SelectOptionViewModel { Value = "3", Label = "Trần Bình" },
                new SelectOptionViewModel { Value = "4", Label = "Lê Chi" }
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildWarehouseOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "1", Label = "Kho trung tâm" }
            };
        }
    }
}
