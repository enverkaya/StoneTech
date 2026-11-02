namespace StoneTech.Entities
{
    public class Slider
    {
        // Slider kimlik numarası
        public int Id { get; set; }

        // Slider başlığı
        public string Title { get; set; } = string.Empty;

        // Slider açıklaması
        public string? Description { get; set; }

        // Slider görselinin yolu
        public string ImageUrl { get; set; } = string.Empty;

        // Görüntülenme sırası
        public int DisplayOrder { get; set; }

        // Slider aktif mi?
        public bool IsActive { get; set; } = true;
    }
}