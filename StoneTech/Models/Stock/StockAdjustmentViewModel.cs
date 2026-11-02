using System.ComponentModel.DataAnnotations;
using StoneTech.Enums;

namespace StoneTech.Models.Stock
{
    public class StockAdjustmentViewModel
    {
        // Düzeltme yapılacak taşın Id'si
        [Required]
        public int StoneId { get; set; }

        // Düzeltme yapılacak taşın adı
        // Ekranda bilgi amaçlı gösterilir
        public string StoneName { get; set; } = string.Empty;

        // Düzeltme yapılmadan önceki mevcut plaka stoğu
        // Ekranda bilgi amaçlı gösterilir
        public int CurrentStock { get; set; }

        // Manuel stok düzeltmesinin türü
        // Sadece AdjustmentIn veya AdjustmentOut kullanılabilir
        [Required(ErrorMessage = "Düzeltme tipi zorunludur.")]
        public StockMovementType MovementType { get; set; }

        // Stokta artırılacak veya azaltılacak plaka adedi
        [Required(ErrorMessage = "Plaka adedi zorunludur.")]
        [Range( 1,int.MaxValue,ErrorMessage = "Plaka adedi en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        // Manuel stok düzeltmesinin yapılma sebebi
        [Required(ErrorMessage = "Düzeltme sebebi zorunludur.")]
        [StringLength(
            500,
            ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; } = string.Empty;
    }
}