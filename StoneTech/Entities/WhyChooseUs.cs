namespace StoneTech.Entities
{
    public class WhyChooseUs
    {
        // Özellik kimlik numarası
        public int Id { get; set; }

        // Özellik başlığı
        public string Title { get; set; } = string.Empty;

        // Özellik açıklaması
        public string? Description { get; set; }

        // Özelliğin ikon sınıfı
        public string? IconClass { get; set; }

        // Görüntülenme sırası
        public int DisplayOrder { get; set; }

        // Özellik aktif mi?
        public bool IsActive { get; set; } = true;
    }
}