using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace KutuphaneMvc.Models.Entity
{
    public class BookController : Controller
    {
        private LibraryDBEntities1 db = new LibraryDBEntities1();

        public ActionResult Index()
        {
            List<KITAP> books = db.KITAP.ToList();

            return View(books);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult BookForm(int? id)
        {
            KITAP book = null;
            if (id == null)
            {
                book = new KITAP();
                book.DURUM = true;
            }
            else
            {
                book = db.KITAP.FirstOrDefault(x => x.KITAP_ID == id);
            }
            var uygunRaflar = db.RAF
                .Where(r => db.KITAP.Count(k => k.RAF_ID == r.RAF_ID) < r.KAPASITE)
                 .Select(r => new SelectListItem
                 {
                     Text = r.RAF_AD,
                     Value = r.RAF_ID.ToString()
                 }).ToList();

            ViewBag.Raflar = uygunRaflar;
            ViewBag.Raflar = db.RAF.Select(r => new SelectListItem
            {
                Value = r.RAF_ID.ToString(),
                Text = r.RAF_AD
            }).ToList();

            ViewBag.Kategoriler = db.KATEGORI.Select(k => new SelectListItem
            {
                Value = k.KATEGORI_ID.ToString(),
                Text = k.KATEGORI_ADI
            }).ToList();

            ViewBag.Durumlar = new List<SelectListItem>
            {
                    new SelectListItem { Value = "true", Text = "Müsait" },
                    new SelectListItem { Value = "false", Text = "Ödünçte" }
            };

            return View(book);
        }

        public ActionResult SaveBook(KITAP book)
        {
            if (book != null)
            {
                if (book.KITAP_ID == 0)
                {
                    db.KITAP.Add(book);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    var dbBook = db.KITAP.FirstOrDefault(x => x.KITAP_ID == book.KITAP_ID);
                    if (dbBook != null)
                    {
                        dbBook.AD = book.AD;
                        dbBook.YAZAR = book.YAZAR;
                        db.SaveChanges();

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return HttpNotFound();
                    }
                }
            }
            return View();
        }

        public ActionResult DeleteBook(int id)
        {
            var b = db.KITAP.Find(id);
            if (b == null)
                return HttpNotFound();
            db.KITAP.Remove(b);
            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}