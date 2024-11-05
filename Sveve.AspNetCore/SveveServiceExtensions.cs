using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Sveve.AspNetCore;

public static class SveveServiceExtensions
{
    /// <summary>
    /// Registers <typeparamref name="T"/> as a transient <see cref="ISveveDeliveryConsumer"/>.
    /// </summary>
    public static IServiceCollection AddSveveDeliveryConsumer<T>(this IServiceCollection services) where T : class, ISveveDeliveryConsumer 
        => services.AddTransient<ISveveDeliveryConsumer, T>();

    /// <summary>
    /// Registers <typeparamref name="T"/> as a transient <see cref="ISveveSmsConsumer"/>.
    /// </summary>
    public static IServiceCollection AddSveveSmsConsumer<T>(this IServiceCollection services) where T : class, ISveveSmsConsumer 
        => services.AddTransient<ISveveSmsConsumer, T>();

    /// <summary>
    /// Registers <typeparamref name="T"/> as a transient <see cref="ISveveDeliveryConsumer"/> and <see cref="ISveveSmsConsumer"/>.
    /// </summary>
    public static IServiceCollection AddSveveConsumer<T>(this IServiceCollection services) where T : class, ISveveDeliveryConsumer, ISveveSmsConsumer 
        => services.AddSveveDeliveryConsumer<T>().AddSveveSmsConsumer<T>();

    /// <summary>
    /// Maps the endpoint used for consuming events from Sveve.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="RouteEndpointBuilder"/> has already been configured with documentation details and to allow anonymous requests.
    /// </remarks>
    /// <param name="builder"></param>
    /// <param name="pattern">Path to endpoint.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> to configure the endpoint.</returns>
    public static RouteHandlerBuilder MapSveveConsumerEndpoint(this IEndpointRouteBuilder builder, string pattern) => builder
        .MapPost(pattern, SveveConsumer.Endpoint)
        .AllowAnonymous()
        .WithGroupName("Sveve")
        .WithDisplayName("Consume Sveve notifications")
        .WithDescription("This is a callback endpoint for notifications from Sveve. The endpoint accepts all notifications Sveve can produce. To get started, copy the full URL to this endpoint into every callback on https://sveve.no/apidok/lev (Delivery reports) and https://sveve.no/apidok/motta (Incoming messages)");


}