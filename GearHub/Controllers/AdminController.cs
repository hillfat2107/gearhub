using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GearHub.Models.Data;
using GearHub.Models.Entities;

namespace GearHub.Controllers
{
    public class AdminController : Controller
    {
        private const string SessionKey = "GearHubAdmin";
        private const int MaxImageSize = 5 * 1024 * 1024;
        private const int MaxImageCount = 14;

        // Danh mục này được dùng chung cho ô chọn trong trang thêm sản phẩm.
        private static readonly CategoryOption[] Categories =
        {
            new CategoryOption("pc-gaming", "PC Gaming"),
            new CategoryOption("laptop", "Laptop"),
            new CategoryOption("linh-kien", "Linh kiện"),
            new CategoryOption("man-hinh", "Màn hình"),
            new CategoryOption("gaming-gear", "Gaming gear"),
            new CategoryOption("phu-kien", "Phụ kiện"),
            new CategoryOption("console", "Máy Game Console"),
            new CategoryOption("pc-linh-kien", "PC & linh kiện")
        };

        // Mở màn hình đăng nhập; nếu đã đăng nhập thì chuyển thẳng tới danh sách hàng.
        public ActionResult Login()
        {
            if (IsAdmin())
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // So sánh thông tin đăng nhập demo rồi lưu trạng thái trong phiên hiện tại.
        public ActionResult Login(string username, string password)
        {
            if (username == "admin" && password == "admin")
            {
                Session[SessionKey] = true;
                return RedirectToAction("Index");
            }

            ViewBag.ErrorMessage = "Tài khoản hoặc mật khẩu chưa đúng.";
            return View();
        }

        // Chỉ tài khoản đã đăng nhập mới xem được danh sách sản phẩm đã lưu.
        public ActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            var products = ProductCatalogStore.GetProducts(CatalogFilePath())
                .OrderByDescending(product => product.CreatedAt)
                .ToList();

            return View(products);
        }

        // Mở biểu mẫu nhập thông tin và cấu hình sản phẩm.
        public ActionResult Create()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            return View(new AdminProduct { SpecificationRows = CreateBlankSpecificationRows(5) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Kiểm tra thông tin, lưu ảnh vào thư mục hình và ghi mặt hàng vào danh sách.
        public ActionResult Create(AdminProduct product, HttpPostedFileBase[] imageFiles)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            var category = Categories.FirstOrDefault(item => item.Key == product.CategoryKey);
            if (category == null)
            {
                ModelState.AddModelError("CategoryKey", "Vui lòng chọn một danh mục có sẵn.");
            }

            PrepareSpecifications(product);
            var uploadedImages = GetUploadedImages(imageFiles);
            ValidateProductImages(uploadedImages, true);

            if (!ModelState.IsValid || !string.IsNullOrEmpty(ViewBag.ImageError as string))
            {
                return View(product);
            }

            var imagePaths = uploadedImages.Select(SaveProductImage).ToList();

            product.Id = Guid.NewGuid().ToString("N");
            product.CategoryName = category.Name;
            product.ImagePath = imagePaths[0];
            product.ImagePaths = imagePaths;
            product.CreatedAt = DateTime.Now;
            ProductCatalogStore.AddProduct(CatalogFilePath(), product);

            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào GearHub.";
            return RedirectToAction("Index");
        }

        // Lấy thông tin đang lưu để điền sẵn vào biểu mẫu sửa.
        public ActionResult Edit(string id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            var product = FindProduct(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            product.SpecificationRows = ProductCatalogStore.GetSpecificationRows(product);
            if (product.SpecificationRows.Count == 0)
            {
                product.SpecificationRows = CreateBlankSpecificationRows(5);
            }

            PrepareEditView(product, product.CategoryKey);
            return View(product);
        }

        // Cập nhật thông tin, giữ các ảnh đã chọn và nối thêm những ảnh mới.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,CategoryKey,Price,Description,SpecificationRows")] AdminProduct product, HttpPostedFileBase[] imageFiles, string[] keepImagePaths, bool imageListSubmitted = false)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            var existingProduct = FindProduct(product.Id);
            if (existingProduct == null)
            {
                return HttpNotFound();
            }

            var category = Categories.FirstOrDefault(item => item.Key == product.CategoryKey);
            if (category == null)
            {
                ModelState.AddModelError("CategoryKey", "Vui lòng chọn một danh mục có sẵn.");
            }

            PrepareSpecifications(product);
            var oldImagePaths = ProductCatalogStore.GetImagePaths(existingProduct);
            var requestedImagePaths = new System.Collections.Generic.HashSet<string>(
                keepImagePaths ?? new string[0], StringComparer.OrdinalIgnoreCase);
            var retainedImagePaths = imageListSubmitted
                ? oldImagePaths.Where(path => requestedImagePaths.Contains(path)).ToList()
                : oldImagePaths;

            var uploadedImages = GetUploadedImages(imageFiles);
            ValidateProductImages(uploadedImages, false);

            if (string.IsNullOrEmpty(ViewBag.ImageError as string))
            {
                if (retainedImagePaths.Count + uploadedImages.Count == 0)
                {
                    ViewBag.ImageError = "Sản phẩm cần có ít nhất một ảnh. Hãy giữ lại ảnh cũ hoặc tải ảnh mới lên.";
                }
                else if (retainedImagePaths.Count + uploadedImages.Count > MaxImageCount)
                {
                    ViewBag.ImageError = "Tổng số ảnh cũ được giữ và ảnh mới không được vượt quá 14.";
                }
            }

            if (!ModelState.IsValid || !string.IsNullOrEmpty(ViewBag.ImageError as string))
            {
                PrepareEditView(existingProduct, product.CategoryKey, retainedImagePaths);
                return View(product);
            }

            var newImagePaths = uploadedImages.Select(SaveProductImage).ToList();
            var imagePaths = retainedImagePaths.Concat(newImagePaths).ToList();

            var updatedProduct = new AdminProduct
            {
                Id = existingProduct.Id,
                Name = product.Name,
                CategoryKey = product.CategoryKey,
                CategoryName = category.Name,
                Price = product.Price,
                Description = product.Description,
                Specifications = product.Specifications,
                SpecificationRows = product.SpecificationRows,
                ImagePath = imagePaths.FirstOrDefault(),
                ImagePaths = imagePaths,
                CreatedAt = existingProduct.CreatedAt
            };

            if (!ProductCatalogStore.UpdateProduct(CatalogFilePath(), updatedProduct))
            {
                DeleteProductImages(newImagePaths);
                return HttpNotFound();
            }

            // Chỉ xóa file của những ảnh cũ đã bị bỏ khỏi danh sách sau khi dữ liệu lưu thành công.
            DeleteProductImages(oldImagePaths.Except(retainedImagePaths, StringComparer.OrdinalIgnoreCase));

            TempData["SuccessMessage"] = "Đã cập nhật sản phẩm.";
            return RedirectToAction("Index");
        }

        // Chỉ xóa khi quản trị viên xác nhận trên danh sách sản phẩm.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            var deletedProduct = ProductCatalogStore.DeleteProduct(CatalogFilePath(), id);
            if (deletedProduct == null)
            {
                return HttpNotFound();
            }

            DeleteProductImages(ProductCatalogStore.GetImagePaths(deletedProduct));
            TempData["SuccessMessage"] = "Đã xóa sản phẩm.";
            return RedirectToAction("Index");
        }

