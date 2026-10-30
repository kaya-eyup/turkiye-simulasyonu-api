namespace TurkiyeSimulasyonu.Api.Infrastructure;

// OpenAPI belgesinin kurulumu tek yerde: Program.cs sadece "belgeyi ekle" der
public static class ApiDocs
{
    public static IServiceCollection AddApiDocs(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<ApiDocInfoTransformer>();
        });
        return services;
    }
}
