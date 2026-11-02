
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StoneTech.Models.About
{
    public class AboutEditViewModel
    {
        // Düzenlenecek hakkımızda kaydının kimlik numarası
        public int Id { get; set; }

        // Hakkımızda başlığı
        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        // Birinci açıklama metni
        public string? Description1 { get; set; }

        // İkinci açıklama metni
        public string? Description2 { get; set; }

        // Yeni birinci görsel
        public IFormFile? ImageFile1 { get; set; }

        // Mevcut birinci görselin dosya yolu
        public string? CurrentImageUrl1 { get; set; }

        // Yeni ikinci görsel
        public IFormFile? ImageFile2 { get; set; }

        // Mevcut ikinci görselin dosya yolu
        public string? CurrentImageUrl2 { get; set; }

        // Yeni üçüncü görsel
        public IFormFile? ImageFile3 { get; set; }

        // Mevcut üçüncü görselin dosya yolu
        public string? CurrentImageUrl3 { get; set; }
    }
}
