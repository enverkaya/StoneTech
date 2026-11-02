

namespace StoneTech.Entities
{
    public class Supplier
    {
        // Tedarikçinin Id
        public int SupplierId { get; set; }

        // Firma veya tedarikçi adı
        public string SupplierName { get; set; } = string.Empty;

        // Telefon numarası
        public string? Phone { get; set; }

        // Adres bilgisi
        public string? Address { get; set; }

        // Tedarikçiyle ilgili not
        public string? Note { get; set; }

        // Tedarikçi aktif mi?
        public bool IsActive { get; set; } = true;

        // Tedarikçiden yapılan alımlar
        public ICollection<Purchase> Purchases { get; set; }
            = new List<Purchase>();
    }
}