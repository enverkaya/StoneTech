
using System.ComponentModel.DataAnnotations;

namespace StoneTech.Models.WhyChooseUs
{
    public class WhyChooseUsEditViewModel
    {
        // Düzenlenecek özelliğin kimlik numarası
        public int Id { get; set; }

        // Özellik başlığı
        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
        public string Title { get; set; } = string.Empty;

        // Özellik açıklaması
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        // Özelliğin Font Awesome ikon sınıfı
        [StringLength(150, ErrorMessage = "İkon sınıfı en fazla 150 karakter olabilir.")]
        public string? IconClass { get; set; }

        // Görüntülenme sırası
        [Range(0, int.MaxValue, ErrorMessage = "Sıralama negatif olamaz.")]
        public int DisplayOrder { get; set; }

        // Özellik web sitesinde gösterilsin mi?
        public bool IsActive { get; set; }
    }
}
