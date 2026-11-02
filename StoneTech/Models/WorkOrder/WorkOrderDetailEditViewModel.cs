using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.WorkOrder
{
    public class WorkOrderDetailEditViewModel
    {
        // Mevcut iş emri detayının Id'si
        // Edit sırasında eski kaydı takip etmek için kullanılır
        public int WorkOrderDetailId { get; set; }

        // Kullanılan taşın Id'si
        [Required(ErrorMessage = "Taş seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir taş seçiniz.")]
        public int StoneId { get; set; }

        // Kullanılan plaka adedi
        [Required(ErrorMessage = "Plaka adedi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Plaka adedi en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        // Plaka başına uygulanan satış fiyatı
        [Required(ErrorMessage = "Birim fiyat zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Birim fiyat 0'dan büyük olmalıdır.")]
        public decimal UnitPrice { get; set; }

        // Taşa ait ölçü veya işlem açıklaması
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }
    }
}