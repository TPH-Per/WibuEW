using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DoAnLTWHQT.Models
{
    public class ReviewCreateRequest
    {

        [Required(ErrorMessage = "Rating là bắt buộc")]
        [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5")]
        public int Rating { get; set; }

        [MaxLength(2000, ErrorMessage = "Comment không được quá 2000 ký tự")]
        public string Comment { get; set; }

    }
}