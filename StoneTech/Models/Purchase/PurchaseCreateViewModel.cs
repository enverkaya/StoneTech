using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Purchase
{
    public class PurchaseCreateViewModel
    {
        // Alımın yapılacağı tedarikçi
        [Required(ErrorMessage = "Tedarikçi seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir tedarikçi seçiniz.")]
        public int SupplierId { get; set; }

        // Alım tarihi
        [Required(ErrorMessage = "Alım tarihi zorunludur.")]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        // Alım hakkında isteğe bağlı açıklama
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        // Alım içerisindeki taşlar
        public List<PurchaseDetailCreateViewModel> Details { get; set; }
            = new List<PurchaseDetailCreateViewModel>();
    }
}