namespace StoneTech.Entities
{
    public class ServiceImage
    {
        // Görsel kimlik numarası
        public int Id { get; set; }

        // Görselin ait olduğu hizmet
        public int ServiceId { get; set; }

        // Görselin dosya yolu
        public string ImageUrl { get; set; } = string.Empty;

        // Görüntülenme sırası
        public int DisplayOrder { get; set; }

        // Hizmet ilişkisi
        public Service Service { get; set; } = null!;
    }
}