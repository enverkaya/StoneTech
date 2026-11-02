using StoneTech.Enums;

namespace StoneTech.Entities
{
    public class Payment
    {
        // Ödemenin benzersiz kimliği
        public int PaymentId { get; set; }

        // Ödemenin ait olduğu iş emri
        public int WorkOrderId { get; set; }

        // Ödenen tutar
        public decimal Amount { get; set; }

        // Ödeme tarihi
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Ödemenin hangi yöntemle yapıldığı
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        // Ödemeyle ilgili açıklama
        public string? Description { get; set; }

        // İlişkili iş emri
        public WorkOrder WorkOrder { get; set; } = null!;
    }
}