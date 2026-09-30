using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Scaidome.Json;

/// <summary>Adds the JSON rules to a host.</summary>
public static class JsonServiceCollectionExtensions
{
    /// <summary>
    /// Adds the registry of stored names and the one codec, resolved under its reading half, its writing half and itself, all
    /// the same instance.
    /// </summary>
    public static IServiceCollection AddScaiJson(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ISettingsKindRegistry, SettingsKindRegistry>();
        services.TryAddSingleton(_ => JsonCodecFactory.Create());
        services.TryAddSingleton<IJsonDocumentReader>(provider => provider.GetRequiredService<JsonDocumentCodec>());
        services.TryAddSingleton<IJsonDocumentWriter>(provider => provider.GetRequiredService<JsonDocumentCodec>());
        return services;
    }
}
