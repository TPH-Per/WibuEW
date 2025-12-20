using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class BranchesController : AdminBaseController
    {
        private readonly perwEntities _db = new perwEntities();

        // GET: Admin/Branches
        public ActionResult Index()
        {
            var branches = _db.branches
                .OrderBy(b => b.name)
                .Select(b => new BranchViewModel
                {
                    Id = b.id,
                    Name = b.name,
                    Location = b.location ?? "",
                    Manager = b.user != null ? b.user.full_name : "Chưa gán",
                    SourceWarehouse = b.warehouse != null ? b.warehouse.name : "Chưa gán"
                })
                .ToList();

            return View(branches);
        }

        // GET: Admin/Branches/Create
        public ActionResult Create()
        {
            var vm = BuildBranchForm();
            ViewBag.Title = "Tạo chi nhánh";
            return View("Form", vm);
        }

        // POST: Admin/Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BranchFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate manager must have "branch" role
                if (model.ManagerUserId.HasValue)
                {
                    var manager = _db.users.Find(model.ManagerUserId.Value);
                    if (manager == null || manager.role == null || manager.role.name != "branch_manager")
                    {
                        ModelState.AddModelError("ManagerUserId", "Quản lý phải có vai trò 'branch_manager'.");
                        ViewBag.Title = "Tạo chi nhánh";
                        model.ManagerOptions = GetManagerOptions();
                        return View("Form", model);
                    }
                }

                try
                {
                    var branch = new branch
                    {
                        name = model.Name,
                        location = model.Location,
                        manager_user_id = model.ManagerUserId,
                        warehouse_id = 1, // Default to main warehouse
                        created_at = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow
                    };

                    _db.branches.Add(branch);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã thêm chi nhánh thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi thêm chi nhánh: " + ex.Message);
                }
            }

            ViewBag.Title = "Tạo chi nhánh";
            model.ManagerOptions = GetManagerOptions();
            return View("Form", model);
        }

        // GET: Admin/Branches/Edit/5
        public ActionResult Edit(long id)
        {
            var branch = _db.branches.Find(id);
            if (branch == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chi nhánh!";
                return RedirectToAction("Index");
            }

            var vm = new BranchFormViewModel
            {
                Id = branch.id,
                Name = branch.name,
                Location = branch.location,
                ManagerUserId = branch.manager_user_id,
                ManagerOptions = GetManagerOptions()
            };

            ViewBag.Title = "Cập nhật chi nhánh";
            return View("Form", vm);
        }

        // POST: Admin/Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(long id, BranchFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validate manager must have "branch" role
                if (model.ManagerUserId.HasValue)
                {
                    var manager = _db.users.Find(model.ManagerUserId.Value);
                    if (manager == null || manager.role == null || manager.role.name != "branch_manager")
                    {
                        ModelState.AddModelError("ManagerUserId", "Quản lý phải có vai trò 'branch_manager'.");
                        model.Id = id;
                        model.ManagerOptions = GetManagerOptions();
                        ViewBag.Title = "Cập nhật chi nhánh";
                        return View("Form", model);
                    }
                }

                try
                {
                    var branch = _db.branches.Find(id);
                    if (branch == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy chi nhánh!";
                        return RedirectToAction("Index");
                    }

                    branch.name = model.Name;
                    branch.location = model.Location;
                    branch.manager_user_id = model.ManagerUserId;
                    branch.updated_at = DateTime.UtcNow;

                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã cập nhật chi nhánh thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật chi nhánh: " + ex.Message);
                }
            }

            model.Id = id;
            model.ManagerOptions = GetManagerOptions();
            ViewBag.Title = "Cập nhật chi nhánh";
            return View("Form", model);
        }

        // POST: Admin/Branches/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            try
            {
                var branch = _db.branches.Find(id);
                if (branch == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy chi nhánh!";
                    return RedirectToAction("Index");
                }

                // Check if branch has related data
                var hasInventory = _db.branch_inventories.Any(bi => bi.branch_id == id);
                var hasTransfers = _db.warehouse_transfers.Any(wt => wt.to_branch_id == id);
                var hasOrders = _db.purchase_orders.Any(po => po.branch_id == id);

                if (hasInventory || hasTransfers || hasOrders)
                {
                    TempData["ErrorMessage"] = "Không thể xóa chi nhánh vì có dữ liệu liên quan (tồn kho, phiếu chuyển hoặc đơn hàng).";
                    return RedirectToAction("Index");
                }

                _db.branches.Remove(branch);
                _db.SaveChanges();

                TempData["SuccessMessage"] = "Đã xóa chi nhánh thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa chi nhánh: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        private BranchFormViewModel BuildBranchForm(long? id = null)
        {
            return new BranchFormViewModel
            {
                Id = id,
                ManagerOptions = GetManagerOptions()
            };
        }

        private IEnumerable<SelectOptionViewModel> GetManagerOptions()
        {
            // Chỉ lấy danh sách users có role "branch_manager"
            return _db.users
                .Where(u => u.deleted_at == null && u.role != null && u.role.name == "branch_manager")
                .OrderBy(u => u.full_name)
                .Select(u => new SelectOptionViewModel
                {
                    Value = u.id.ToString(),
                    Label = u.full_name ?? u.name
                })
                .ToList();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
