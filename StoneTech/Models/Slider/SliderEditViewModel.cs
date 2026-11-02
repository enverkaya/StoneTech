using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoneTech.Models.Slider
{
    public class SliderEditViewModel
    {
        // Düzenlenecek slider kimlik numarası
        public int Id { get; set; }

        // Slider başlığı
        [Required(ErrorMessage = "Slider başlığı zorunludur.")]
        [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        // Slider açıklaması
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        // Slider için yüklenecek yeni görsel
        public IFormFile? ImageFile { get; set; }

        // Slider'ın mevcut görsel yolu
        public string? CurrentImageUrl { get; set; }

        // Slider görüntülenme sırası
        [Range(0, int.MaxValue, ErrorMessage = "Sıralama negatif olamaz.")]
        public int DisplayOrder { get; set; }

        // Slider aktif mi?
        public bool IsActive { get; set; }
    }
}