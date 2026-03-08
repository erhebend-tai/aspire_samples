// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspireShop.NlpCache;

/// <summary>
/// Extension methods for registering the NLP caching library with a dependency-injection
/// container.
/// </summary>
public static class NlpCacheExtensions
{
    /// <summary>
    /// Registers <see cref="INlpCache"/> (implemented by <see cref="NlpCache"/>) and
    /// <see cref="ITokenizer"/> (implemented by <see cref="CodeTokenizer"/>) with the
    /// <paramref name="services"/> container.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Optional delegate to configure <see cref="NlpCacheOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddNlpCache(
        this IServiceCollection services,
        Action<NlpCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<INlpCache, NlpCache>();
        services.TryAddSingleton<ITokenizer>(
            _ => new CodeTokenizer(new CodeTokenizerOptions()));

        return services;
    }

    /// <summary>
    /// Registers <see cref="INlpCache"/> and <see cref="ITokenizer"/> using custom tokenizer
    /// options.
    /// </summary>
    public static IServiceCollection AddNlpCache(
        this IServiceCollection services,
        CodeTokenizerOptions tokenizerOptions,
        Action<NlpCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(tokenizerOptions, nameof(tokenizerOptions));

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<INlpCache, NlpCache>();
        services.TryAddSingleton<ITokenizer>(_ => new CodeTokenizer(tokenizerOptions));

        return services;
    }
}
