using KutuphaneMvc.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace KutuphaneMvc.Controllers
{
    public class EmanetController : Controller
    {
        private LibraryDBEntities1 db = new LibraryDBEntities1();


        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            List<View_EmanetKitap> emanetList = db.View_EmanetKitap.ToList();
            return View(emanetList);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Emanet(int? kitapId)
        {
            // DropDown için kitap listesi
            ViewBag.BookList = db.KITAP.Where(x => x.DURUM == true)
                .Select(k => new SelectListItem { Value = k.KITAP_ID.ToString(), Text = k.AD })
                .ToList();

            // DropDown için üye listesi
            ViewBag.UserList = db.UYE
                .Select(u => new SelectListItem
                {
                    Value = u.UYE_ID.ToString(),
                    Text = u.AD + " " + u.SOYAD
                }).ToList();

            var model = new EMANET
            {
                KITAP_ID = kitapId ?? 0,
                ALINIS_TARIHI = DateTime.Today,
                TESLIM_TARIHI = DateTime.Today.AddDays(15),
                TESLIM_EDILDI_MI = false
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EmanetKaydet(int? KITAP_ID, int UYE_ID, DateTime ALINIS_TARIHI, DateTime TESLIM_TARIHI)
        {
            var kitap = db.KITAP.FirstOrDefault(k => k.KITAP_ID == KITAP_ID);
            if (kitap == null)
                return HttpNotFound();

            var uye = db.UYE.FirstOrDefault(u => u.UYE_ID == UYE_ID);
            if (uye == null)
                return HttpNotFound();

            // 1. Ceza puanı kontrolü
            if (!UyeIslemYapabilirMi(uye)) // ceza puanı 50 veya üstüyse işlem yapamaz
            {
                ViewBag.BookList = db.KITAP.Where(x => x.DURUM == true)
                    .Select(k => new SelectListItem { Value = k.KITAP_ID.ToString(), Text = k.AD })
                    .ToList();

                ViewBag.UserList = db.UYE
                    .Select(u => new SelectListItem
                    {
                        Value = u.UYE_ID.ToString(),
                        Text = u.AD + " " + u.SOYAD
                    }).ToList();

                ModelState.AddModelError("", "Üyenin ceza puanı yüksek, ödünç alamaz!");
                return View("Emanet", new EMANET { KITAP_ID = KITAP_ID ?? 0, UYE_ID = UYE_ID });
            }

            // 2. Üyenin zaten aktif ödüncü var mı? (hangi kitap olursa olsun)
            bool aktifOduncVar = db.EMANET.Any(e => e.UYE_ID == UYE_ID && e.TESLIM_EDILDI_MI == false);
            if (aktifOduncVar)
            {
                ViewBag.BookList = db.KITAP.Where(x => x.DURUM == true)
                    .Select(k => new SelectListItem { Value = k.KITAP_ID.ToString(), Text = k.AD })
                    .ToList();

                ViewBag.UserList = db.UYE
                    .Select(u => new SelectListItem
                    {
                        Value = u.UYE_ID.ToString(),
                        Text = u.AD + " " + u.SOYAD
                    }).ToList();

                ModelState.AddModelError("", "Bu üyenin zaten aktif ödünç kitabı var. Başka kitap alamaz!");
                return View("Emanet", new EMANET { KITAP_ID = KITAP_ID ?? 0, UYE_ID = UYE_ID });
            }

            // 3. Kitap şu an başka üyede ödünçte mi?
            bool kitapOduncte = db.EMANET.Any(e => e.KITAP_ID == KITAP_ID && e.TESLIM_EDILDI_MI == false);
            if (kitapOduncte)
            {
                ViewBag.BookList = db.KITAP.Where(x => x.DURUM == true)
                    .Select(k => new SelectListItem { Value = k.KITAP_ID.ToString(), Text = k.AD })
                    .ToList();

                ViewBag.UserList = db.UYE
                    .Select(u => new SelectListItem
                    {
                        Value = u.UYE_ID.ToString(),
                        Text = u.AD + " " + u.SOYAD
                    }).ToList();

                ModelState.AddModelError("", "Bu kitap şu an ödünçte, verilemez!");
                return View("Emanet", new EMANET { KITAP_ID = KITAP_ID ?? 0, UYE_ID = UYE_ID });
            }

            // 4. Aynı üye aynı kitabı teslim etmeden tekrar almak istiyor mu?
            bool ayniKitapUye = db.EMANET.Any(e => e.KITAP_ID == KITAP_ID && e.UYE_ID == UYE_ID && e.TESLIM_EDILDI_MI == false);
            if (ayniKitapUye)
            {
                ViewBag.BookList = db.KITAP.Where(x => x.DURUM == true)
                    .Select(k => new SelectListItem { Value = k.KITAP_ID.ToString(), Text = k.AD })
                    .ToList();

                ViewBag.UserList = db.UYE
                    .Select(u => new SelectListItem
                    {
                        Value = u.UYE_ID.ToString(),
                        Text = u.AD + " " + u.SOYAD
                    }).ToList();

                ModelState.AddModelError("", "Üye aynı kitabı teslim etmeden tekrar alamaz!");
                return View("Emanet", new EMANET { KITAP_ID = KITAP_ID ?? 0, UYE_ID = UYE_ID });
            }

            // ✅ 5. Yeni emanet kaydı oluştur
            var yeniEmanet = new EMANET
            {
                KITAP_ID = KITAP_ID,
                UYE_ID = UYE_ID,
                ALINIS_TARIHI = ALINIS_TARIHI,
                TESLIM_TARIHI = TESLIM_TARIHI,
                TESLIM_EDILDI_MI = false
            };

            db.EMANET.Add(yeniEmanet);

            // 6. Kitap durumunu ödünçte yap
            kitap.DURUM = false;

            db.SaveChanges();

            TempData["ok"] = "Emanet başarıyla kaydedildi.";
            return RedirectToAction("Index");
        }

        public ActionResult EmanetSil(int id)
        {
            var b = db.EMANET.Find(id);
            if (b == null)
                return HttpNotFound();

            db.EMANET.Remove(b);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Gecikenler()
        {
            var gecikenler = db.EMANET
                            .Where(e => e.TESLIM_EDILDI_MI == false &&
                                    e.TESLIM_TARIHI < DateTime.Now).ToList();
            return View(gecikenler);
        }

        private bool UyeIslemYapabilirMi(UYE uye)
        {
            return (uye.CEZA_PUAN ?? 0) < 50;
        }
    }
}