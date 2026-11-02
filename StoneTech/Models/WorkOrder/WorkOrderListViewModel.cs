using StoneTech.Enums;

namespace StoneTech.Models.WorkOrder
{
    public class WorkOrderListViewModel
    {
        // İş emrinin Id'si
        public int WorkOrderId { get; set; }
        // Kullanıcıya gösterilecek iş emri numarası
        // Örnek: IE-2026-000001
        public string WorkOrderNumber { get; set; } = string.Empty;

        // Müşterinin adı soyadı veya firma adı
        public string CustomerName { get; set; } = string.Empty;

        // İş emrinin oluşturulma tarihi
        public DateTime CreatedDate { get; set; }

        // İş emrinin mevcut durumu
        public WorkOrderStatus Status { get; set; }

        // İş emrinin taş ve işçilik dahil toplam tutarı
        public decimal TotalAmount { get; set; }

        // İş emrine yapılan toplam ödeme
        public decimal TotalPaid { get; set; }

        // İş emrinin kalan borcu
        public decimal RemainingAmount { get; set; }
    }
}