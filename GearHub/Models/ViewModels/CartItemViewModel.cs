using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GearHub.Models.ViewModels
{
    // Giữ lại những thông tin cần thiết để vẽ một dòng sản phẩm trong giỏ.
    public class CartItemViewModel
    {
        public string ProductId { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public string Specifications { get; set; }
        public string ImagePath { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        // Tính tiền của riêng mặt hàng này dựa trên giá và số lượng.
        public decimal LineTotal
        {
            get { return Price * Quantity; }
        }
    }
}
