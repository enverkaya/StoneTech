using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoneTech.Models.Service
{
    public class ServiceEditViewModel
    {
        // Düzenlenecek hizmetin kimlik numarası
        public int Id { get; set; }

        // Hizmet başlığı (sadece admin panelinde kullanılacak)
        [Required(ErrorMessage = "Hizmet başlığı zorunludur.")]
        [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        // Görselin altında gösterilecek hizmet açıklaması
        [Required(ErrorMessage = "Hizmet açıklaması zorunludur.")]
        public string Description { get; set; } = string.Empty;

        // Yeni hizmet görseli (isteğe bağlı)
        public IFormFile? ImageFile { get; set; }

        // Mevcut hizmet görseli
        public string? CurrentImageUrl { get; set; }

        // Hizmetin görüntülenme sırası
        [Range(0, int.MaxValue, ErrorMessage = "Sıralama negatif olamaz.")]
        public int DisplayOrder { get; set; }

        // Hizmet web sitesinde gösterilsin mi?
        public bool IsActive { get; set; }
    }
}