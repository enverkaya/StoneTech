using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Purchase
{
    public class PurchaseEditViewModel
    {
        public int PurchaseId { get; set; }

        // Alım numarası kullanıcı tarafından değiştirilemez.
        // Edit ekranında sadece bilgi amaçlı gösterilir.
        public string PurchaseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tedarikçi seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir tedarikçi seçiniz.")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Alım tarihi zorunludur.")]
        public DateTime PurchaseDate { get; set; }

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        public List<PurchaseDetailEditViewModel> Details { get; set; }
            = new List<PurchaseDetailEditViewModel>();
    }
}