namespace StoneTech.Entities
{
    public class About
    {
        // Hakkımızda kimlik numarası
        public int Id { get; set; }

        // Hakkımızda başlığı
        public string Title { get; set; } = string.Empty;

        // Birinci açıklama metni
        public string? Description1 { get; set; }

        // İkinci açıklama metni
        public string? Description2 { get; set; }

        // Birinci görsel
        public string? ImageUrl1 { get; set; }

        // İkinci görsel
        public string? ImageUrl2 { get; set; }

        // Üçüncü görsel
        public string? ImageUrl3 { get; set; }
    }
}