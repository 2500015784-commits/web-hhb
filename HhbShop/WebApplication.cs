using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
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
            GlobalFilters.Filters.Add(new DatabaseUnavailableFilter());
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
            var role = identity.Ticket.UserData;
            if (role != "Admin" && role != "Customer") return;
            var principal = new GenericPrincipal(identity, new[] { role });
            Context.User = principal;
            Thread.CurrentPrincipal = principal;
        }
    }

    internal sealed class DatabaseUnavailableFilter : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.ExceptionHandled) return;
            for (var error = context.Exception; error != null; error = error.InnerException)
            {
                if (!(error is SqlException sql)) continue;
                switch (sql.Number)
                {
                    case -2: case -1: case 2: case 26: case 53: case 64:
                    case 233: case 701: case 1205: case 10054: case 10060:
                        Trace.TraceError("HHB Studio database unavailable: {0}", context.Exception);
                        var response = context.HttpContext.Response;
                        response.StatusCode = 503;
                        response.TrySkipIisCustomErrors = true;
                        response.Cache.SetNoStore();
                        context.Result = new ViewResult { ViewName = "~/Views/Shared/DatabaseUnavailable.cshtml" };
                        context.ExceptionHandled = true;
                        return;
                }
            }
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
