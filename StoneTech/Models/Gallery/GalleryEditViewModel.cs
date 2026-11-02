
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoneTech.Models.Gallery
{
    public class GalleryEditViewModel
    {
        // Düzenlenecek galeri görselinin kimlik numarası
        public int Id { get; set; }

        // Galeri görselinin başlığı
        [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
        public string? Title { get; set; }

        // Yeni yüklenecek görsel
        public IFormFile? ImageFile { get; set; }

        // Mevcut görselin dosya yolu
        public string? CurrentImageUrl { get; set; }

        // Görselin görüntülenme sırası
        [Range(0, int.MaxValue, ErrorMessage = "Sıralama negatif olamaz.")]
        public int DisplayOrder { get; set; }

        // Görsel web sitesinde gösterilsin mi?
        public bool IsActive { get; set; }
    }
}
