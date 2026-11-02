namespace StoneTech.Enums
{
    public enum WorkOrderStatus
    {
        PendingApproval = 1,   // Onay bekliyor
        Approved = 2,          // Yönetici tarafından onaylandı
        InWorkshop = 3,        // Atölyede işleme alındı
        Completed = 4,         // İş tamamlandı
        Rejected = 5,          // Yönetici tarafından reddedildi
        Cancelled = 6          // İş iptal edildi
    }
}