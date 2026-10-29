using System.Linq;
using System.Web.Mvc;
using GearHub.Models.Data;

namespace GearHub.Controllers
{
    public class ProductController : Controller
    {
        // Mở trang chi tiết dựa trên mã của sản phẩm đã lưu trong danh mục.
        public ActionResult Detail(string id)
        {
            var product = ProductCatalogStore.GetProducts(Server.MapPath("~/App_Data/products.json"))
                .FirstOrDefault(item => item.Id == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.ProductImages = ProductCatalogStore.GetImagePaths(product);
            ViewBag.ProductSpecifications = ProductCatalogStore.GetSpecificationRows(product);
            return View(product);
        }

        // Chọn đúng tên và sản phẩm theo danh mục người dùng đang xem.
        public ActionResult Category(string category)
        {
            var categoryKey = (category ?? "pc-gaming").ToLowerInvariant();

            switch (categoryKey)
            {
                case "pc-linh-kien":
                    ViewBag.CategoryName = "PC & linh kiện";
                    break;
                case "laptop":
                    ViewBag.CategoryName = "Laptop";
                    break;
                case "linh-kien":
                    ViewBag.CategoryName = "Linh kiện";
                    break;
                case "man-hinh":
                    ViewBag.CategoryName = "Màn hình";
                    break;
                case "gaming-gear":
                    ViewBag.CategoryName = "Gaming gear";
                    break;
                case "console":
                    ViewBag.CategoryName = "Máy Game Console";
                    break;
                case "phu-kien":
                    ViewBag.CategoryName = "Phụ kiện";
                    break;
                default:
                    categoryKey = "pc-gaming";
                    ViewBag.CategoryName = "PC Gaming";
                    break;
            }

            ViewBag.CategoryKey = categoryKey;
            ViewBag.Products = ProductCatalogStore.GetProducts(Server.MapPath("~/App_Data/products.json"))
                .Where(product => product.CategoryKey == categoryKey)
                .OrderByDescending(product => product.CreatedAt)
                .ToList();

            return View();
        }
    }
}
