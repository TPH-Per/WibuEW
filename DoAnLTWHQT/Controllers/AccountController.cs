using System;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using DoAnLTWHQT.Security;
using Ltwhqt.ViewModels.Admin;
using BCryptNet = BCrypt.Net.BCrypt;

namespace DoAnLTWHQT.Controllers
{
    public class AccountController : Controller
    {
        private readonly perwEntities _db = new perwEntities();

        // GET: /Account/Login
        [AllowAnonymous]
        [HttpGet]
        [Route("login")]
        public ActionResult Login(string logout = null)
        {
            System.Diagnostics.Debug.WriteLine("========== LOGIN GET CALLED ==========");
            if (!string.IsNullOrEmpty(logout))
            {
                System.Diagnostics.Debug.WriteLine("Logout flag detected - clearing auth state before showing login page.");
                ClearAuthenticationData();
                ViewBag.SuccessMessage = "Bạn đã đăng xuất thành công.";
                // Sau khi xóa auth, hiển thị trang login, không redirect nữa
                return View();
            }

            // If already logged in, redirect to dashboard
            if (User?.Identity?.IsAuthenticated == true)
            {
                var customPrincipal = User as CustomPrincipal;
                // Lấy role từ principal, nếu không có thì mặc định là "admin" để an toàn
                var role = customPrincipal?.Role ?? "admin"; 
                return RedirectToDashboard(role);
            }

            return View();
        }

        // POST: /Account/Login
        [AllowAnonymous]
        [HttpPost]
        [Route("login")]
        public ActionResult Login(FormCollection form)
        {
            System.Diagnostics.Debug.WriteLine("========== LOGIN POST CALLED ==========");
            System.Diagnostics.Debug.WriteLine($"Form Keys: {string.Join(", ", form.AllKeys)}");

            var Email = form["Email"];
            var Password = form["Password"];
            var RememberMe = form["RememberMe"] == "true";

            System.Diagnostics.Debug.WriteLine($"Email: {Email}");
            System.Diagnostics.Debug.WriteLine($"Password Length: {Password?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"RememberMe: {RememberMe}");

            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    ViewBag.ErrorMessage = "Vui lòng nhập email và mật khẩu.";
                    return View();
                }

                // Normalize email
                var normalizedEmail = Email.Trim().ToLowerInvariant();
                System.Diagnostics.Debug.WriteLine($"Normalized Email: {normalizedEmail}");

                // Try to find user in database
                user dbUser = null;
                try
                {
                    dbUser = _db.users
                        .Include(u => u.role)
                        .FirstOrDefault(u => u.email.ToLower() == normalizedEmail && u.deleted_at == null);

                    System.Diagnostics.Debug.WriteLine($"User found in DB: {dbUser != null}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DB Error: {ex.Message}");
                }

                // Check if user exists and password matches
                bool isAuthenticated = false;
                string userRole = null;
                string userEmail = null;

                if (dbUser != null && VerifyPassword(Password, dbUser.password))
                {
                    // Check if account is active
                    if (!string.Equals(dbUser.status, "active", StringComparison.OrdinalIgnoreCase))
                    {
                        ViewBag.ErrorMessage = "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.";
                        return View();
                    }

                    // Validate branch_manager must be assigned to a branch
                    if (string.Equals(dbUser.role?.name, "branch_manager", StringComparison.OrdinalIgnoreCase))
                    {
                        var assignedBranch = _db.branches.FirstOrDefault(b => b.manager_user_id == dbUser.id);
                        if (assignedBranch == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Branch manager {dbUser.email} not assigned to any branch");
                            ViewBag.ErrorMessage = "Tài khoản chưa được phân công quản lý chi nhánh nào. Vui lòng liên hệ quản trị viên.";
                            return View();
                        }
                        System.Diagnostics.Debug.WriteLine($"Branch manager {dbUser.email} assigned to branch: {assignedBranch.name}");
                    }

                    isAuthenticated = true;
                    userRole = dbUser.role?.name ?? "client";
                    userEmail = dbUser.email;
                    System.Diagnostics.Debug.WriteLine($"DB User authenticated. Role: {userRole}");
                }

                if (!isAuthenticated)
                {
                    System.Diagnostics.Debug.WriteLine("Authentication failed");
                    ViewBag.ErrorMessage = "Email hoặc mật khẩu không chính xác.";
                    return View();
                }

                // Create authentication ticket
                System.Diagnostics.Debug.WriteLine($"Creating auth ticket for {userEmail}");

                var ticket = new FormsAuthenticationTicket(
                    1,                                              // version
                    userEmail,                                      // user name
                    DateTime.Now,                                   // issue time
                    DateTime.Now.AddDays(RememberMe ? 7 : 1),     // expiration
                    RememberMe,                                     // persistent
                    userRole,                                       // user data (role)
                    FormsAuthentication.FormsCookiePath            // cookie path
                );

                // Encrypt the ticket
                var encryptedTicket = FormsAuthentication.Encrypt(ticket);

                // Create cookie
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true,
                    Secure = FormsAuthentication.RequireSSL,
                    Path = FormsAuthentication.FormsCookiePath
                };

