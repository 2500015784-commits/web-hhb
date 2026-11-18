using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HhbShop.Controllers;
using HhbShop.Models;

namespace HhbShop.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public sealed class ProductsController : ShopController
    {
        public ActionResult Index(string q)
        {
            var products = Db.Products.AsNoTracking().Include(p => p.Category);
            q = (q ?? "").Trim();
            if (q.Length > 100) q = q.Substring(0, 100);
            if (q.Length > 0) products = products.Where(p => p.Name.Contains(q));
            ViewBag.Query = q;
            return View(products.OrderByDescending(p => p.Id).ToList());
        }

        public ActionResult Create() => Editor(new ProductInput { IsActive = true, Stock = 10 });

        public ActionResult Edit(int id)
        {
            var product = Db.Products.AsNoTracking().SingleOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();
            return Editor(new ProductInput
            {
                Id = product.Id, Name = product.Name, Price = product.Price, CategoryId = product.CategoryId,
                Stock = product.Stock, ImagePath = product.ImagePath, Description = product.Description,
                IsActive = product.IsActive, Featured = product.Featured, Version = Convert.ToBase64String(product.Version)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(ProductInput input, HttpPostedFileBase imageFile)
        {
            var existing = input.Id > 0 ? Db.Products.Find(input.Id) : null;
            if (input.Id != 0 && existing == null) return HttpNotFound();
            input.ImagePath = existing?.ImagePath;
            ModelState.Remove("ImagePath");
            if (existing != null && Convert.ToBase64String(existing.Version) != input.Version)
                ModelState.AddModelError("", "Sản phẩm vừa thay đổi. Hãy tải lại trước khi sửa.");
            if (!Db.Categories.Any(c => c.Id == input.CategoryId))
                ModelState.AddModelError(nameof(input.CategoryId), "Danh mục không tồn tại.");
            if (decimal.Truncate(input.Price) != input.Price)
                ModelState.AddModelError(nameof(input.Price), "Nhập giá theo số đồng nguyên.");
            if (existing == null && (imageFile == null || imageFile.ContentLength == 0))
                ModelState.AddModelError("imageFile", "Chọn ảnh thật của sản phẩm.");
            if (!ModelState.IsValid) return Editor(input);
            string newImage = null;
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                try { newImage = SaveImage(imageFile); }
                catch (ArgumentException ex) { ModelState.AddModelError("imageFile", ex.Message); return Editor(input); }
                catch (IOException) { ModelState.AddModelError("imageFile", "Không thể lưu ảnh. Vui lòng thử lại."); return Editor(input); }
            }
            var product = existing ?? Db.Products.Add(new Product());
            var previousImage = product.Image;
            product.Image = newImage ?? previousImage;
            product.Name = input.Name.Trim();
            product.Description = input.Description.Trim();
            product.Price = input.Price;
            product.CategoryId = input.CategoryId;
            product.Stock = input.Stock;
            product.IsActive = input.IsActive;
            product.Featured = input.Featured;
            try { Db.SaveChanges(); }
            catch (DbUpdateException)
            {
                RemoveUpload(newImage);
                ModelState.AddModelError("", "Không thể lưu sản phẩm. Hãy kiểm tra dữ liệu và thử lại.");
                return Editor(input);
            }
            if (newImage != null) RemoveUpload(previousImage);
            TempData["Success"] = "Đã lưu sản phẩm.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var product = Db.Products.Find(id);
            if (product == null) return HttpNotFound();
            bool hasOrders = Db.OrderDetails.Any(l => l.ProductId == id);
            if (hasOrders) product.IsActive = false;
            else Db.Products.Remove(product);
            try
            {
                Db.SaveChanges();
                if (!hasOrders) RemoveUpload(product.Image);
                TempData["Success"] = hasOrders ? "Đã ẩn sản phẩm và giữ lịch sử đơn." : "Đã xóa sản phẩm.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Sản phẩm vừa thay đổi. Hãy thử lại.";
            }
            return RedirectToAction("Index");
        }

        private ActionResult Editor(ProductInput input)
        {
            ViewBag.Categories = new SelectList(Db.Categories.AsNoTracking().OrderBy(c => c.Name).ToList(), "Id", "Name", input.CategoryId);
            return View("Editor", input);
        }

        private string SaveImage(HttpPostedFileBase file)
        {
            const string invalid = "Chọn ảnh JPG hoặc PNG hợp lệ, tối đa 5 MB và 16 triệu điểm ảnh.";
            if (file.ContentLength < 8 || file.ContentLength > 5 * 1024 * 1024) throw new ArgumentException(invalid);
            Image source;
            try { source = Image.FromStream(file.InputStream, true, true); }
            catch (ArgumentException) { throw new ArgumentException(invalid); }
            catch (OutOfMemoryException) { throw new ArgumentException(invalid); }
            using (source)
            {
                if ((source.RawFormat.Guid != ImageFormat.Jpeg.Guid && source.RawFormat.Guid != ImageFormat.Png.Guid) ||
                    source.Width < 1 || source.Height < 1 || (long)source.Width * source.Height > 16000000)
                    throw new ArgumentException(invalid);
                var folder = Server.MapPath("~/Uploads/");
                Directory.CreateDirectory(folder);
                var name = "upload-" + Guid.NewGuid().ToString("N") + ".jpg";
                var path = Path.Combine(folder, name);
                var scale = Math.Min(1.0, 1600.0 / Math.Max(source.Width, source.Height));
                using (var output = new Bitmap(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale))))
                using (var graphics = Graphics.FromImage(output))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(source, 0, 0, output.Width, output.Height);
                    try { output.Save(path, ImageFormat.Jpeg); }
                    catch (System.Runtime.InteropServices.ExternalException ex)
                    {
                        RemoveUpload(name);
                        throw new IOException("Image encoder could not write the upload.", ex);
                    }
                }
                return name;
            }
        }

        private void RemoveUpload(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith("upload-", StringComparison.Ordinal) || Path.GetFileName(name) != name) return;
            try { System.IO.File.Delete(Server.MapPath("~/Uploads/" + name)); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
