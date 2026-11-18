using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace HhbShop.Controllers
{
    [CustomerOnly, OutputCache(Duration = 0, NoStore = true, VaryByParam = "*")]
    public sealed class OrdersController : ShopController
    {
        public ActionResult Index()
        {
            var customerId = CustomerId;
            return View(Db.Orders.AsNoTracking().Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.Id).ToList());
        }

        public ActionResult Invoice(int id)
        {
            var customerId = CustomerId;
            var order = Db.Orders.AsNoTracking().Include(o => o.Lines)
                .SingleOrDefault(o => o.Id == id && o.CustomerId == customerId);
            return order == null ? (ActionResult)HttpNotFound() : View(order);
        }
    }
}
