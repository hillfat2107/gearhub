using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GearHub.Models.Data;

namespace GearHub.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // Lấy vài mặt hàng vừa thêm để mục nổi bật trên trang chủ luôn được cập nhật.
            ViewBag.FeaturedProducts = ProductCatalogStore.GetProducts(Server.MapPath("~/App_Data/products.json"))
                .OrderByDescending(product => product.CreatedAt)
                .Take(4)
                .ToList();

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
