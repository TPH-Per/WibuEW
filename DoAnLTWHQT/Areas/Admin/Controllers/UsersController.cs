using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        public ActionResult Index(string role = "all", string status = "all")
        {
            var users = BuildSampleUsers();

            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "all", StringComparison.OrdinalIgnoreCase))
            {
                users = users.Where(u => string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                users = users.Where(u => string.Equals(u.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.RoleFilter = role;
            ViewBag.StatusFilter = status;

            return View(users);
        }

        public ActionResult Create()
        {
            var vm = BuildUserForm();
            ViewBag.Title = "Tạo tài khoản";
            return View("Form", vm);
        }

        public ActionResult Edit(long id)
        {
            var vm = BuildUserForm(id);
            vm.Id = id;
            vm.Status = "active";
            vm.Name = "perw.admin";
            vm.FullName = "Quản trị PERW";
            vm.Email = "admin@perw.vn";
            vm.PhoneNumber = "0901 234 567";
            vm.Role = "admin";
            ViewBag.Title = "Cập nhật tài khoản";
            return View("Form", vm);
        }

        public ActionResult Details(long id)
        {
            var user = BuildSampleUsers().FirstOrDefault(u => u.Id == id) ?? BuildSampleUsers().First();
            return View(user);
        }

        private static List<UserListItemViewModel> BuildSampleUsers()
        {
            return new List<UserListItemViewModel>
            {
                new UserListItemViewModel
                {
                    Id = 1,
                    Name = "perw.admin",
                    FullName = "Quản trị PERW",
                    Email = "admin@perw.vn",
                    PhoneNumber = "0987 654 321",
                    Role = "admin",
                    Status = "active",
                    WarehouseName = "Kho trung tâm",
                    CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6)
                },
                new UserListItemViewModel
                {
                    Id = 2,
                    Name = "warehouse.thuy",
                    FullName = "Thúy Kho",
                    Email = "warehouse@perw.vn",
                    PhoneNumber = "0903 111 222",
                    Role = "warehouse_manager",
                    Status = "active",
                    WarehouseName = "Kho trung tâm",
                    CreatedAt = DateTimeOffset.UtcNow.AddMonths(-4)
                },
                new UserListItemViewModel
                {
                    Id = 3,
                    Name = "branch.q1",
                    FullName = "Nguyễn An",
                    Email = "branch.q1@perw.vn",
                    PhoneNumber = "0933 444 555",
                    Role = "branch_manager",
                    Status = "inactive",
                    BranchName = "Chi nhánh Quận 1",
                    CreatedAt = DateTimeOffset.UtcNow.AddMonths(-3)
                },
                new UserListItemViewModel
                {
                    Id = 4,
                    Name = "client.hn",
                    FullName = "Trần Bình",
                    Email = "client@perw.vn",
                    PhoneNumber = "0911 222 333",
                    Role = "client",
                    Status = "active",
                    CreatedAt = DateTimeOffset.UtcNow.AddMonths(-1)
                }
            };
        }

        private static UserFormViewModel BuildUserForm(long? id = null)
        {
            return new UserFormViewModel
            {
                Id = id,
                RoleOptions = BuildRoleOptions(),
                WarehouseOptions = BuildWarehouseOptions(),
                BranchOptions = BuildBranchOptions()
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildRoleOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "admin", Label = "Admin" },
                new SelectOptionViewModel { Value = "warehouse_manager", Label = "Warehouse Manager" },
                new SelectOptionViewModel { Value = "branch_manager", Label = "Branch Manager" },
                new SelectOptionViewModel { Value = "client", Label = "Khách hàng" }
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildWarehouseOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "1", Label = "Kho trung tâm" }
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildBranchOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "1", Label = "Chi nhánh Quận 1" },
                new SelectOptionViewModel { Value = "2", Label = "Chi nhánh Hà Đông" }
            };
        }
    }
}
