using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using HhbShop.Models;

namespace HhbShop.Controllers
{
    internal static class ShopAuthentication
    {
        internal static string Hash(string password, string salt)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 210000, HashAlgorithmName.SHA256))
                return Convert.ToBase64String(kdf.GetBytes(32));
        }

        internal static string NewSalt()
        {
            var salt = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        internal static bool Check(string password, string salt, string hash)
        {
            var actual = Hash(password, salt ?? Convert.ToBase64String(new byte[16]));
            var expected = hash ?? Convert.ToBase64String(new byte[32]);
            int difference = 0;
            for (int i = 0; i < actual.Length; i++) difference |= actual[i] ^ expected[i];
            return hash != null && difference == 0;
        }

        internal static void SignIn(HttpContextBase context, string name, string role)
        {
            var issued = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(1, name, issued, issued.AddMinutes(30),
                false, role, FormsAuthentication.FormsCookiePath);
            context.Response.Cookies.Set(new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true, Secure = context.Request.IsSecureConnection, SameSite = SameSiteMode.Lax, Path = ticket.CookiePath
            });
            context.Session.Remove("CheckoutToken");
        }
    }

    internal sealed class CustomerOnlyAttribute : AuthorizeAttribute
    {
        public CustomerOnlyAttribute() => Roles = "Customer";

        protected override void HandleUnauthorizedRequest(AuthorizationContext context)
        {
            var url = new UrlHelper(context.RequestContext);
            context.Result = new RedirectResult(url.Action("Login", "CustomerAccount",
                new { area = "", returnUrl = context.HttpContext.Request.RawUrl }));
        }
    }

    [OutputCache(Duration = 0, NoStore = true, VaryByParam = "*")]
    public sealed class CustomerAccountController : ShopController
    {
        [HttpGet]
        public ActionResult Login(string returnUrl) => User.IsInRole("Customer")
            ? ContinueTo(returnUrl) : View(new CustomerLoginInput { ReturnUrl = returnUrl });

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(CustomerLoginInput input)
        {
            if (!ModelState.IsValid) return View(input);
            const string window = "CustomerLoginWindow";
            const string attemptCount = "CustomerLoginAttempts";
            var now = DateTime.UtcNow;
            if (!(Session[window] is DateTime started) || now - started >= TimeSpan.FromMinutes(5))
            {
                Session[window] = now;
                Session[attemptCount] = 0;
            }
            var attempts = (int)Session[attemptCount];
            if (attempts >= 10)
            {
                ModelState.AddModelError("", "Bạn đã thử quá nhiều lần. Vui lòng thử lại sau vài phút.");
                return View(input);
            }

            var email = input.Email.Trim().ToLowerInvariant();
            var customer = Db.Customers.AsNoTracking().SingleOrDefault(c => c.Email == email);
            if (!ShopAuthentication.Check(input.Password, customer?.Salt, customer?.PasswordHash))
            {
                Session[attemptCount] = attempts + 1;
                ModelState.AddModelError("", "Email hoặc mật khẩu chưa đúng.");
                return View(input);
            }
            Session.Remove(window);
            Session.Remove(attemptCount);
            return SignIn(customer, input.ReturnUrl);
        }

        [HttpGet]
        public ActionResult Register(string returnUrl) => User.IsInRole("Customer")
            ? ContinueTo(returnUrl) : View(new RegisterInput { ReturnUrl = returnUrl });

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterInput input)
        {
            if (!ModelState.IsValid) return View(input);
            var customer = new Customer { Name = input.Name.Trim(), Email = input.Email.Trim().ToLowerInvariant() };
            customer.Salt = ShopAuthentication.NewSalt();
            customer.PasswordHash = ShopAuthentication.Hash(input.Password, customer.Salt);
            Db.Customers.Add(customer);
            try { Db.SaveChanges(); }
            catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
                return View(input);
            }
            return SignIn(customer, input.ReturnUrl);
        }

        [CustomerOnly, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        private ActionResult SignIn(Customer customer, string returnUrl)
        {
            ShopAuthentication.SignIn(HttpContext, customer.Id.ToString(CultureInfo.InvariantCulture), "Customer");
            return ContinueTo(returnUrl);
        }

        private ActionResult ContinueTo(string returnUrl) => Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl) : (ActionResult)RedirectToAction("Index", "Orders");
    }
}
