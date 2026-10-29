using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using GearHub.Models.Data;
using GearHub.Models.Entities;
using GearHub.Models.ViewModels;

namespace GearHub.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "GearHubCart";
        private const string PaymentSessionKey = "GearHubPaymentMethod";
        private const string ShippingOriginDistrict = "Bình Thạnh";
        private const string ShippingOriginCity = "TP. Hồ Chí Minh";

        // Mở giỏ và lấy các mặt hàng đang được giữ trong phiên làm việc của người mua.
        public ActionResult Index()
        {
            return View(GetCart());
        }

        // Tìm giá và thông tin từ danh sách sản phẩm, tránh lấy các giá trị tự gửi từ trình duyệt.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(string productId, int quantity = 1)
        {
            var product = ProductCatalogStore.GetProducts(Server.MapPath("~/App_Data/products.json"))
                .FirstOrDefault(item => item.Id == productId);

            if (product == null)
            {
                return HttpNotFound();
            }

            quantity = System.Math.Min(System.Math.Max(quantity, 1), 99);
            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(item => item.ProductId == product.Id);

            if (existingItem == null)
            {
                cart.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    CategoryName = product.CategoryName,
                    Description = product.Description,
                    Specifications = product.Specifications,
                    ImagePath = product.ImagePath,
                    Price = product.Price,
                    Quantity = quantity
                });
            }
            else
            {
                existingItem.Quantity = System.Math.Min(existingItem.Quantity + quantity, 99);
            }

            Session[CartSessionKey] = cart;
            TempData["CartMessage"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction("Index");
        }

        // Cho phép bỏ một mặt hàng ra khỏi giỏ nếu người mua đổi ý.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(string productId)
        {
            var cart = GetCart();
            cart.RemoveAll(item => item.ProductId == productId);
            Session[CartSessionKey] = cart;
            return RedirectToAction("Index");
        }

        // Ghi nhớ cách thanh toán đã chọn rồi mở biểu mẫu nhập địa chỉ giao hàng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string paymentMethod)
        {
            if (paymentMethod != "momo" && paymentMethod != "cod")
            {
                paymentMethod = "cod";
            }

            Session[PaymentSessionKey] = paymentMethod;
            return RedirectToAction("Checkout");
        }

        // Mở form để người mua tự nhập địa chỉ nhận; địa điểm xuất hàng chỉ dùng khi ước tính phí.
        [HttpGet]
        public ActionResult Checkout()
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            var paymentMethod = Session[PaymentSessionKey] as string ?? "cod";
            PrepareCheckoutView(cart, paymentMethod, null, null, null, null, null, null, null);
            return View(cart);
        }

        // Kiểm tra thông tin giao hàng rồi hiện trang xác nhận đơn minh họa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string fullName, string phone, string streetAddress, string ward,
            string district, string city, string paymentMethod, string note)
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError("fullName", "Vui lòng nhập tên người nhận.");
            }

            var digitsOnlyPhone = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digitsOnlyPhone.Length < 9 || digitsOnlyPhone.Length > 11)
            {
                ModelState.AddModelError("phone", "Số điện thoại cần có từ 9 đến 11 chữ số.");
            }

            if (string.IsNullOrWhiteSpace(streetAddress))
            {
                ModelState.AddModelError("streetAddress", "Vui lòng nhập số nhà và tên đường.");
            }

            if (string.IsNullOrWhiteSpace(ward))
            {
                ModelState.AddModelError("ward", "Vui lòng nhập phường hoặc xã.");
            }

            if (string.IsNullOrWhiteSpace(district))
            {
                ModelState.AddModelError("district", "Vui lòng nhập quận hoặc huyện.");
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                ModelState.AddModelError("city", "Vui lòng nhập tỉnh hoặc thành phố.");
            }

            if (paymentMethod != "momo" && paymentMethod != "cod")
            {
                ModelState.AddModelError("paymentMethod", "Vui lòng chọn phương thức thanh toán.");
            }

            if (!ModelState.IsValid)
            {
                PrepareCheckoutView(cart, paymentMethod, fullName, phone, streetAddress, ward, district, city, note);
                return View("Checkout", cart);
            }

            var itemTotal = cart.Sum(item => item.LineTotal);
            var shippingFee = CalculateShippingFee(district, city).Value;
            TempData["OrderNumber"] = "GH-" + DateTime.Now.ToString("yyMMdd-HHmmss");
            TempData["OrderFullName"] = fullName.Trim();
            TempData["OrderPhone"] = phone.Trim();
            TempData["OrderAddress"] = string.Join(", ", streetAddress.Trim(), ward.Trim(), district.Trim(), city.Trim());
            TempData["OrderPayment"] = paymentMethod == "momo" ? "Ví MoMo (minh họa)" : "Thanh toán khi nhận hàng";
            TempData["OrderItemTotal"] = itemTotal;
            TempData["OrderShippingFee"] = shippingFee;
            TempData["OrderTotal"] = itemTotal + shippingFee;
            TempData["OrderNote"] = string.IsNullOrWhiteSpace(note) ? "Không có" : note.Trim();

            Session.Remove(CartSessionKey);
            Session.Remove(PaymentSessionKey);
            return RedirectToAction("Success");
        }

        // Tạo trang xác nhận sau khi thông tin giao hàng đã hợp lệ.
        [HttpGet]
        public ActionResult Success()
        {
            if (TempData["OrderNumber"] == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.OrderNumber = TempData["OrderNumber"];
            ViewBag.OrderFullName = TempData["OrderFullName"];
            ViewBag.OrderPhone = TempData["OrderPhone"];
            ViewBag.OrderAddress = TempData["OrderAddress"];
            ViewBag.OrderPayment = TempData["OrderPayment"];
            ViewBag.OrderItemTotal = TempData["OrderItemTotal"];
            ViewBag.OrderShippingFee = TempData["OrderShippingFee"];
            ViewBag.OrderTotal = TempData["OrderTotal"];
            ViewBag.OrderNote = TempData["OrderNote"];
            return View();
        }

        // Giữ lại biểu mẫu đã điền nếu người mua cần sửa thông tin chưa hợp lệ.
        private void PrepareCheckoutView(IList<CartItemViewModel> cart, string paymentMethod, string fullName,
            string phone, string streetAddress, string ward, string district, string city, string note)
        {
            ViewBag.CartTotal = cart.Sum(item => item.LineTotal);
            ViewBag.PaymentMethod = paymentMethod == "momo" ? "momo" : "cod";
            ViewBag.FullName = fullName;
            ViewBag.Phone = phone;
            ViewBag.StreetAddress = streetAddress;
            ViewBag.Ward = ward;
            ViewBag.District = district;
            ViewBag.City = city;
            ViewBag.Note = note;
            ViewBag.ShippingFee = CalculateShippingFee(district, city);
            ViewBag.OrderTotal = ViewBag.CartTotal + ((decimal?)ViewBag.ShippingFee ?? 0m);
        }

        // Ước tính từ điểm xuất hàng nội bộ đến địa chỉ người mua; mức phí này chỉ là ví dụ, chưa gọi bản đồ.
        private static decimal? CalculateShippingFee(string district, string city)
        {
            if (string.IsNullOrWhiteSpace(district) || string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            var destinationCity = NormalizeLocation(city);
            var destinationDistrict = NormalizeLocation(district);
            var originCity = NormalizeLocation(ShippingOriginCity).Replace("tp", "").Trim();
            var originDistrict = NormalizeLocation(ShippingOriginDistrict);
            var isSameCity = destinationCity.Contains(originCity)
                || destinationCity.Contains("hcm")
                || destinationCity.Contains("sai gon");

            if (isSameCity)
            {
                return destinationDistrict.Contains(originDistrict) ? 15000m : 25000m;
            }

            return 45000m;
        }

        // Bỏ dấu để người mua có thể nhập tên tỉnh thành có hoặc không có dấu tiếng Việt.
        private static string NormalizeLocation(string value)
        {
            var decomposed = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
            var plainText = new string(decomposed
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            var normalizedText = plainText.Replace('đ', 'd')
                .Replace('Đ', 'D')
                .ToLowerInvariant()
                .Replace(".", " ");

            return string.Join(" ", normalizedText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }

        // Tạo giỏ trống lần đầu và dùng lại danh sách trong session ở các trang kế tiếp.
        private List<CartItemViewModel> GetCart()
        {
            var cart = Session[CartSessionKey] as List<CartItemViewModel>;
            if (cart == null)
            {
                cart = new List<CartItemViewModel>();
                Session[CartSessionKey] = cart;
            }

            return cart;
        }
    }
}
