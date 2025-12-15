using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;
using BCryptNet = BCrypt.Net.BCrypt;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        private readonly Entities _db = new Entities();

        public ActionResult Index(string role = "all", string status = "all")
        {
            // Lấy users từ database thay vì hardcode
            var usersQuery = _db.users
                .Include(u => u.role)
                .Include(u => u.warehouse)
                .Where(u => u.deleted_at == null);

            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "all", StringComparison.OrdinalIgnoreCase))
            {
                usersQuery = usersQuery.Where(u => u.role.name == role);
            }

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                usersQuery = usersQuery.Where(u => u.status == status);
            }

            var users = usersQuery.ToList().Select(u => new UserListItemViewModel
            {
                Id = u.id,
                Name = u.name,
                FullName = u.full_name ?? u.name,
                Email = u.email,
                PhoneNumber = u.phone_number,
                Role = u.role != null ? u.role.name : "N/A",
                Status = u.status ?? "active",
                WarehouseName = u.warehouse != null ? u.warehouse.name : null,
                CreatedAt = u.created_at.HasValue 
                    ? new DateTimeOffset(u.created_at.Value) 
                    : DateTimeOffset.Now
            }).ToList();

            ViewBag.RoleFilter = role;
            ViewBag.StatusFilter = status;

            return View(users);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var vm = BuildUserForm();
            ViewBag.Title = "Tạo tài khoản";
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserFormViewModel model)
        {
            try
            {
                // Kiểm tra email trùng
                var existingEmail = _db.users.FirstOrDefault(u => u.email.ToLower() == model.Email.ToLower() && u.deleted_at == null);
                if (existingEmail != null)
                {
                    ViewBag.ErrorMessage = "Email này đã được sử dụng.";
                    model.RoleOptions = BuildRoleOptions();
                    model.WarehouseOptions = BuildWarehouseOptions();
                    model.BranchOptions = BuildBranchOptions();
                    return View("Form", model);
                }

                // Kiểm tra username trùng
                var existingName = _db.users.FirstOrDefault(u => u.name.ToLower() == model.Name.ToLower() && u.deleted_at == null);
                if (existingName != null)
                {
                    ViewBag.ErrorMessage = "Username này đã được sử dụng.";
                    model.RoleOptions = BuildRoleOptions();
                    model.WarehouseOptions = BuildWarehouseOptions();
                    model.BranchOptions = BuildBranchOptions();
                    return View("Form", model);
                }

                // Lấy role_id
                var roleEntity = _db.roles.FirstOrDefault(r => r.name == model.Role);
                if (roleEntity == null)
                {
                    ViewBag.ErrorMessage = "Role không hợp lệ.";
                    model.RoleOptions = BuildRoleOptions();
                    model.WarehouseOptions = BuildWarehouseOptions();
                    model.BranchOptions = BuildBranchOptions();
                    return View("Form", model);
                }

                // Hash password
                var hashedPassword = BCryptNet.HashPassword(model.Password);

                // Tạo user mới
                var newUser = new user
                {
                    name = model.Name,
                    full_name = model.FullName,
                    email = model.Email,
                    phone_number = model.PhoneNumber,
                    password = hashedPassword,
                    role_id = roleEntity.id,
                    warehouse_id = model.WarehouseId,
                    status = model.Status ?? "active",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                _db.users.Add(newUser);
                _db.SaveChanges();

                // Gọi stored procedure tạo SQL User (nếu cần)
                try
                {
                    CreateSQLUser(model.Name, model.Password, GetRoleType(model.Role));
                }
                catch (Exception spEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SP Error (non-critical): {spEx.Message}");
                }

                TempData["SuccessMessage"] = $"Tạo tài khoản '{model.Name}' thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                model.RoleOptions = BuildRoleOptions();
                model.WarehouseOptions = BuildWarehouseOptions();
                model.BranchOptions = BuildBranchOptions();
                return View("Form", model);
            }
        }

        [HttpGet]
        public ActionResult Edit(long id)
        {
            var user = _db.users.Include(u => u.role).FirstOrDefault(u => u.id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy user.";
                return RedirectToAction("Index");
            }

            var vm = BuildUserForm(id);
            vm.Id = id;
            vm.Name = user.name;
            vm.FullName = user.full_name;
            vm.Email = user.email;
            vm.PhoneNumber = user.phone_number;
            vm.Role = user.role != null ? user.role.name : null;
            vm.WarehouseId = user.warehouse_id;
            vm.Status = user.status;

            ViewBag.Title = "Cập nhật tài khoản";
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(long id, UserFormViewModel model)
        {
            try
            {
                var user = _db.users.FirstOrDefault(u => u.id == id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy user.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra email trùng (trừ user hiện tại)
                var existingEmail = _db.users.FirstOrDefault(u => u.email.ToLower() == model.Email.ToLower() && u.id != id && u.deleted_at == null);
                if (existingEmail != null)
                {
                    ViewBag.ErrorMessage = "Email này đã được sử dụng.";
                    model.RoleOptions = BuildRoleOptions();
                    model.WarehouseOptions = BuildWarehouseOptions();
                    model.BranchOptions = BuildBranchOptions();
                    return View("Form", model);
                }

                // Lấy role_id
                var roleEntity = _db.roles.FirstOrDefault(r => r.name == model.Role);
                if (roleEntity == null)
                {
                    ViewBag.ErrorMessage = "Role không hợp lệ.";
                    model.RoleOptions = BuildRoleOptions();
                    model.WarehouseOptions = BuildWarehouseOptions();
                    model.BranchOptions = BuildBranchOptions();
                    return View("Form", model);
                }

                // Cập nhật user
                user.name = model.Name;
                user.full_name = model.FullName;
                user.email = model.Email;
                user.phone_number = model.PhoneNumber;
                user.role_id = roleEntity.id;
                user.warehouse_id = model.WarehouseId;
                user.status = model.Status ?? "active";
                user.updated_at = DateTime.Now;

                _db.SaveChanges();

                TempData["SuccessMessage"] = $"Cập nhật tài khoản '{model.Name}' thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                model.RoleOptions = BuildRoleOptions();
                model.WarehouseOptions = BuildWarehouseOptions();
                model.BranchOptions = BuildBranchOptions();
                return View("Form", model);
            }
        }

        public ActionResult Details(long id)
        {
            var user = _db.users
                .Include(u => u.role)
                .Include(u => u.warehouse)
                .FirstOrDefault(u => u.id == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy user.";
                return RedirectToAction("Index");
            }

            var vm = new UserListItemViewModel
            {
                Id = user.id,
                Name = user.name,
                FullName = user.full_name ?? user.name,
                Email = user.email,
                PhoneNumber = user.phone_number,
                Role = user.role != null ? user.role.name : "N/A",
                Status = user.status ?? "active",
                WarehouseName = user.warehouse != null ? user.warehouse.name : null,
                CreatedAt = user.created_at.HasValue 
                    ? new DateTimeOffset(user.created_at.Value) 
                    : DateTimeOffset.Now
            };

            return View(vm);
        }

        #region Private Methods

        private UserFormViewModel BuildUserForm(long? id = null)
        {
            return new UserFormViewModel
            {
                Id = id,
                RoleOptions = BuildRoleOptions(),
                WarehouseOptions = BuildWarehouseOptions(),
                BranchOptions = BuildBranchOptions()
            };
        }

        private IEnumerable<SelectOptionViewModel> BuildRoleOptions()
        {
            return _db.roles.Where(r => r.deleted_at == null).ToList()
                .Select(r => new SelectOptionViewModel
                {
                    Value = r.name,
                    Label = r.description ?? r.name
                }).ToList();
        }

        private IEnumerable<SelectOptionViewModel> BuildWarehouseOptions()
        {
            return _db.warehouses.Where(w => w.deleted_at == null).ToList()
                .Select(w => new SelectOptionViewModel
                {
                    Value = w.id.ToString(),
                    Label = w.name
                }).ToList();
        }

        private IEnumerable<SelectOptionViewModel> BuildBranchOptions()
        {
            return _db.branches.ToList()
                .Select(b => new SelectOptionViewModel
                {
                    Value = b.id.ToString(),
                    Label = b.name
                }).ToList();
        }

        private string GetRoleType(string roleName)
        {
            switch (roleName?.ToLower())
            {
                case "admin": return "Admin";
                case "warehouse_manager": return "Warehouse";
                case "branch_manager": return "Branch";
                default: return "Customer";
            }
        }

        private void CreateSQLUser(string username, string password, string roleType)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["PerwDbContext"]?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString)) return;

            using (var conn = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand("sp_System_CreateSQLUser", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.Parameters.AddWithValue("@RoleType", roleType);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

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
