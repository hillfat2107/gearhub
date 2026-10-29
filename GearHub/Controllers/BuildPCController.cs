using System.Web.Mvc;

namespace GearHub.Controllers
{
    public class BuildPCController : Controller
    {
        // Mở trang chọn linh kiện để tạo cấu hình PC.
        public ActionResult Index()
        {
            return View();
        }
    }
}