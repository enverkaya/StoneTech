using System.ComponentModel.DataAnnotations;
using StoneTech.Enums;

namespace StoneTech.Models.WorkOrder
{
    public class WorkOrderEditViewModel
    {
        // Düzenlenecek iş emrinin Id'si
        public int WorkOrderId { get; set; }

        // İş emrinin ait olduğu müşteri
        [Required(ErrorMessage = "Müşteri seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir müşteri seçiniz.")]
        public int CustomerId { get; set; }

        // İş emrinin oluşturulma tarihi
        [Required(ErrorMessage = "İş emri tarihi zorunludur.")]
        public DateTime CreatedDate { get; set; }

        // Atölyede yapılacak toplam kesim metresi
        [Range(0, double.MaxValue, ErrorMessage = "Kesim metresi 0'dan küçük olamaz.")]
        public decimal CuttingMeter { get; set; }

        // Kesimin metre başına KDV hariç işçilik fiyatı
        [Range(0, double.MaxValue, ErrorMessage = "Metre işçilik fiyatı 0'dan küçük olamaz.")]
        public decimal CuttingPricePerMeter { get; set; }

        // İş emrine uygulanacak KDV oranı
        [Range(0, 100, ErrorMessage = "KDV oranı 0 ile 100 arasında olmalıdır.")]
        public decimal VatRate { get; set; } = 20m;

        // İş emrinin mevcut durumu
        public WorkOrderStatus Status { get; set; }

        // İş emriyle ilgili genel açıklama
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        // İş emrinde kullanılan taşlar
        public List<WorkOrderDetailEditViewModel> Details { get; set; }
            = new List<WorkOrderDetailEditViewModel>();
    }
}