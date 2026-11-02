using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Purchase
{
    public class PurchaseDetailEditViewModel
    {
        public int PurchaseDetailId { get; set; }

        [Required(ErrorMessage = "Taş seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir taş seçiniz.")]
        public int StoneId { get; set; }

        [Required(ErrorMessage = "Plaka adedi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Plaka adedi en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Birim fiyat zorunludur.")]
        [Range(
            0.01,
            double.MaxValue,
            ErrorMessage = "Birim fiyat 0'dan büyük olmalıdır.")]
        public decimal UnitPrice { get; set; }
    }
}