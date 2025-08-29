using KutuphaneMvc.Models.Entity;
using System;
using System.Data.Entity;                 // Edit için
using System.Linq;
using System.Web.Mvc;

namespace KutuphaneMvc.Controllers
{

    [Authorize(Roles = "Admin")]
    public class KategoriController : Controller
    {
        private readonly LibraryDBEntities1 db = new LibraryDBEntities1();

        [Authorize(Roles = "Admin")] // Sadece adminler görebilir
        public ActionResult Index()
        {
            var model = db.KATEGORI.ToList();
            return View(model);
        }

        // CREATE (GET) - Formu aç
        [HttpGet]
        public ActionResult Create()
        {
            return View(new KATEGORI());     // Boş model ver
        }

        [Authorize(Roles = "1")] // Sadece adminler ekleyebilir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KATEGORI m)
        {
            m.KATEGORI_ADI = (m.KATEGORI_ADI ?? "").Trim();

            if (string.IsNullOrWhiteSpace(m.KATEGORI_ADI))
                ModelState.AddModelError("KATEGORI_ADI", "Kategori adı zorunludur.");

            // Aynı isim var mı? (case-insensitive)
            bool varMi = db.KATEGORI.Any(x => x.KATEGORI_ADI.ToLower() == m.KATEGORI_ADI.ToLower());
            if (varMi)
                ModelState.AddModelError("KATEGORI_ADI", "Bu ad zaten kayıtlı.");

            if (!ModelState.IsValid) return View(m);

            db.KATEGORI.Add(m);
            db.SaveChanges();

            TempData["ok"] = "Kategori eklendi.";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var m = db.KATEGORI.Find(id);
            if (m == null) return HttpNotFound();
            return View(m);
        }

        public ActionResult Edit(KATEGORI m)
        {
            m.KATEGORI_ADI = (m.KATEGORI_ADI ?? "").Trim();

            if (string.IsNullOrWhiteSpace(m.KATEGORI_ADI))
                ModelState.AddModelError("KATEGORI_ADI", "Kategori adı zorunludur.");

            bool varMi = db.KATEGORI
                .Any(x => x.KATEGORI_ID != m.KATEGORI_ID &&
                          x.KATEGORI_ADI.ToLower() == m.KATEGORI_ADI.ToLower());
            if (varMi)
                ModelState.AddModelError("KATEGORI_ADI", "Bu ad zaten kayıtlı.");

            if (!ModelState.IsValid) return View(m);

            db.Entry(m).State = EntityState.Modified;
            db.SaveChanges();

            TempData["ok"] = "Kategori güncellendi.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")] // Sadece adminler formu görebilir
        public ActionResult KategoriForm(int? id)
        {
            KATEGORI kat = (id == null)
                ? new KATEGORI()
                : db.KATEGORI.FirstOrDefault(x => x.KATEGORI_ID == id);

            if (id != null && kat == null) return HttpNotFound();
            return View(kat);
        }

        public ActionResult SaveKategori(KATEGORI m)
        {
            // Basit normalize + doğrulama
            m.KATEGORI_ADI = (m.KATEGORI_ADI ?? "").Trim();
            if (string.IsNullOrWhiteSpace(m.KATEGORI_ADI))
                ModelState.AddModelError("KATEGORI_ADI", "Kategori adı zorunludur.");

            bool isimVarMi = db.KATEGORI.Any(x =>
                x.KATEGORI_ID != m.KATEGORI_ID &&
                x.KATEGORI_ADI.ToLower() == m.KATEGORI_ADI.ToLower());

            if (isimVarMi)
                ModelState.AddModelError("KATEGORI_ADI", "Bu ad zaten kayıtlı.");

            if (!ModelState.IsValid)
                return View("KategoriForm", m);

            if (m.KATEGORI_ID == 0) // EKLE
            {
                db.KATEGORI.Add(m);
            }
            else                    // GÜNCELLE
            {
                var dbKat = db.KATEGORI.Find(m.KATEGORI_ID);
                if (dbKat == null) return HttpNotFound();

                dbKat.KATEGORI_ADI = m.KATEGORI_ADI;
                db.Entry(dbKat).State = EntityState.Modified;
            }

            db.SaveChanges();       // SaveChangesAsync değil (await yoksa)
            return RedirectToAction("Index");
        }

        // DELETE (POST) - Sil
        public ActionResult Delete(int id)
        {
            var kategori = db.KATEGORI.Find(id);
            if (kategori == null) return HttpNotFound();

            try
            {
                db.KATEGORI.Remove(kategori);
                db.SaveChanges();
                TempData["ok"] = "Kategori silindi.";
            }
            catch (Exception)
            {
                TempData["err"] = "Kategori silinemedi. (Bağlı kayıt olabilir)";
            }

            return RedirectToAction("Index");
        }
    }
}