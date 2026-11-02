
namespace StoneTech.Models.Service
{
    public class ServiceImageViewModel
    {
        // Görselin kimlik numarası
        public int Id { get; set; }

        // Görselin ait olduğu hizmet
        public int ServiceId { get; set; }

        // Görselin dosya yolu
        public string ImageUrl { get; set; } = string.Empty;

        // Görselin görüntülenme sırası
        public int DisplayOrder { get; set; }
    }
}
