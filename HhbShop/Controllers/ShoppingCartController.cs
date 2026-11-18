using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using HhbShop.Models;

namespace HhbShop.Controllers
{
    public sealed class ShoppingCartController : ShopController
    {
        private Dictionary<int, int> Cart
        {
            get
            {
                if (Session["Cart"] is Dictionary<int, int> cart) return cart;
                cart = new Dictionary<int, int>();
                Session["Cart"] = cart;
                return cart;
            }
        }

        public ActionResult Index() => View(BuildPage());

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Add(int id, int quantity = 1, string returnUrl = null)
        {
            var product = Db.Products.AsNoTracking().SingleOrDefault(p => p.Id == id && p.IsActive);
            if (product == null) return HttpNotFound();
            Cart.TryGetValue(id, out int current);
            if (!ModelState.IsValid || quantity < 1 || quantity > 99 || current + quantity > Math.Min(99, product.Stock))
                TempData["Error"] = "Số lượng vượt quá mức có thể mua hoặc sản phẩm đã hết hàng.";
            else
            {
                Cart[id] = current + quantity;
                TempData["Success"] = "Đã thêm “" + product.Name + "” vào giỏ hàng.";
            }
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Update(int id, int quantity)
        {
            if (!Cart.ContainsKey(id)) return RedirectToAction("Index");
            var product = Db.Products.AsNoTracking().SingleOrDefault(p => p.Id == id && p.IsActive);
            if (!ModelState.IsValid || product == null || quantity < 1 || quantity > Math.Min(99, product.Stock))
                TempData["Error"] = "Số lượng không hợp lệ hoặc vượt quá tồn kho. Bạn có thể xóa sản phẩm khỏi giỏ.";
            else
            {
                Cart[id] = quantity;
                TempData["Success"] = "Đã cập nhật số lượng.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Remove(int id)
        {
            Cart.Remove(id);
            return RedirectToAction("Index");
        }

        public ActionResult Checkout()
        {
            var page = BuildPage();
            if (!page.CanCheckout)
            {
                TempData["Error"] = "Hãy kiểm tra lại sản phẩm trong giỏ trước khi đặt hàng.";
                return RedirectToAction("Index");
            }
            if (!(Session["CheckoutToken"] is Guid)) Session["CheckoutToken"] = Guid.NewGuid();
            return CheckoutView(new CheckoutInput { Token = (Guid)Session["CheckoutToken"] }, page);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutInput input)
        {
            if (Session["LastCheckoutToken"] is Guid last && last == input.Token)
                return RedirectToAction("Success");
            if (!(Session["CheckoutToken"] is Guid token) || token != input.Token)
            {
                TempData["Error"] = "Phiên đặt hàng đã hết hạn. Vui lòng mở lại trang đặt hàng.";
                return RedirectToAction("Checkout");
            }

            var page = BuildPage();
            if (!page.CanCheckout)
            {
                TempData["Error"] = "Tồn kho đã thay đổi. Hãy kiểm tra lại giỏ hàng.";
                return RedirectToAction("Index");
            }
            if (!ModelState.IsValid) return CheckoutView(input, page);

            using (var transaction = Db.Database.BeginTransaction())
            {
                var items = Cart.Keys.ToList();
                var available = Db.Products.Where(p => items.Contains(p.Id) && p.IsActive).ToDictionary(p => p.Id);
                if (Cart.Any(x => !available.ContainsKey(x.Key) || available[x.Key].Stock < x.Value))
                {
                    TempData["Error"] = "Tồn kho đã thay đổi. Hãy kiểm tra lại giỏ hàng.";
                    return RedirectToAction("Index");
                }
                var order = new Order
                {
                    CheckoutToken = token, CreatedUtc = DateTime.UtcNow, Status = "Pending",
                    CustomerName = input.CustomerName.Trim(), CustomerPhone = input.CustomerPhone.Trim(),
                    CustomerEmail = input.CustomerEmail?.Trim(), CustomerAddress = input.CustomerAddress.Trim(),
                    Note = input.Note?.Trim()
                };
                foreach (var selection in Cart)
                {
                    var product = available[selection.Key];
                    product.Stock -= selection.Value;
                    var line = new OrderDetail { ProductId = product.Id, ProductName = product.Name, Price = product.Price, Quantity = selection.Value };
                    order.Lines.Add(line);
                    order.Total += line.Price * line.Quantity;
                }
                Db.Orders.Add(order);
                try
                {
                    Db.SaveChanges();
                    transaction.Commit();
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Sản phẩm vừa được cập nhật. Hãy thử đặt hàng lại.";
                    return RedirectToAction("Index");
                }
                Session["LastOrderId"] = order.Id;
                Session["LastCheckoutToken"] = token;
                Session.Remove("CheckoutToken");
                Cart.Clear();
                return RedirectToAction("Success");
            }
        }

        public ActionResult Success()
        {
            if (!(Session["LastOrderId"] is int id)) return RedirectToAction("Index");
            return View(id);
        }

        private ViewResult CheckoutView(CheckoutInput input, CartPage page)
        {
            ViewBag.Cart = page;
            return View("Checkout", input);
        }

        private CartPage BuildPage()
        {
            var keys = Cart.Keys.ToList();
            var products = Db.Products.AsNoTracking().Where(p => keys.Contains(p.Id)).ToDictionary(p => p.Id);
            return new CartPage
            {
                Lines = Cart.Select(x => new CartLine
                {
                    Id = x.Key, Product = products.TryGetValue(x.Key, out var p) ? p : null, Quantity = x.Value
                }).ToList()
            };
        }
    }
}