        // Xóa phiên đăng nhập khi quản trị viên thoát khỏi trang quản lý.
        public ActionResult Logout()
        {
            Session.Remove(SessionKey);
            return RedirectToAction("Login");
        }

        // File dữ liệu nằm trong App_Data nên không được trình duyệt truy cập trực tiếp.
        private string CatalogFilePath()
        {
            return Server.MapPath("~/App_Data/products.json");
        }

        private AdminProduct FindProduct(string id)
        {
            return ProductCatalogStore.GetProducts(CatalogFilePath())
                .FirstOrDefault(product => product.Id == id);
        }

        private void PrepareEditView(AdminProduct product, string selectedCategoryKey, System.Collections.Generic.IEnumerable<string> currentImagePaths = null)
        {
            ViewBag.CategoryOptions = new SelectList(Categories, "Key", "Name", selectedCategoryKey);
            ViewBag.CurrentImagePaths = currentImagePaths == null
                ? ProductCatalogStore.GetImagePaths(product)
                : currentImagePaths.ToList();
        }

        // Chỉ nhận các dòng cấu hình có đủ tên linh kiện và nội dung để bảng không có ô trống.
        private void PrepareSpecifications(AdminProduct product)
        {
            var rows = product.SpecificationRows ?? new System.Collections.Generic.List<ProductSpec>();
            var enteredRows = rows
                .Where(row => row != null && (!string.IsNullOrWhiteSpace(row.Name) || !string.IsNullOrWhiteSpace(row.Value)))
                .ToList();

            if (enteredRows.Count == 0)
            {
                ModelState.AddModelError("SpecificationRows", "Vui lòng nhập ít nhất một dòng cấu hình.");
            }

            if (enteredRows.Count > 16)
            {
                ModelState.AddModelError("SpecificationRows", "Mỗi sản phẩm nhận tối đa 16 dòng cấu hình.");
            }

            if (enteredRows.Any(row => string.IsNullOrWhiteSpace(row.Name) || string.IsNullOrWhiteSpace(row.Value)))
            {
                ModelState.AddModelError("SpecificationRows", "Mỗi dòng cần có cả tên linh kiện và thông tin chi tiết.");
            }

            if (enteredRows.Any(row => (row.Name ?? string.Empty).Trim().Length > 50 || (row.Value ?? string.Empty).Trim().Length > 250))
            {
                ModelState.AddModelError("SpecificationRows", "Tên linh kiện tối đa 50 ký tự, thông tin chi tiết tối đa 250 ký tự.");
            }

            var completeRows = enteredRows
                .Where(row => !string.IsNullOrWhiteSpace(row.Name) && !string.IsNullOrWhiteSpace(row.Value))
                .Select(row => new ProductSpec { Name = row.Name.Trim(), Value = row.Value.Trim() })
                .ToList();

            product.SpecificationRows = rows.Count == 0 ? CreateBlankSpecificationRows(5) : rows;
            if (enteredRows.Count > 0
                && completeRows.Count <= 16
                && completeRows.All(row => row.Name.Length <= 50 && row.Value.Length <= 250)
                && enteredRows.All(row => !string.IsNullOrWhiteSpace(row.Name) && !string.IsNullOrWhiteSpace(row.Value)))
            {
                product.SpecificationRows = completeRows;
                product.Specifications = string.Join(Environment.NewLine, completeRows.Select(row => row.Name + ": " + row.Value));
            }
        }

