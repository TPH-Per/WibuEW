using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using BCryptNet = BCrypt.Net.BCrypt;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        private readonly DoAnLTWHQT.Entities _db = new DoAnLTWHQT.Entities();

        // GET: Admin/Users
        public ActionResult Index(string role = "all", string status = "all")
        {
            var query = _db.users.Include("role").Where(u => u.deleted_at == null);

            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.role.name == role);
            }

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.status == status);
            }

            var users = query.OrderByDescending(u => u.created_at).ToList().Select(u => new UserListItemViewModel
            {
                Id = u.id,
                Name = u.name,
                FullName = u.full_name,
                Email = u.email,
                PhoneNumber = u.phone_number ?? "",
                Role = u.role?.name ?? "client",
                Status = u.status ?? "active",
                CreatedAt = u.created_at.HasValue ? new DateTimeOffset(u.created_at.Value) : DateTimeOffset.MinValue
            }).ToList();

            ViewBag.RoleFilter = role;
            ViewBag.StatusFilter = status;

            return View(users);
        }

        // GET: Admin/Users/Create
        public ActionResult Create()
        {
            var vm = BuildUserForm();
            ViewBag.Title = "Tạo tài khoản";
            return View("Form", vm);
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserFormViewModel model)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError("Name", "Vui lòng nhập tên đăng nhập.");
                }
                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "Vui lòng nhập email.");
                }
                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
                }
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                }

                if (!ModelState.IsValid)
                {
                    model.RoleOptions = BuildRoleOptions();
                    model.BranchOptions = BuildBranchOptions();
                    return View("Form", model);
                }

                // Normalize inputs
                var normalizedEmail = model.Email.Trim().ToLowerInvariant();
                var normalizedUsername = model.Name.Trim().ToLowerInvariant();

                // Check if email already exists
                if (_db.users.Any(u => u.email.ToLower() == normalizedEmail && u.deleted_at == null))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    model.RoleOptions = BuildRoleOptions();
                    model.BranchOptions = BuildBranchOptions();
                    return View("Form", model);
                }

                // Check if username already exists
                if (_db.users.Any(u => u.name.ToLower() == normalizedUsername && u.deleted_at == null))
                {
                    ModelState.AddModelError("Name", "Tên đăng nhập này đã được sử dụng.");
                    model.RoleOptions = BuildRoleOptions();
                    model.BranchOptions = BuildBranchOptions();
                    return View("Form", model);
                }

                // Determine SQL Server role type from application role
                string sqlRoleType = GetSqlRoleType(model.Role);

                // Step 1: Create SQL User using stored procedure
                var connectionString = ConfigurationManager.ConnectionStrings["PerwDbContext"]?.ConnectionString;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    try
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            using (var command = new SqlCommand("sp_System_CreateSQLUser", connection))
                            {
                                command.CommandType = System.Data.CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@Username", normalizedUsername);
                                command.Parameters.AddWithValue("@Password", model.Password);
                                command.Parameters.AddWithValue("@RoleType", sqlRoleType);

                                command.ExecuteNonQuery();
                            }
                        }
                        System.Diagnostics.Debug.WriteLine($"SQL User created: {normalizedUsername} with role {sqlRoleType}");
                    }
                    catch (SqlException sqlEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"SQL Error creating user: {sqlEx.Message}");
                        TempData["WarningMessage"] = $"SQL User không thể tạo: {sqlEx.Message}. User vẫn được tạo trong hệ thống.";
                    }
                }

                // Step 2: Get role_id from role name
                var dbRole = _db.roles.FirstOrDefault(r => r.name == model.Role);
                long roleId = dbRole?.id ?? 4; // Default to customer (4)

                // Step 3: Hash password using BCrypt
                var hashedPassword = BCryptNet.HashPassword(model.Password, BCryptNet.GenerateSalt(12));

                // Step 4: Create new user in users table
                var newUser = new DoAnLTWHQT.user
                {
                    name = normalizedUsername,
                    full_name = model.FullName?.Trim() ?? normalizedUsername,
                    email = normalizedEmail,
                    phone_number = !string.IsNullOrWhiteSpace(model.PhoneNumber) ? model.PhoneNumber.Trim() : null,
                    password = hashedPassword,
                    role_id = roleId,
                    status = model.Status ?? "active",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                _db.users.Add(newUser);
                _db.SaveChanges();

                TempData["SuccessMessage"] = $"Đã tạo tài khoản '{normalizedUsername}' thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                model.RoleOptions = BuildRoleOptions();
                model.BranchOptions = BuildBranchOptions();
                return View("Form", model);
            }
        }

        // GET: Admin/Users/Edit/{id}
        public ActionResult Edit(long id)
        {
            var user = _db.users.Include("role").FirstOrDefault(u => u.id == id && u.deleted_at == null);
            if (user == null)
            {
                return HttpNotFound();
            }

            var vm = new UserFormViewModel
            {
                Id = user.id,
                Name = user.name,
                FullName = user.full_name,
                Email = user.email,
                PhoneNumber = user.phone_number,
                Role = user.role?.name ?? "client",
                Status = user.status,
                RoleOptions = BuildRoleOptions(),
                BranchOptions = BuildBranchOptions()
            };

            ViewBag.Title = "Cập nhật tài khoản";
            return View("Form", vm);
        }

        // GET: Admin/Users/Details/{id}
        public ActionResult Details(long id)
        {
            var user = _db.users.Include("role").FirstOrDefault(u => u.id == id && u.deleted_at == null);
            if (user == null)
            {
                return HttpNotFound();
            }

            var vm = new UserListItemViewModel
            {
                Id = user.id,
                Name = user.name,
                FullName = user.full_name,
                Email = user.email,
                PhoneNumber = user.phone_number ?? "",
                Role = user.role?.name ?? "client",
                Status = user.status ?? "active",
                CreatedAt = user.created_at.HasValue ? new DateTimeOffset(user.created_at.Value) : DateTimeOffset.MinValue
            };

            return View(vm);
        }

        private string GetSqlRoleType(string appRole)
        {
            switch (appRole?.ToLower())
            {
                case "admin":
                    return "Admin";
                case "warehouse_manager":
                    return "Warehouse";
                case "branch_manager":
                    return "Branch";
                case "client":
                default:
                    return "Customer";
            }
        }

        private UserFormViewModel BuildUserForm(long? id = null)
        {
            return new UserFormViewModel
            {
                Id = id,
                RoleOptions = BuildRoleOptions(),
                BranchOptions = BuildBranchOptions()
            };
        }

        private IEnumerable<SelectOptionViewModel> BuildRoleOptions()
        {
            var roles = _db.roles.Where(r => r.deleted_at == null).ToList();
            return roles.Select(r => new SelectOptionViewModel
            {
                Value = r.name,
                Label = GetRoleLabel(r.name)
            }).ToList();
        }

        private string GetRoleLabel(string roleName)
        {
            switch (roleName?.ToLower())
            {
                case "admin": return "Quản trị viên";
                case "warehouse_manager": return "Quản lý kho";
                case "branch_manager": return "Quản lý chi nhánh";
                case "client": return "Khách hàng";
                default: return roleName;
            }
        }

        private IEnumerable<SelectOptionViewModel> BuildBranchOptions()
        {
            return _db.branches.ToList().Select(b => new SelectOptionViewModel
            {
                Value = b.id.ToString(),
                Label = b.name
            }).ToList();
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

