using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StoneTech.ModelBinding
{
    public sealed class InvariantDecimalModelBinderProvider
        : IModelBinderProvider
    {
        private static readonly IModelBinder Binder =
            new InvariantDecimalModelBinder();

        public IModelBinder? GetBinder(
            ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var modelType =
                Nullable.GetUnderlyingType(
                    context.Metadata.ModelType) ??
                context.Metadata.ModelType;

            return modelType == typeof(decimal)
                ? Binder
                : null;
        }
    }
}