        // Mở sẵn vài dòng để quản trị viên chỉ việc điền thông số của sản phẩm.
        private static System.Collections.Generic.List<ProductSpec> CreateBlankSpecificationRows(int count)
        {
            return Enumerable.Range(0, count)
                .Select(index => new ProductSpec())
                .ToList();
        }

        // Tên file được tạo ngẫu nhiên để ảnh mới không ghi đè lên ảnh khác.
        private string SaveProductImage(HttpPostedFileBase imageFile)
        {
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var imageFolder = Server.MapPath("~/wwwroot/images/products");
            Directory.CreateDirectory(imageFolder);

            var imageFileName = Guid.NewGuid().ToString("N") + extension;
            imageFile.SaveAs(Path.Combine(imageFolder, imageFileName));
            return "~/wwwroot/images/products/" + imageFileName;
        }

        // Bỏ các ô tải lên còn trống để chỉ xử lý đúng những ảnh đã được chọn.
        private static System.Collections.Generic.List<HttpPostedFileBase> GetUploadedImages(HttpPostedFileBase[] imageFiles)
        {
            return (imageFiles ?? new HttpPostedFileBase[0])
                .Where(image => image != null && image.ContentLength > 0)
                .ToList();
        }

        // Mỗi sản phẩm nhận tối đa 14 ảnh, mỗi ảnh tối đa 5 MB.
        private void ValidateProductImages(System.Collections.Generic.IList<HttpPostedFileBase> images, bool required)
        {
            if (images.Count == 0 && required)
            {
                ViewBag.ImageError = "Vui lòng chọn ít nhất một ảnh sản phẩm.";
            }
            else if (images.Count > MaxImageCount)
            {
                ViewBag.ImageError = "Mỗi sản phẩm chỉ nhận tối đa 14 ảnh.";
            }
            else if (images.Any(image => image.ContentLength > MaxImageSize))
            {
                ViewBag.ImageError = "Mỗi ảnh cần nhỏ hơn 5 MB.";
            }
            else if (images.Any(image => !IsSupportedImage(Path.GetExtension(image.FileName).ToLowerInvariant())))
            {
                ViewBag.ImageError = "Chỉ nhận ảnh JPG, PNG hoặc WEBP.";
            }
        }

        // Gỡ cả dãy ảnh khi thay ảnh hoặc xóa sản phẩm.
        private void DeleteProductImages(System.Collections.Generic.IEnumerable<string> imagePaths)
        {
            foreach (var imagePath in imagePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                DeleteProductImage(imagePath);
            }
        }

        // Chỉ gỡ ảnh có tên file nằm trong thư mục ảnh sản phẩm của trang.
        private void DeleteProductImage(string imagePath)
        {
            const string imagePrefix = "~/wwwroot/images/products/";
            if (string.IsNullOrEmpty(imagePath) || !imagePath.StartsWith(imagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName = imagePath.Substring(imagePrefix.Length);
            if (Path.GetFileName(fileName) != fileName)
            {
                return;
            }

            var imageFilePath = Path.Combine(Server.MapPath("~/wwwroot/images/products"), fileName);
            if (System.IO.File.Exists(imageFilePath))
            {
                System.IO.File.Delete(imageFilePath);
            }
        }

        private bool IsAdmin()
        {
            return Session[SessionKey] is bool && (bool)Session[SessionKey];
        }

        private static bool IsSupportedImage(string extension)
        {
            return extension == ".jpg" || extension == ".jpeg"
                || extension == ".png" || extension == ".webp";
        }

        private class CategoryOption
        {
            public CategoryOption(string key, string name)
            {
                Key = key;
                Name = name;
            }

            public string Key { get; private set; }
            public string Name { get; private set; }
        }
    }
}
