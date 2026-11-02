using Microsoft.AspNetCore.Http;

namespace StoneTech.Services
{
    // Yüklenen görsellerin güvenliğini kontrol eder
    public static class ImageUploadValidator
    {
        // İzin verilen uzantılar ve içerik türleri
        private static readonly IReadOnlyDictionary<string, string[]> AllowedContentTypes =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = ["image/jpeg", "image/pjpeg"],
                [".jpeg"] = ["image/jpeg", "image/pjpeg"],
                [".png"] = ["image/png"],
                [".webp"] = ["image/webp"]
            };

        // Görselin boyutunu, türünü ve gerçek dosya içeriğini doğrular
        public static string? Validate(IFormFile imageFile, long maxFileSize)
        {
            // Boş dosyayı engeller
            if (imageFile.Length <= 0)
                return "Seçilen görsel dosyası boş.";

            // Maksimum dosya boyutunu kontrol eder
            if (imageFile.Length > maxFileSize)
                return "Görsel boyutu en fazla 5 MB olabilir.";

            // Dosya uzantısını alır
            string extension = Path
                .GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            // İzin verilmeyen uzantıları engeller
            if (!AllowedContentTypes.TryGetValue(
                    extension,
                    out string[]? allowedContentTypes))
            {
                return "Sadece JPG, JPEG, PNG veya WEBP görsel yükleyebilirsiniz.";
            }

            // Uzantı ile içerik türünün uyumunu kontrol eder
            if (string.IsNullOrWhiteSpace(imageFile.ContentType) ||
                !allowedContentTypes.Contains(
                    imageFile.ContentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                return "Dosyanın içerik türü seçilen görsel formatıyla uyuşmuyor.";
            }

            // Gerçek dosya türünü kontrol etmek için ilk byte'ları okur
            Span<byte> header = stackalloc byte[12];
            int bytesRead;

            try
            {
                using Stream stream = imageFile.OpenReadStream();
                bytesRead = stream.Read(header);
            }
            catch
            {
                return "Görsel dosyası okunamadı.";
            }

            // JPG, PNG veya WEBP dosya imzasını doğrular
            bool signatureIsValid = extension switch
            {
                ".jpg" or ".jpeg" =>
                    bytesRead >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                ".png" =>
                    bytesRead >= 8 &&
                    header[..8].SequenceEqual(
                        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),

                ".webp" =>
                    bytesRead >= 12 &&
                    header[..4].SequenceEqual("RIFF"u8) &&
                    header[8..12].SequenceEqual("WEBP"u8),

                _ => false
            };

            // Dosya gerçek bir görselse kabul eder
            return signatureIsValid
                ? null
                : "Dosya içeriği geçerli bir görsel formatında değil.";
        }
    }
}