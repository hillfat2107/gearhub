using System.Web.Mvc;

namespace GearHub.Controllers
{
    public class AccountController : Controller
    {
        private const string DemoUserName = "admin";
        private const string DemoPassword = "admin";
        private const string UserSessionKey = "GearHubUser";

        // Mở trang đăng nhập để người dùng vào khu vực tài khoản.
        public ActionResult Login()
        {
            return View();
        }

        // Kiểm tra tài khoản mẫu và giữ trạng thái đăng nhập trong phiên trình duyệt.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (username == DemoUserName && password == DemoPassword)
            {
                Session[UserSessionKey] = DemoUserName;
                return RedirectToAction("OrderHistory");
            }

            ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu chưa đúng.";
            return View();
        }

        // Trang này giới thiệu tài khoản mẫu đã có sẵn, không cần nhập email hay số điện thoại.
        public ActionResult Register()
        {
            return View();
        }

        // Mở trang lấy lại mật khẩu khi người dùng quên thông tin đăng nhập.
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // Mở danh sách đơn hàng minh họa của tài khoản.
        public ActionResult OrderHistory()
        {
            ViewBag.UserName = Session[UserSessionKey] as string;
            return View();
        }

        // Mở trang chi tiết của một đơn hàng.
        public ActionResult OrderDetail(string id)
        {
            ViewBag.OrderId = string.IsNullOrWhiteSpace(id) ? "GH-DEMO-001" : id;
            return View();
        }
    }
}