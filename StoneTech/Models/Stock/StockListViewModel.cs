namespace StoneTech.Models.Stock
{
    public class StockListViewModel
    {
        public int StoneId { get; set; }

        //Taşın adı
        public string StoneName { get; set; } = string.Empty;   

        //Taşın rengi 
        public string? Color { get; set; }

        //Taşa ait görsel 
        public string? ImageUrl { get; set; }

        //Taşın mevcut plaka stoğu
        public int CurrentStock {  get; set; }

        // Taşın güncel alış fiyatı ortalama 
        public decimal AveragePurchasePrice { get; set; }

        // Mevcut stok miktarının toplam maliyet değeri
        // CurrentStock x AveragePurchasePrice
        public decimal StockValue { get; set; }

        // Taşın sistemde aktif olup olmadığını belirtir
        public bool IsActive { get; set; }

    }
}
