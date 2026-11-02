namespace StoneTech.Entities
{
    public class Gallery
    {
        // Galeri görselinin kimlik numarası
        public int Id { get; set; }

        // Görsel başlığı
        public string? Title { get; set; }

        // Görselin dosya yolu
        public string ImageUrl { get; set; } = string.Empty;

        // Görüntülenme sırası
        public int DisplayOrder { get; set; }

        // Görsel web sitesinde gösterilsin mi?
        public bool IsActive { get; set; } = true;
    }
}