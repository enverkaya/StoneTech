
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Stone
{
    public class StoneCreateViewModel
    {
        // Taş adı
        [Required(ErrorMessage = "Taş adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Taş adı en fazla 100 karakter olabilir.")]
        public string StoneName { get; set; } = string.Empty;

        // Taş rengi
        [StringLength(50, ErrorMessage = "Renk en fazla 50 karakter olabilir.")]
        public string? Color { get; set; }

        // Taşın alış fiyatı
        [Range(0, double.MaxValue, ErrorMessage = "Alış fiyatı negatif olamaz.")]
        public decimal PurchasePrice { get; set; }

        // Taşın satış fiyatı
        [Range(0, double.MaxValue, ErrorMessage = "Satış fiyatı negatif olamaz.")]
        public decimal SalePrice { get; set; }

        // Kullanıcının yükleyeceği taş görseli
        public IFormFile? ImageFile { get; set; }

        // Taşın web sitesinde gösterilecek açıklaması
        public string? WebDescription { get; set; }

        // Taş web sitesinde gösterilsin mi?
        public bool ShowOnWeb { get; set; } = false;
    }
}
