using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.WorkOrder
{
    public class WorkOrderDetailCreateViewModel
    {
        // İş emrinde kullanılacak taşın Id'si
        [Required(ErrorMessage = "Taş seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir taş seçiniz.")]
        public int StoneId { get; set; }

        // Kullanılacak plaka adedi
        [Required(ErrorMessage = "Plaka adedi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Plaka adedi en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        // Plaka başına uygulanacak satış fiyatı
        [Required(ErrorMessage = "Birim fiyat zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Birim fiyat 0'dan büyük olmalıdır.")]
        public decimal UnitPrice { get; set; }

        // Bu taş için yapılacak işlem veya ölçü bilgisi
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }
    }
}