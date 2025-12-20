using Ltwhqt.ViewModels.Shared;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ltwhqt.ViewModels.Admin
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public IList<AlertViewModel> Alerts { get; set; } = new List<AlertViewModel>();
    }
}
