namespace StoneTech.Entities
{
    public class Service
    {
        // Hizmet kimlik numarası
        public int Id { get; set; }

        // Hizmet başlığı
        public string Title { get; set; } = string.Empty;

        // Hizmetin kısa açıklaması
        public string? ShortDescription { get; set; }

        // Hizmetin detaylı açıklaması
        public string? Description { get; set; }

        // Hizmetin kapak görseli
        public string? ImageUrl { get; set; }

        // Görüntülenme sırası
        public int DisplayOrder { get; set; }

        // Hizmet web sitesinde gösterilsin mi?
        public bool IsActive { get; set; } = true;

        // Hizmete ait görseller
        public ICollection<ServiceImage> ServiceImages { get; set; } = new List<ServiceImage>();
    }
}