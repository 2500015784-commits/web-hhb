using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HhbShop.Models;

namespace HhbShop.Controllers
{
    public abstract class ShopController : Controller
    {
        protected readonly ShopDb Db = new ShopDb();
        protected int CustomerId => User.IsInRole("Customer") && int.TryParse(User.Identity.Name, out var id) ? id : 0;
        protected override void Dispose(bool disposing)
        {
            if (disposing) Db.Dispose();
            base.Dispose(disposing);
        }
    }

    public sealed class HomeController : ShopController
    {
        public ActionResult Index() => View(new HomePage
        {
            Categories = Db.Categories.AsNoTracking().OrderBy(c => c.Id).ToList(),
            Featured = Db.Products.AsNoTracking().Include(p => p.Category)
                .Where(p => p.IsActive).OrderByDescending(p => p.Featured).ThenBy(p => p.Id).Take(8).ToList()
        });

        public ActionResult About() => View();
    }

    public sealed class ProductController : ShopController
    {
        private const int PageSize = 8;

        public ActionResult Index(int? categoryId, string q, string sort = "featured", int page = 1)
        {
            var categories = Db.Categories.AsNoTracking().OrderBy(c => c.Id).ToList();
            if (categoryId.HasValue && categories.All(c => c.Id != categoryId)) return HttpNotFound();
            q = (q ?? "").Trim();
            if (q.Length > 100) q = q.Substring(0, 100);
            var products = Db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive);
            if (categoryId.HasValue) products = products.Where(p => p.CategoryId == categoryId);
            if (q.Length > 0) products = products.Where(p => p.Name.Contains(q));
            var total = products.Count();
            var pages = Math.Max(1, (total + PageSize - 1) / PageSize);
            page = Math.Max(1, Math.Min(page, pages));
            IOrderedQueryable<Product> sorted;
            switch (sort)
            {
                case "price-asc": sorted = products.OrderBy(p => p.Price); break;
                case "price-desc": sorted = products.OrderByDescending(p => p.Price); break;
                case "newest": sorted = products.OrderByDescending(p => p.Id); break;
                default: sort = "featured"; sorted = products.OrderByDescending(p => p.Featured); break;
            }
            return View(new CatalogPage
            {
                Categories = categories, CategoryId = categoryId, Query = q, Sort = sort,
                Products = sorted.ThenBy(p => p.Id).Skip((page - 1) * PageSize).Take(PageSize).ToList(),
                Page = page, Pages = pages, Total = total
            });
        }

        public ActionResult Details(int id = 0)
        {
            var product = Db.Products.AsNoTracking().Include(p => p.Category)
                .SingleOrDefault(p => p.Id == id && p.IsActive);
            if (product == null) return HttpNotFound();
            return View(new DetailsPage
            {
                Product = product,
                Related = Db.Products.AsNoTracking().Include(p => p.Category)
                    .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
                    .OrderByDescending(p => p.Featured).ThenBy(p => p.Id).Take(4).ToList()
            });
        }
    }
}
