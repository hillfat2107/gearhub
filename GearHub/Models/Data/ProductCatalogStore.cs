using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GearHub.Models.Entities;
using Newtonsoft.Json;

namespace GearHub.Models.Data
{
    // Đọc và ghi danh sách sản phẩm vào một file để dữ liệu vẫn còn sau khi tắt web.
    public static class ProductCatalogStore
    {
        private static readonly object FileLock = new object();

        public static List<AdminProduct> GetProducts(string filePath)
        {
            lock (FileLock)
            {
                if (!File.Exists(filePath))
                {
                    return new List<AdminProduct>();
                }

                var json = File.ReadAllText(filePath, Encoding.UTF8);
                return JsonConvert.DeserializeObject<List<AdminProduct>>(json)
                    ?? new List<AdminProduct>();
            }
        }

        public static void AddProduct(string filePath, AdminProduct product)
        {
            lock (FileLock)
            {
                var folder = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var products = File.Exists(filePath)
                    ? JsonConvert.DeserializeObject<List<AdminProduct>>(File.ReadAllText(filePath, Encoding.UTF8))
                    : new List<AdminProduct>();

                if (products == null)
                {
                    products = new List<AdminProduct>();
                }

                products.Add(product);
                var json = JsonConvert.SerializeObject(products, Formatting.Indented);
                File.WriteAllText(filePath, json, new UTF8Encoding(false));
            }
        }

        public static List<string> GetImagePaths(AdminProduct product)
        {
            var imagePaths = product.ImagePaths == null
                ? new List<string>()
                : product.ImagePaths.Where(path => !string.IsNullOrWhiteSpace(path)).ToList();

            if (!string.IsNullOrWhiteSpace(product.ImagePath)
                && !imagePaths.Any(path => string.Equals(path, product.ImagePath, System.StringComparison.OrdinalIgnoreCase)))
            {
                imagePaths.Insert(0, product.ImagePath);
            }

            return imagePaths;
        }

        // Dùng các dòng cấu hình mới; nếu là sản phẩm cũ thì tách từ nội dung kiểu "CPU: ...".
        public static List<ProductSpec> GetSpecificationRows(AdminProduct product)
        {
            var savedRows = product.SpecificationRows == null
                ? new List<ProductSpec>()
                : product.SpecificationRows
                    .Where(row => row != null && !string.IsNullOrWhiteSpace(row.Name) && !string.IsNullOrWhiteSpace(row.Value))
                    .Select(row => new ProductSpec { Name = row.Name.Trim(), Value = row.Value.Trim() })
                    .ToList();

            if (savedRows.Count > 0)
            {
                return savedRows;
            }

            return (product.Specifications ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    var separator = line.IndexOf(':');
                    return separator > 0
                        ? new ProductSpec { Name = line.Substring(0, separator).Trim(), Value = line.Substring(separator + 1).Trim() }
                        : new ProductSpec { Name = "Thông tin", Value = line.Trim() };
                })
                .Where(row => !string.IsNullOrWhiteSpace(row.Value))
                .ToList();
        }

        // Ghi đè thông tin của sản phẩm có cùng mã mà không làm mất các sản phẩm khác.
        public static bool UpdateProduct(string filePath, AdminProduct updatedProduct)
        {
            lock (FileLock)
            {
                var products = ReadProducts(filePath);
                var index = products.FindIndex(item => item.Id == updatedProduct.Id);
                if (index < 0)
                {
                    return false;
                }

                products[index] = updatedProduct;
                WriteProducts(filePath, products);
                return true;
            }
        }

        // Bỏ sản phẩm khỏi danh sách và trả lại bản ghi để controller xử lý ảnh đi kèm.
        public static AdminProduct DeleteProduct(string filePath, string productId)
        {
            lock (FileLock)
            {
                var products = ReadProducts(filePath);
                var product = products.FirstOrDefault(item => item.Id == productId);
                if (product == null)
                {
                    return null;
                }

                products.Remove(product);
                WriteProducts(filePath, products);
                return product;
            }
        }

        // Dùng chung cách đọc và ghi để các thao tác luôn làm việc trên toàn bộ danh sách.
        private static List<AdminProduct> ReadProducts(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<AdminProduct>();
            }

            return JsonConvert.DeserializeObject<List<AdminProduct>>(File.ReadAllText(filePath, Encoding.UTF8))
                ?? new List<AdminProduct>();
        }

        private static void WriteProducts(string filePath, List<AdminProduct> products)
        {
            var folder = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var json = JsonConvert.SerializeObject(products, Formatting.Indented);
            File.WriteAllText(filePath, json, new UTF8Encoding(false));
        }
    }
}
