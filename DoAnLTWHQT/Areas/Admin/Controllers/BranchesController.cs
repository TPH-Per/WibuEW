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
            // Bước 1: Tải dữ liệu thô về RAM trước (tránh lỗi LINQ to Entities với các cột mới như phone)
            var branchesData = _db.branches.OrderBy(b => b.id).ToList();

            // Bước 2: Map dữ liệu sang ViewModel
            var branches = branchesData.Select(b => new BranchViewModel
            {
                Id = b.id,
                Name = b.name,
                Location = b.location ?? "",
                // SỬA LỖI Ở ĐÂY:
                // Chỉ lấy tên từ bảng User liên kết. Nếu null thì báo "Chưa gán".
                // Bỏ đoạn "b.manager_name" đi vì cột này không còn dùng/không tồn tại.
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
                try
                {
                    var branch = new branch
                    {
                        name = model.Name,
                        location = model.Location,
                        manager_user_id = model.ManagerUserId, // Lưu ID User chọn từ menu
                        warehouse_id = model.WarehouseId > 0 ? model.WarehouseId : (long?)null,
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
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            ViewBag.Title = "Tạo chi nhánh";
            model.ManagerOptions = GetManagerOptions();
            model.WarehouseOptions = GetWarehouseOptions();
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
                ManagerUserId = branch.manager_user_id, // Load ID cũ lên form
                WarehouseId = branch.warehouse_id,
                
                // Load danh sách option
                ManagerOptions = GetManagerOptions(),
                WarehouseOptions = GetWarehouseOptions()
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
                try
                {
                    var branch = _db.branches.Find(id);
                    if (branch == null) return HttpNotFound();

                    branch.name = model.Name;
                    branch.location = model.Location;
                    branch.manager_user_id = model.ManagerUserId; // Cập nhật ID User mới
                    branch.warehouse_id = model.WarehouseId > 0 ? model.WarehouseId : (long?)null;
                    branch.updated_at = DateTime.UtcNow;

                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã cập nhật chi nhánh thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            model.Id = id;
            model.ManagerOptions = GetManagerOptions();
            model.WarehouseOptions = GetWarehouseOptions();
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

                // Kiểm tra ràng buộc dữ liệu
                bool hasData = _db.branch_inventories.Any(x => x.branch_id == id) ||
                               _db.warehouse_transfers.Any(x => x.to_branch_id == id) ||
                               _db.purchase_orders.Any(x => x.branch_id == id);

                if (hasData)
                {
                    TempData["ErrorMessage"] = "Không thể xóa: Chi nhánh này đã có dữ liệu giao dịch.";
                    return RedirectToAction("Index");
                }

                _db.branches.Remove(branch);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa chi nhánh!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // --- CÁC HÀM HỖ TRỢ ---

        private BranchFormViewModel BuildBranchForm(long? id = null)
        {
            return new BranchFormViewModel
            {
                Id = id,
                ManagerOptions = GetManagerOptions(),
                WarehouseOptions = GetWarehouseOptions()
            };
        }

        private IEnumerable<SelectOptionViewModel> GetManagerOptions()
        {
            return _db.users
                .Where(u => u.status == "active" && u.deleted_at == null)
                .OrderBy(u => u.full_name)
                .ToList()
                .Select(u => new SelectOptionViewModel
                {
                    Value = u.id.ToString(),
                    Label = u.full_name ?? u.name
                });
        }

        private IEnumerable<SelectOptionViewModel> GetWarehouseOptions()
        {
            return _db.warehouses
                .OrderBy(w => w.name)
                .ToList()
                .Select(w => new SelectOptionViewModel 
                { 
                    Value = w.id.ToString(), 
                    Label = w.name 
                });
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}