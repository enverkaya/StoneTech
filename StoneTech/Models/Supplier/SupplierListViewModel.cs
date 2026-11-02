namespace StoneTech.Models.Supplier
{
    public class SupplierListViewModel
    {
        // Tedarikçinin Id bilgisi
        public int SupplierId { get; set; }

        // Tedarikçi veya firma adı
        public string SupplierName { get; set; } = string.Empty;

        // Telefon bilgisi
        public string? Phone { get; set; }

        // Adres bilgisi
        public string? Address { get; set; }

        // Tedarikçi hakkında not
        public string? Note { get; set; }

        // Tedarikçi sistemde aktif mi
        public bool IsActive { get; set; }
    }
}