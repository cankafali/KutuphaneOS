using KutuphaneMvc.Models.Entity;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace KutuphaneMvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryDBEntities1 db = new LibraryDBEntities1();

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password, string returnUrl)
        {
            var user = db.UYE.FirstOrDefault(u => u.EMAIL == email && u.PAROLA_HASH == password);

            if (user != null)
            {
                string roleName = db.ROLE
                    .Where(r => r.ROLE_ID == user.ROLE_ID)
                    .Select(r => r.ROLE_AD)
                    .FirstOrDefault() ?? "uye";

                // Kullanıcıya rolünü içeren auth ticket oluştur
                var authTicket = new FormsAuthenticationTicket(
                    1,
                    user.EMAIL,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(60),
                    false,
                    roleName // Buraya rol adı geliyor (admin/uye)
                );

                string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                Response.Cookies.Add(authCookie);

                // Session bilgilerini yaz
                Session["UYE_ID"] = user.UYE_ID;
                Session["AdSoyad"] = (user.AD ?? "") + " " + (user.SOYAD ?? "");
                Session["Role"] = user.ROLE_ID;

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Geçersiz e-posta veya şifre.");
            return View();
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }
    }
}