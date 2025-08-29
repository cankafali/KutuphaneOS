using KutuphaneMvc.Models.Entity;
using System;
using System.Linq;
using System.Web.Mvc;

namespace KutuphaneMvc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RafController : Controller
    {
        private LibraryDBEntities1 db = new LibraryDBEntities1();

        public ActionResult Index()
        {
            var data = db.View_RAF_KITAP.ToList(); // View'i kullanıyorsun zaten
            return View(data);
        }

        public ActionResult RafTransferIndex(int page = 1)
        {
            int pageSize = 10; // her sayfada kaç kitap gösterilsin
            int skipCount = (page - 1) * pageSize;

            // Toplam kitap sayısı
            int toplamKayit = db.View_RAF_KITAP.Count();

            // Sayfalama için kitap listesi
            var kitaplar = db.View_RAF_KITAP
                             .OrderBy(k => k.KITAP_ID)
                             .Skip(skipCount)
                             .Take(pageSize)
                             .ToList();

            ViewBag.Raflar = db.RAF.ToList();
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)toplamKayit / pageSize);

            return View(kitaplar);
        }

        [HttpPost]
        public ActionResult RafTransfer(int id, int yenirafid)
        {
            var kitap = db.KITAP.Find(id);
            if (kitap == null)
                return HttpNotFound();

            kitap.RAF_ID = yenirafid;
            db.SaveChanges();

            TempData["mesaj"] = "Kitap başarıyla yeni rafa taşındı.";
            return RedirectToAction("RafTransferIndex");
        }
    }
}