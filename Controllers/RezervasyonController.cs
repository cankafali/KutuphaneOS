using KutuphaneMvc.Models.Entity;
using KutuphaneMvc.Models.Enums;
using System;
using System.Linq;
using System.Web.Mvc;

namespace KutuphaneMvc.Controllers
{
    public class RezervasyonController : Controller
    {
        private LibraryDBEntities1 db = new LibraryDBEntities1();

        // GET: rezervasyon
        [Authorize(Roles ="Admin")]
        public ActionResult Index()
        {
            string email = User.Identity.Name; // giriş yapan kullanıcının emaili
            var uye = db.UYE.FirstOrDefault(x => x.EMAIL == email);

            IQueryable<VW_Rezervasyon> rezervasyonlar;

            if (uye.ROLE_ID == (int)Roller.Admin)
            {
                // Admin -> bütün rezervasyonları görür
                rezervasyonlar = db.VW_Rezervasyon
                      .Where(r => r.DURUM == (int)RezervasyonDurum.Beklemede);
            }
            else
            {
                // Kullanıcı -> sadece kendi rezervasyonlarını görür
                rezervasyonlar = db.VW_Rezervasyon
                                   .Where(r => r.DURUM == (int)RezervasyonDurum.Beklemede)
                                   .Where(r => r.EMAIL == email);
            }

            return View(rezervasyonlar.ToList());
        }

        // --- Yeni Rezervasyon ---
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult RezervasyonYap(int kitapId)
        {
            string email = User.Identity.Name;
            var uye = db.UYE.FirstOrDefault(x => x.EMAIL == email);

            if (uye == null)
            {
                return new HttpUnauthorizedResult();
            }

            // oduncte mi?
            bool kitapoduncte = db.EMANET.Any(e => e.KITAP_ID == kitapId && e.TESLIM_EDILDI_MI == false);

            bool aynikitap = db.REZERVASYON.Any(e => e.UYE_ID == uye.UYE_ID
                                                 && e.KITAP_ID == kitapId
                                                 && e.DURUM == ((int)RezervasyonDurum.Beklemede));
            if (aynikitap)
            {
                TempData["Error"] = "Bu kitap için zaten aktif rezervasyon bulunuyor.";
                return RedirectToAction("Index");
            }

            int toplamRez = db.REZERVASYON.Count(r => r.KITAP_ID == kitapId
                                       && r.DURUM == (int)RezervasyonDurum.Beklemede);

            if (toplamRez >= 3)
            {
                TempData["Error"] = "Bu kitap için maksimum 3 rezervasyon yapılabilir.";
                return RedirectToAction("Index");
            }

            if (!kitapoduncte)
            {
                TempData["Error"] = "Bu kitap şu an müsait; rezervasyona gerek yok.";
                return RedirectToAction("Index");
            }

            var rezervasyon = new REZERVASYON
            {
                KITAP_ID = kitapId,
                UYE_ID = uye.UYE_ID,
                TALEP_TARIH = DateTime.Now,
                SON_GECERLILIK = DateTime.Now.AddDays(3),
                DURUM = (int)RezervasyonDurum.Beklemede
            };
            db.REZERVASYON.Add(rezervasyon);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Onay(int id)
        {
            var rez = db.REZERVASYON.Find(id);

            if (rez != null)
            {
                // Kitap şu an birinde emanette mi?
                bool kitapZatenOduncte = db.EMANET.Any(e => e.KITAP_ID == rez.KITAP_ID && e.TESLIM_EDILDI_MI == false);

                if (kitapZatenOduncte)
                {
                    // sadece rezervasyon durumunu güncelle
                    rez.DURUM = (int)RezervasyonDurum.Beklemede;
                    TempData["Error"] = "Kitap halen emanette. Teslim alındığında bu rezervasyon devreye girecek.";
                }
                else
                {
                    // kitap boş → rezervasyonu onayla ve emanet oluştur
                    rez.DURUM = (int)RezervasyonDurum.Onaylandi;

                    var kitap = db.KITAP.FirstOrDefault(k => k.KITAP_ID == rez.KITAP_ID);
                    if (kitap != null) kitap.DURUM = false;

                    var emanet = new EMANET
                    {
                        KITAP_ID = rez.KITAP_ID,
                        UYE_ID = rez.UYE_ID,
                        ALINIS_TARIHI = DateTime.Now,
                        TESLIM_TARIHI = DateTime.Now.AddDays(15),
                        TESLIM_EDILDI_MI = false
                    };

                    db.EMANET.Add(emanet);
                }

                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult YeniRezervasyon()
        {
            var oduncKitap = (from em in db.EMANET
                              join k in db.KITAP on em.KITAP_ID equals k.KITAP_ID
                              where em.TESLIM_EDILDI_MI == false
                              select k)
                     .Distinct()
                     .ToList();
            return View(oduncKitap);
        }
    }
}