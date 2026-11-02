
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Stone
{
    public class StoneEditViewModel
    {
        // Güncellenecek taşın Id bilgisi
        public int StoneId { get; set; }

        // Taş adı boş bırakılamaz
        [Required(ErrorMessage = "Taş adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Taş adı en fazla 100 karakter olabilir.")]
        public string StoneName { get; set; } = string.Empty;

        // Taş rengi isteğe bağlıdır
        [StringLength(50, ErrorMessage = "Renk en fazla 50 karakter olabilir.")]
        public string? Color { get; set; }

        // Taşın alış fiyatı
        [Range(0, double.MaxValue, ErrorMessage = "Alış fiyatı negatif olamaz.")]
        public decimal PurchasePrice { get; set; }

        // Taşın satış fiyatı
        [Range(0, double.MaxValue, ErrorMessage = "Satış fiyatı negatif olamaz.")]
        public decimal SalePrice { get; set; }

        // Taş sistemde aktif mi?
        public bool IsActive { get; set; }

        // Taşın veritabanında kayıtlı mevcut görsel yolu
        public string? CurrentImageUrl { get; set; }

        // Kullanıcının yükleyeceği yeni taş görseli
        public IFormFile? ImageFile { get; set; }

        // Taşın web sitesinde gösterilecek açıklaması
        public string? WebDescription { get; set; }

        // Taş web sitesinde gösterilsin mi?
        public bool ShowOnWeb { get; set; } = false;
    }
}
