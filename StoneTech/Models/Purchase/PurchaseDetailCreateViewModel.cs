using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Purchase
{
    public class PurchaseDetailCreateViewModel
    {
        // Satın alınan taş
        [Required(ErrorMessage = "Taş seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir taş seçiniz.")]
        public int StoneId { get; set; }

        // Satın alınan plaka adedi
        [Required(ErrorMessage = "Plaka adedi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Plaka adedi en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        // Bir plakanın alış fiyatı
        [Required(ErrorMessage = "Birim fiyat zorunludur.")]
        [Range(
            0.01,
            double.MaxValue,
            ErrorMessage = "Birim fiyat 0'dan büyük olmalıdır.")]
        public decimal UnitPrice { get; set; }
    }
}