using KutuphaneMvc.Models.Entity;
using System;
using System.Linq;
using System.Web.Mvc;

namespace KutuphaneMvc.Controllers
{
    public class IstekController : Controller
    {
        private readonly LibraryDBEntities1 db = new LibraryDBEntities1();

        // ADMIN METODLARI
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            var istekler = db.View_ISTEK.ToList();
            return View(istekler);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult YeniIstek()
        {
            var uyeEmail = User.Identity.Name;
            var uye = db.UYE.FirstOrDefault(u => u.EMAIL == uyeEmail);
            if (uye == null) return RedirectToAction("Login", "Account");

            ViewBag.BosKitaplar = db.KITAP
                .Where(k => k.DURUM == true)
                .Select(k => new SelectListItem
                {
                    Value = k.KITAP_ID.ToString(),
                    Text = k.AD
                })
                .ToList();

            ViewBag.OduncteKitaplar = (from e in db.EMANET
                                       join k in db.KITAP on e.KITAP_ID equals k.KITAP_ID
                                       where e.TESLIM_EDILDI_MI == false
                                       select new SelectListItem
                                       {
                                           Value = e.KITAP_ID.ToString(),
                                           Text = k.AD + " (Ödünçte)"
                                       })
                                       .ToList();

            ViewBag.AktifEmanetler = (from e in db.EMANET
                                      join k in db.KITAP on e.KITAP_ID equals k.KITAP_ID
                                      where e.UYE_ID == uye.UYE_ID && e.TESLIM_EDILDI_MI == false
                                      select new SelectListItem
                                      {
                                          Value = e.KITAP_ID.ToString(),
                                          Text = k.AD
                                      })
                                      .ToList();

            return View(new ISTEK());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult YeniIstek(ISTEK model, string Bagis_KitapAdi, string Bagis_Yazar, int? Bagis_Yil)
        {
            var uyeEmail = User.Identity.Name;
            var uye = db.UYE.FirstOrDefault(u => u.EMAIL == uyeEmail);
            if (uye == null) return RedirectToAction("Login", "Account");

            model.UYE_ID = uye.UYE_ID;
            model.TARIH = DateTime.Now;
            model.DURUM = "Beklemede";

            if (model.TIP == "Bagis")
            {
                model.ACIKLAMA = $"Kitap: {Bagis_KitapAdi}, Yazar: {Bagis_Yazar}, Yıl: {Bagis_Yil}";
                model.KITAP_ID = null;
                db.ISTEK.Add(model);
                db.SaveChanges();
            }
            else if (model.TIP == "Emanet")
            {
                if (model.KITAP_ID.HasValue)
                {
                    db.ISTEK.Add(model);
                    db.SaveChanges();
                }
                return RedirectToAction("YeniIstek");
            }
            else if (model.TIP == "Rezervasyon")
            {
                if (int.TryParse(Request.Form["Rezervasyon_KITAP_ID"], out int kitapId))
                {
                    model.KITAP_ID = kitapId;
                    db.ISTEK.Add(model);
                    db.SaveChanges();
                    TempData["ok"] = "Rezervasyon isteği kaydedildi.";
                }
                else
                {
                    TempData["hata"] = "Kitap seçilmedi veya sistemsel bir hata oluştu.";
                }
                return RedirectToAction("Index");
            }
            else if (model.TIP == "TeslimEtme")
            {
                if (int.TryParse(Request.Form["Teslim_KITAP_ID"], out int kitapId))
                {
                    model.KITAP_ID = kitapId;
                    db.ISTEK.Add(model);
                    db.SaveChanges();
                    TempData["ok"] = "Teslim etme isteği kaydedildi.";
                }
                else
                {
                    TempData["hata"] = "Teslim edilecek kitap seçilmedi.";
                }
                return RedirectToAction("Index");
            }

            TempData["ok"] = "İsteğiniz alınmıştır. Admin onayı bekleniyor.";
            return RedirectToDashboard();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Onayla(int id)
        {
            var istek = db.ISTEK.Find(id);
            if (istek == null) return HttpNotFound();

            if (istek.TIP == "Bagis")
            {
                return RedirectToAction("KitapEkle", "Kitap", new { istekId = istek.ISTEK_ID });
            }

            if (istek.TIP == "TeslimEtme")
            {
                var emanet = db.EMANET.FirstOrDefault(e => e.KITAP_ID == istek.KITAP_ID && e.TESLIM_EDILDI_MI == false);
                if (emanet != null)
                {
                    db.EMANET.Remove(emanet);
                }
            }

            if (istek.TIP == "Emanet")
            {
                var emanet = new EMANET
                {
                    KITAP_ID = istek.KITAP_ID.Value,
                    UYE_ID = istek.UYE_ID,
                    ALINIS_TARIHI = DateTime.Now,
                    TESLIM_TARIHI = DateTime.Now.AddDays(15),
                    TESLIM_EDILDI_MI = false
                };

                db.EMANET.Add(emanet);

                // Kitap durumunu pasif yap
                var kitap = db.KITAP.Find(istek.KITAP_ID);
                if (kitap != null)
                    kitap.DURUM = false;
            }

            istek.DURUM = "Onaylandı";
            db.SaveChanges();

            TempData["ok"] = "İstek onaylandı.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Reddet(int id)
        {
            var istek = db.ISTEK.Find(id);

            if (istek != null)
            {
                istek.DURUM = "Reddedildi";
                db.SaveChanges();
                TempData["info"] = "İstek reddedildi.";
            }

            return RedirectToAction("Index");
        }

        // ÜYE METODLARI
        [Authorize(Roles = "Uye")]
        public ActionResult YeniIstekUye()
        {
            if (Session["UYE_ID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int uyeId = Convert.ToInt32(Session["UYE_ID"]);
            var uye = db.UYE.FirstOrDefault(x => x.UYE_ID == uyeId);

            if (uye == null || uye.CEZA_PUAN >= 50)
            {
                TempData["BanliUyari"] = "Ceza puanınız yüksek olduğu için istek gönderemezsiniz.";
                return RedirectToAction("Index", "Home");
            }

            // ❗ KONTROL: Zaten aktif emanet varsa, yeni emanet isteğine izin verme
            bool aktifEmanetiVar = db.EMANET.Any(e => e.UYE_ID == uyeId && e.TESLIM_EDILDI_MI == false);
            if (aktifEmanetiVar)
            {
                TempData["BanliUyari"] = "Zaten ödünç aldığınız bir kitap var. İade etmeden yeni istek gönderemezsiniz.";
                return RedirectToAction("Index", "Home");
            }

            // Ödünç alınabilir kitaplar → DURUM = true
            var emanetKitaplar = db.KITAP
                .Where(k => k.DURUM == true)
                .Select(k => new SelectListItem
                {
                    Value = k.KITAP_ID.ToString(),
                    Text = k.AD
                }).ToList();

            // Rezervasyon yapılabilecek kitaplar → şu anda ödünçte (teslim edilmemiş)
            var rezervasyonKitaplar = (from e in db.EMANET
                                       join k in db.KITAP on e.KITAP_ID equals k.KITAP_ID
                                       where e.TESLIM_EDILDI_MI == false
                                       select new SelectListItem
                                       {
                                           Value = e.KITAP_ID.ToString(),
                                           Text = k.AD + " (Ödünçte)"
                                       }).ToList();

            ViewBag.TeslimEtmeKitaplari = emanetKitaplar;
            ViewBag.RezervasyonKitaplari = rezervasyonKitaplar;

            return View(new ISTEK());
        }

        [Authorize(Roles = "Uye")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult YeniIstekUye(ISTEK model)
        {
            if (Session["UYE_ID"] == null)
                return RedirectToAction("Login", "Account");

            int uyeId = Convert.ToInt32(Session["UYE_ID"]);
            var uye = db.UYE.FirstOrDefault(x => x.UYE_ID == uyeId);

            if (uye == null || uye.CEZA_PUAN >= 50)
            {
                TempData["BanliUyari"] = "Ceza puanınız yüksek olduğu için istek gönderemezsiniz.";
                return RedirectToAction("Index", "Home");
            }

            model.UYE_ID = uye.UYE_ID;
            model.TARIH = DateTime.Now;
            model.DURUM = "Beklemede";

            db.ISTEK.Add(model);
            db.SaveChanges();

            TempData["ok"] = "İsteğiniz kaydedildi. Onay bekleniyor.";
            return RedirectToAction("Index", "Home");
        }

        // YARDIMCI METODLAR
        private ActionResult RedirectToDashboard()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}