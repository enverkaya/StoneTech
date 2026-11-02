
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoneTech.Models.Gallery
{
    public class GalleryCreateViewModel
    {
        // Galeri görselinin başlığı
        [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
        public string? Title { get; set; }

        // Galeriye yüklenecek görsel
        [Required(ErrorMessage = "Görsel seçmelisiniz.")]
        public IFormFile? ImageFile { get; set; }

        // Görselin görüntülenme sırası
        [Range(0, int.MaxValue, ErrorMessage = "Sıralama negatif olamaz.")]
        public int DisplayOrder { get; set; }

        // Görsel web sitesinde gösterilsin mi?
        public bool IsActive { get; set; } = true;
    }
}
