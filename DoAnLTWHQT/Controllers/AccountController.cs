using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using DoAnLTWHQT.Security;
using BCryptNet = BCrypt.Net.BCrypt;

namespace DoAnLTWHQT.Controllers
{
    public class AccountController : Controller
    {
        private readonly Entities _db = new Entities();

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
