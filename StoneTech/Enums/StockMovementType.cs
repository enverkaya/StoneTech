namespace StoneTech.Enums
{
    public enum StockMovementType
    {
        Entry = 1,          // Alım nedeniyle stok girişi
        Exit = 2,           // İş emri nedeniyle stok çıkışı
        AdjustmentIn = 3,   // Manuel stok artırma düzeltmesi
        AdjustmentOut = 4,  // Manuel stok azaltma düzeltmesi
        WorkOrderReturn = 5 // İptal edilen iş emrinden stok iadesi
    }
}