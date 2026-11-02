using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StoneTech.ModelBinding
{
    public sealed class InvariantDecimalModelBinder : IModelBinder
    {
        // HTML number alanlarının gönderdiği noktalı ondalık biçimi kültürden bağımsız okur.
        private const NumberStyles DecimalStyles =
            NumberStyles.AllowLeadingSign |
            NumberStyles.AllowDecimalPoint;

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var valueProviderResult =
                bindingContext.ValueProvider.GetValue(
                    bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(
                bindingContext.ModelName,
                valueProviderResult);

            var value = valueProviderResult.FirstValue?.Trim();
            var isNullable =
                Nullable.GetUnderlyingType(
                    bindingContext.ModelMetadata.ModelType) != null;

            if (string.IsNullOrWhiteSpace(value))
            {
                if (isNullable)
                {
                    bindingContext.Result =
                        ModelBindingResult.Success(null);
                }
                else
                {
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        "Bu alan için sayısal bir değer giriniz.");
                }

                return Task.CompletedTask;
            }

            if (decimal.TryParse(
                    value,
                    DecimalStyles,
                    CultureInfo.InvariantCulture,
                    out var parsedValue))
            {
                bindingContext.Result =
                    ModelBindingResult.Success(parsedValue);

                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "Geçerli bir ondalık değer giriniz.");

            return Task.CompletedTask;
        }
    }
}
