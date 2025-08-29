namespace KutuphaneMvc.Models.Enums
{
    public enum RezervasyonDurum : byte
    {
        Beklemede = 0,
        Onaylandi = 1,
        Iptal = 2,
        SureBitti = 3,
        TeslimAlindi = 4
    }

    public enum Roller
    {
        Uye = 0,
        Admin = 1
    }
}