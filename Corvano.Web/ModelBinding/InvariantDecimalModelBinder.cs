using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Corvano.Web.ModelBinding;

/// <summary>
/// Arayüz tr-TR olsa da decimal alanlar kültürden bağımsız bağlanır:
/// "1290.00", "1290,00" ve "1.290,00" aynı değeri verir; son ayraç ondalıktır.
/// </summary>
public sealed class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
        var raw = value.FirstValue?.Replace(" ", string.Empty).Replace("₺", string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            return Task.CompletedTask;
        }

        if (decimal.TryParse(Normalize(raw), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Geçerli bir sayı yazın.");
        }

        return Task.CompletedTask;
    }

    private static string Normalize(string raw)
    {
        var decimalSeparator = Math.Max(raw.LastIndexOf(','), raw.LastIndexOf('.'));
        if (decimalSeparator < 0)
        {
            return raw;
        }

        var integerPart = raw[..decimalSeparator].Replace(",", string.Empty).Replace(".", string.Empty);
        return $"{integerPart}.{raw[(decimalSeparator + 1)..]}";
    }
}

public sealed class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
        => context.Metadata.UnderlyingOrModelType == typeof(decimal) ? new InvariantDecimalModelBinder() : null;
}
