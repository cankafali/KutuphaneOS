using KutuphaneMvc.Models.Entity;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace KutuphaneMvc.Controllers
{
    public class UyeController : Controller
    {
        private LibraryDBEntities1 db = new LibraryDBEntities1();

        public ActionResult Index()
        {
            var uye = db.UYE.ToList();
            return View(uye);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult YeniUye()
        {
            ViewBag.Yetkiler = db.ROLE
                .Select(r => new SelectListItem
                {
                    Value = r.ROLE_ID.ToString(),
                    Text = r.ROLE_AD
                }).ToList();

            return View(new UYE { UYELIK_TARIHI = DateTime.Now, CEZA_PUAN = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult YeniUye(UYE m)
        {
            // ModelState hatalıysa sayfaya döneceğiz -> listeyi TEKRAR doldur!
            ViewBag.Yetkiler = db.ROLE
                .Select(r => new SelectListItem
                {
                    Value = r.ROLE_ID.ToString(),
                    Text = r.ROLE_AD
                }).ToList();

            if (!ModelState.IsValid)
                return View(m);

            if (!m.UYELIK_TARIHI.HasValue) m.UYELIK_TARIHI = DateTime.Now;
            if (!m.CEZA_PUAN.HasValue) m.CEZA_PUAN = 0;

            if (!string.IsNullOrWhiteSpace(m.PAROLA_HASH))
                m.PAROLA_HASH = KutuphaneMvc.Utils.PasswordHasher.Hash(m.PAROLA_HASH);

            db.UYE.Add(m);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Sil(int id)
        {
            var item = db.UYE.Find(id);
            if (item != null)
            {
                db.UYE.Remove(item);

                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Aski()
        {
            var askidakiUyeler = db.UYE.Where(u => u.CEZA_PUAN >= 50).ToList();
            return View(askidakiUyeler);
        }

        private bool UyeAskidaMi(UYE uye)
        {
            return uye.CEZA_PUAN >= 50;
        }

        private bool UyeBanliMi(UYE uye)
        {
            return uye.CEZA_PUAN >= 100;
        }

        // Üye işlemlerinde kullanmak için (örneğin ödünç alma, rezervasyon vs.)
        private bool UyeIslemYapabilirMi(UYE uye)
        {
            return uye.CEZA_PUAN < 50; // 50'den az ceza puanı olanlar işlem yapabilir
        }

        [HttpGet]
        public ActionResult SifreDegistir(int id)
        {
            var uye = db.UYE.Find(id);
            if (uye == null) return HttpNotFound();
            return View(new SifreDegistirModel { UYE_ID = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SifreDegistir(SifreDegistirModel m)
        {
            if (string.IsNullOrWhiteSpace(m.YeniSifre))
            {
                ModelState.AddModelError("YeniSifre", "Şifre boş olamaz.");
                return View(m);
            }
            var uye = db.UYE.Find(m.UYE_ID);
            if (uye == null) return HttpNotFound();

            uye.PAROLA_HASH = KutuphaneMvc.Utils.PasswordHasher.Hash(m.YeniSifre);
            db.SaveChanges();

            TempData["ok"] = "Şifre başarıyla değiştirildi.";
            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult Profil()
        {
            // Login sırasında SetAuthCookie'de ne yazıldıysa, Identity.Name onu taşır
            string email = User.Identity.Name;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }

            // Email ile kullanıcıyı bul
            var uye = db.UYE.FirstOrDefault(x => x.EMAIL == email);

            if (uye == null)
            {
                // kullanıcı bulunamazsa login sayfasına gönder
                FormsAuthentication.SignOut();
                return RedirectToAction("Login", "Account");
            }

            return View(uye);
        }

        [HttpGet]
        [Authorize]
        public ActionResult ProfilDuzenle()
        {
            string email = User.Identity.Name;
            var uye = db.UYE.FirstOrDefault(x => x.EMAIL == email);
            return View(uye);
        }

        [HttpPost]
        [Authorize]
        public ActionResult ProfilDuzenle(UYE model)
        {
            var uye = db.UYE.Find(model.UYE_ID);
            if (uye != null)
            {
                uye.AD = model.AD;
                uye.SOYAD = model.SOYAD;
                uye.EMAIL = model.EMAIL;
                uye.TELEFON = model.TELEFON;

                db.SaveChanges();
            }
            return RedirectToAction("Profil");
        }

        [HttpGet]
        public ActionResult Duzenle(int? id)
        {
            var uye = db.UYE.Find(id);
            if (uye == null) return HttpNotFound();

            return View(uye); // Edit.cshtml çalışır
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Duzenle(UYE model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // formda hata varsa geri dön
            }

            var uye = db.UYE.Find(model.UYE_ID);
            if (uye == null) return HttpNotFound();

            // Alanları güncelle
            uye.AD = model.AD;
            uye.SOYAD = model.SOYAD;
            uye.EMAIL = model.EMAIL;
            uye.TELEFON = model.TELEFON;
            uye.CEZA_PUAN = model.CEZA_PUAN;
            uye.ROLE_ID = model.ROLE_ID;

            db.SaveChanges();

            TempData["ok"] = "Üye başarıyla güncellendi.";
            return RedirectToAction("Index");
        }

        public class SifreDegistirModel
        {
            public int UYE_ID { get; set; }
            public string YeniSifre { get; set; }
        }
    }
}