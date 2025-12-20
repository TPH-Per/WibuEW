using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    [Authorize(Roles = "branch_manager")]
    public abstract class BranchBaseController : Controller
    {
        protected readonly perwEntities _db = new perwEntities();

        /// <summary>
        /// Lấy branch ID của user đang đăng nhập
        /// </summary>
        protected long? GetCurrentBranchId()
        {
            var userEmail = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return null;

            var user = _db.users.FirstOrDefault(u => u.email.ToLower() == userEmail.ToLower() && u.deleted_at == null);
            if (user == null) return null;

            var branch = _db.branches.FirstOrDefault(b => b.manager_user_id == user.id);
            return branch?.id;
        }

        /// <summary>
        /// Lấy thông tin branch của user đang đăng nhập
        /// </summary>
        protected branch GetCurrentBranch()
        {
            var userEmail = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return null;

            var user = _db.users.FirstOrDefault(u => u.email.ToLower() == userEmail.ToLower() && u.deleted_at == null);
            if (user == null) return null;

            return _db.branches.FirstOrDefault(b => b.manager_user_id == user.id);
        }

        /// <summary>
        /// Lấy user ID của user đang đăng nhập
        /// </summary>
        protected long? GetCurrentUserId()
        {
            var userEmail = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return null;

            var user = _db.users.FirstOrDefault(u => u.email.ToLower() == userEmail.ToLower() && u.deleted_at == null);
            return user?.id;
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            // TODO: add shared branch logging/handling
            base.OnException(filterContext);
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
