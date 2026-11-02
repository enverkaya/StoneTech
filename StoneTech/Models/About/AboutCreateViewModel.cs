
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoneTech.Models.About
{
    public class AboutCreateViewModel
    {
        // Hakkımızda başlığı
        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        // Birinci açıklama metni
        public string? Description1 { get; set; }

        // İkinci açıklama metni
        public string? Description2 { get; set; }

        // Birinci görsel
        public IFormFile? ImageFile1 { get; set; }

        // İkinci görsel
        public IFormFile? ImageFile2 { get; set; }

        // Üçüncü görsel
        public IFormFile? ImageFile3 { get; set; }
    }
}
