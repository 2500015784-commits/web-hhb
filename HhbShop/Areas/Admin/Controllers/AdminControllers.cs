using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Security;
using HhbShop.Controllers;
using HhbShop.Models;

namespace HhbShop.Areas.Admin.Controllers
{
    public sealed class AccountController : ShopController
    {
        public ActionResult Login(string returnUrl)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Dashboard");
            return View(new LoginInput { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginInput input)
        {
            if (!ModelState.IsValid) return View(input);
            const string attemptsKey = "HhbLoginAttempts";
            const string attemptTimeKey = "HhbLoginWindow";
            var now = DateTime.UtcNow;
            if (!(Session[attemptTimeKey] is DateTime started) || (now - started).TotalMinutes >= 5)
            {
                Session[attemptTimeKey] = now;
                Session[attemptsKey] = 0;
            }
            var attempts = (int)Session[attemptsKey];
            if (attempts >= 10)
            {
                ModelState.AddModelError("", "Bạn đã thử quá nhiều lần. Vui lòng thử lại sau vài phút.");
                return View(input);
            }

            var username = input.UserName.Trim();
            var account = Db.AdminUsers.AsNoTracking().SingleOrDefault(u => u.UserName == username);
            if (!ShopAuthentication.Check(input.Password, account?.Salt, account?.PasswordHash))
            {
                Session[attemptsKey] = attempts + 1;
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu chưa đúng.");
                return View(input);
            }

            Session.Remove(attemptsKey);
            Session.Remove(attemptTimeKey);
            ShopAuthentication.SignIn(HttpContext, account.UserName, "Admin");
            if (Url.IsLocalUrl(input.ReturnUrl)) return Redirect(input.ReturnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }

    [Authorize(Roles = "Admin")]
    public sealed class DashboardController : ShopController
    {
        public ActionResult Index() => View(new DashboardPage
        {
            Products = Db.Products.Count(x => x.IsActive),
            Orders = Db.Orders.Count(),
            Pending = Db.Orders.Count(x => x.Status == "Pending"),
            Revenue = Db.Orders.Where(x => x.Status == "Completed").Sum(x => (decimal?)x.Total) ?? 0,
            Recent = Db.Orders.AsNoTracking().OrderByDescending(x => x.Id).Take(6).ToList()
        });
    }

    [Authorize(Roles = "Admin")]
    public sealed class CategoriesController : ShopController
    {
        public ActionResult Index() => View(Db.Categories.AsNoTracking().OrderBy(c => c.Id).ToList());

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(int id, string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 80 || Db.Categories.Any(c => c.Id != id && c.Name == name))
            {
                TempData["Error"] = "Tên danh mục phải từ 1 đến 80 ký tự và chưa được sử dụng.";
                return RedirectToAction("Index");
            }
            var category = id == 0 ? Db.Categories.Add(new Category()) : Db.Categories.Find(id);
            if (category == null) return HttpNotFound();
            category.Name = name;
            try
            {
                Db.SaveChanges();
                TempData["Success"] = "Đã lưu danh mục.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Danh mục không lưu được. Hãy tải lại và kiểm tra tên.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var category = Db.Categories.Find(id);
            if (category == null) return HttpNotFound();
            if (Db.Products.Any(p => p.CategoryId == id))
                TempData["Error"] = "Hãy chuyển các sản phẩm sang danh mục khác trước khi xóa.";
            else
            {
                try { Db.Categories.Remove(category); Db.SaveChanges(); TempData["Success"] = "Đã xóa danh mục."; }
                catch (DbUpdateException) { TempData["Error"] = "Danh mục vừa thay đổi. Hãy tải lại danh sách."; }
            }
            return RedirectToAction("Index");
        }
    }

    [Authorize(Roles = "Admin")]
    public sealed class OrdersController : ShopController
    {
        public ActionResult Index(string status)
        {
            var orders = Db.Orders.AsNoTracking().AsQueryable();
            if (ShopText.Statuses.ContainsKey(status ?? "")) orders = orders.Where(o => o.Status == status);
            ViewBag.Filter = status;
            return View(orders.OrderByDescending(o => o.Id).ToList());
        }

        public ActionResult Details(int id)
        {
            var order = Db.Orders.AsNoTracking().Include(o => o.Lines).SingleOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();
            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Status(int id, string status, string version)
        {
            using (var transaction = Db.Database.BeginTransaction())
            {
                var order = Db.Orders.Include(o => o.Lines.Select(l => l.Product)).SingleOrDefault(o => o.Id == id);
                if (order == null) return HttpNotFound();
                if (Convert.ToBase64String(order.Version) != version || !ShopText.NextStatuses(order.Status).Contains(status))
                {
                    TempData["Error"] = "Đơn đã thay đổi hoặc trạng thái mới không hợp lệ.";
                    return RedirectToAction("Details", new { id });
                }
                if (status == "Cancelled")
                    foreach (var line in order.Lines) line.Product.Stock += line.Quantity;
                order.Status = status;
                try
                {
                    Db.SaveChanges();
                    transaction.Commit();
                    TempData["Success"] = status == "Cancelled" ? "Đã hủy đơn và hoàn tồn kho." : "Đã cập nhật trạng thái.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Dữ liệu vừa thay đổi. Vui lòng kiểm tra lại đơn.";
                }
            }
            return RedirectToAction("Details", new { id });
        }
    }
}