                if (RememberMe)
                {
                    authCookie.Expires = ticket.Expiration;
                }

                // Add cookie to response
                Response.Cookies.Add(authCookie);

                System.Diagnostics.Debug.WriteLine($"Cookie added: {FormsAuthentication.FormsCookieName}");
                System.Diagnostics.Debug.WriteLine($"Redirecting to dashboard for role: {userRole}");

                // Redirect to appropriate dashboard
                return RedirectToDashboard(userRole);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                ViewBag.ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
                return View();
            }
        }

        // GET: /Account/Logout
        [Authorize]
        [Route("logout")]
        public ActionResult Logout()
        {
            // Sign out and clear all authentication data
            ClearAuthenticationData();
        
            // Redirect to login page with logout parameter
            return RedirectToAction("Login", new { logout = "success" });
        }
        
        // POST: /Account/Logout
        [HttpPost]
        [Authorize]
        [Route("logout")]
        [ValidateAntiForgeryToken]
        public ActionResult LogoutPost() // Đổi tên để tránh trùng lặp với GET
        {
            // Sign out and clear all authentication data
            ClearAuthenticationData();
        
            // Redirect to login page with logout parameter
            return RedirectToAction("Login", new { logout = "success" });
        }

        // GET: /Account/Register - Form đăng ký (CaoQuocPhu)
        [AllowAnonymous]
        [HttpGet]
        [Route("register")]
        public ActionResult Register()
        {
            // Nếu đã đăng nhập, redirect về dashboard
            if (User?.Identity?.IsAuthenticated == true)
            {
                var customPrincipal = User as CustomPrincipal;
                var role = customPrincipal?.Role ?? "admin";
                return RedirectToDashboard(role);
            }

            return View(new RegisterViewModel());
        }

        // POST: /Account/Register - Xử lý đăng ký (CaoQuocPhu)
        [AllowAnonymous]
        [HttpPost]
        [Route("register")]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("========== REGISTER POST CALLED ==========");

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Kiểm tra email đã tồn tại
                var normalizedEmail = model.Email.Trim().ToLowerInvariant();
                var existingUser = _db.users.FirstOrDefault(u => u.email.ToLower() == normalizedEmail && u.deleted_at == null);
                if (existingUser != null)
                {
                    ViewBag.ErrorMessage = "Email này đã được đăng ký. Vui lòng sử dụng email khác.";
                    return View(model);
                }

                // Kiểm tra username đã tồn tại
                var existingUsername = _db.users.FirstOrDefault(u => u.name.ToLower() == model.Username.ToLower() && u.deleted_at == null);
                if (existingUsername != null)
                {
                    ViewBag.ErrorMessage = "Tên đăng nhập này đã tồn tại. Vui lòng chọn tên khác.";
                    return View(model);
                }

                // Lấy role Customer (mặc định cho người đăng ký công khai)
                var customerRole = _db.roles.FirstOrDefault(r => r.name == "client" || r.name == "customer");
                if (customerRole == null)
                {
                    ViewBag.ErrorMessage = "Lỗi hệ thống: Không tìm thấy role phù hợp.";
                    return View(model);
                }

                // Hash mật khẩu
                var hashedPassword = BCryptNet.HashPassword(model.Password);

                // Tạo user mới
                var newUser = new user
                {
                    name = model.Username,
                    full_name = model.FullName,
                    email = model.Email,
                    password = hashedPassword,
                    phone_number = model.PhoneNumber,
                    role_id = customerRole.id,
                    status = "active",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                _db.users.Add(newUser);
                _db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"User created: {newUser.email} with ID: {newUser.id}");

                // Gọi stored procedure tạo SQL User (nếu cần)
                try
                {
                    CreateSQLUser(model.Username, model.Password, "Customer");
                }
                catch (Exception spEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SP Error (non-critical): {spEx.Message}");
                    // Không throw lỗi vì user đã được tạo trong DB
                }

                ViewBag.SuccessMessage = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Register error: {ex.Message}");
                ViewBag.ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
                return View(model);
            }
        }

        // Gọi stored procedure sp_System_CreateSQLUser (CaoQuocPhu)
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

        // GET: /Account/TestPost - Simple test page
        [AllowAnonymous]
        [HttpGet]
        public ActionResult TestPost()
        {
            return Content(@"
<!DOCTYPE html>
<html>
<head><title>POST Test</title></head>
<body>
    <h1>Simple POST Test</h1>
    <form method='post' action='/Account/TestPost'>
        <input type='text' name='testfield' placeholder='Type something' />
        <button type='submit'>Submit</button>
    </form>
</body>
</html>
", "text/html");
        }

        // POST: /Account/TestPost - Test if POST works at all
        [AllowAnonymous]
        [HttpPost]
        public ActionResult TestPost(FormCollection form)
        {
            System.Diagnostics.Debug.WriteLine("========== TEST POST CALLED ==========");
            System.Diagnostics.Debug.WriteLine($"Form data: {form["testfield"]}");
            return Content($"POST WORKS! Received: {form["testfield"]}", "text/html");
        }

        // GET: /Account/TestDb - Database diagnostic
        [AllowAnonymous]
        public ActionResult TestDb()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== Database Connection Test ===\n");

            try
            {
                result.AppendLine($"Connection String: {_db.Database.Connection.ConnectionString}\n");

                var userCount = _db.users.Count();
                result.AppendLine($"Total users: {userCount}");

                var activeUsers = _db.users.Where(u => u.deleted_at == null).ToList();
                result.AppendLine($"Active users: {activeUsers.Count}\n");

                result.AppendLine("First 5 active users:");
                foreach (var u in activeUsers.Take(5))
                {
                    result.AppendLine($"  - {u.email} | Status: {u.status} | Role ID: {u.role_id}");
                }

                result.AppendLine("\n--- Test Account Check ---");
                var testUser = _db.users.FirstOrDefault(u => u.email.ToLower() == "admin@test.local");
                if (testUser != null)
                {
                    result.AppendLine($"Email: {testUser.email}");
                    result.AppendLine($"Status: {testUser.status}");
                    result.AppendLine($"Role ID: {testUser.role_id}");
                    result.AppendLine($"Password Hash: {testUser.password?.Substring(0, Math.Min(30, testUser.password?.Length ?? 0))}...");
                }
                else
                {
                    result.AppendLine("Admin test account NOT FOUND in database");
                }
                
                var warehouseUser = _db.users.FirstOrDefault(u => u.email.ToLower() == "warehouse@test.local");
                if (warehouseUser != null)
                {
                    result.AppendLine($"\nWarehouse Manager Email: {warehouseUser.email}");
                    result.AppendLine($"Status: {warehouseUser.status}");
                    result.AppendLine($"Role ID: {warehouseUser.role_id}");
                    result.AppendLine($"Password Hash: {warehouseUser.password?.Substring(0, Math.Min(30, warehouseUser.password?.Length ?? 0))}...");
                }
                else
                {
                    result.AppendLine("\nWarehouse manager test account NOT FOUND in database");
                }
                
                var branchUser = _db.users.FirstOrDefault(u => u.email.ToLower() == "branch@test.local");
                if (branchUser != null)
                {
                    result.AppendLine($"\nBranch Manager Email: {branchUser.email}");
                    result.AppendLine($"Status: {branchUser.status}");
                    result.AppendLine($"Role ID: {branchUser.role_id}");
                    result.AppendLine($"Password Hash: {branchUser.password?.Substring(0, Math.Min(30, branchUser.password?.Length ?? 0))}...");
                }
                else
                {
                    result.AppendLine("\nBranch manager test account NOT FOUND in database");
                }
                
                result.AppendLine("\n--- Role Check ---");
                var adminRole = _db.roles.FirstOrDefault(r => r.name == "admin");
                var warehouseRole = _db.roles.FirstOrDefault(r => r.name == "warehouse_manager");
                var branchRole = _db.roles.FirstOrDefault(r => r.name == "branch_manager");
                
                result.AppendLine($"Admin Role: {(adminRole != null ? adminRole.name : "NOT FOUND")}");
                result.AppendLine($"Warehouse Manager Role: {(warehouseRole != null ? warehouseRole.name : "NOT FOUND")}");
                result.AppendLine($"Branch Manager Role: {(branchRole != null ? branchRole.name : "NOT FOUND")}");
            }
            catch (Exception ex)
            {
                result.AppendLine($"\nERROR: {ex.Message}");
                result.AppendLine($"\nStack: {ex.StackTrace}");
            }

            return Content(result.ToString(), "text/plain");
        }

        private ActionResult RedirectToDashboard(string role)
        {
            System.Diagnostics.Debug.WriteLine($"RedirectToDashboard called with role: {role}");

            if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (string.Equals(role, "warehouse_manager", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Warehouse" });
            }

            if (string.Equals(role, "branch_manager", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Branch" });
            }

            return RedirectToAction("Index", "Home");
        }

        private bool VerifyPassword(string providedPassword, string storedPassword)
        {
            if (string.IsNullOrWhiteSpace(storedPassword) || string.IsNullOrWhiteSpace(providedPassword))
            {
                return false;
            }

            // Plain text comparison (for testing)
            if (string.Equals(storedPassword, providedPassword))
            {
                return true;
            }

            // BCrypt hash
            if (storedPassword.StartsWith("$2"))
            {
                try
                {
                    return BCryptNet.Verify(providedPassword, storedPassword);
                }
                catch
                {
                    return false;
                }
            }

            // ASP.NET Crypto hash
            try
            {
                return Crypto.VerifyHashedPassword(storedPassword, providedPassword);
            }
            catch
            {
                return false;
            }
        }

        private void ClearAuthenticationData()
        {
            // Sign out from Forms Authentication
            FormsAuthentication.SignOut();

            // Clear session data
            Session.Clear();
            Session.Abandon();

            // Delete authentication cookie
            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, "")
                {
                    Expires = DateTime.Now.AddYears(-1),
                    HttpOnly = true,
                    Secure = FormsAuthentication.RequireSSL,
                    Path = FormsAuthentication.FormsCookiePath
                };
                Response.Cookies.Add(cookie);
            }

            // Also delete ASP.NET session cookie
            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                var sessionCookie = new HttpCookie("ASP.NET_SessionId", "")
                {
                    Expires = DateTime.Now.AddYears(-1),
                    HttpOnly = true,
                    Path = "/"
                };
                Response.Cookies.Add(sessionCookie);
            }

            // Add cache control headers to prevent caching of protected pages
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
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
