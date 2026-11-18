using System;
using System.Data.Entity;
using System.Text;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using HhbShop.Models;

namespace HhbShop
{
    public class WebApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer<ShopDb>(null);
            ValueProviderFactories.Factories.Insert(0, new VndPriceValueProviderFactory());
            AreaRegistration.RegisterAllAreas();
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.MapRoute("Public", "{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                new[] { "HhbShop.Controllers" }).DataTokens["UseNamespaceFallback"] = false;
        }

        protected void Application_PostAuthenticateRequest()
        {
            var identity = Context.User?.Identity as FormsIdentity;
            if (identity == null) return;
            if (identity.Ticket.UserData != "Admin") return;
            var principal = new GenericPrincipal(identity, new[] { "Admin" });
            Context.User = principal;
            Thread.CurrentPrincipal = principal;
        }
    }

    public sealed class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Admin";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute("Admin", "Admin/{controller}/{action}/{id}",
                new { controller = "Dashboard", action = "Index", id = UrlParameter.Optional },
                new[] { "HhbShop.Areas.Admin.Controllers" }).DataTokens["UseNamespaceFallback"] = false;
        }
    }

    // HTML number inputs submit invariant decimal strings, even on a vi-VN page.
    internal sealed class VndPriceValueProviderFactory : ValueProviderFactory
    {
        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            var values = new System.Collections.Specialized.NameValueCollection();
            var price = controllerContext.HttpContext.Request.Form["Price"];
            if (price != null) values["Price"] = price;
            return new NameValueCollectionValueProvider(values, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
