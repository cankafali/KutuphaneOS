using System.Security.Principal;
using System.Threading;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace KutuphaneMvc
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_AuthenticateRequest(object sender, System.EventArgs e)
        {
            var c = System.Web.HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (c == null) return;
            var t = FormsAuthentication.Decrypt(c.Value);
            if (t == null || t.Expired) return;

            var roles = (t.UserData ?? "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            var id = new FormsIdentity(t);
            var p = new GenericPrincipal(id, roles);
            System.Web.HttpContext.Current.User = p;
            Thread.CurrentPrincipal = p;
        }
    }
}