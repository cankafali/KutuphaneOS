namespace KutuphaneMvc.Models.Entity
{
    public partial class KITAP
    {
        public string DurumText
        {
            get
            {
                if (DURUM.HasValue && DURUM.Value)
                    return "Müsait";
                else
                    return "Dolu";
            }
        }
    }
}