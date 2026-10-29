using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GearHub.Models.Entities
{
    // Thông tin sản phẩm do trang quản trị nhập và lưu lại cho GearHub.
    public class AdminProduct
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(120, ErrorMessage = "Tên sản phẩm không được dài quá 120 ký tự.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        public string CategoryKey { get; set; }

        public string CategoryName { get; set; }

        [Range(typeof(decimal), "1", "999999999", ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập phần mô tả.")]
        [StringLength(1000, ErrorMessage = "Mô tả không được dài quá 1000 ký tự.")]
        public string Description { get; set; }

        [StringLength(6000, ErrorMessage = "Thông tin cấu hình quá dài.")]
        public string Specifications { get; set; }

        // Mỗi dòng gồm tên linh kiện và phần thông tin sẽ hiện thành một hàng trên trang chi tiết.
        public List<ProductSpec> SpecificationRows { get; set; }

        public string ImagePath { get; set; }

        // Ảnh đầu tiên làm ảnh đại diện; các ảnh còn lại dùng trong thư viện ở trang chi tiết.
        public List<string> ImagePaths { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
