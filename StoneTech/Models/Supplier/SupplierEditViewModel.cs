using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.Supplier
{
    public class SupplierEditViewModel
    {
        // Güncellenecek tedarikçinin Id bilgisi
        public int SupplierId { get; set; }

        // Tedarikçi veya firma adı
        [Required(ErrorMessage = "Tedarikçi adı zorunludur.")]
        [StringLength(150, ErrorMessage = "Tedarikçi adı en fazla 150 karakter olabilir.")]
        public string SupplierName { get; set; } = string.Empty;

        // Telefon bilgisi isteğe bağlıdır
        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        public string? Phone { get; set; }

        // Adres bilgisi isteğe bağlıdır
        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        public string? Address { get; set; }

        // Tedarikçi hakkında not
        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        public string? Note { get; set; }

        // Tedarikçi sistemde aktif mi
        public bool IsActive { get; set; }
    }
}