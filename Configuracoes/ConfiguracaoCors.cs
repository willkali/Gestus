namespace Gestus.Configuracoes;

public static class ConfiguracaoCors
{
    public static IServiceCollection ConfigurarCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsConfig = configuration.GetSection("Cors");
        
        services.AddCors(options =>
        {
            options.AddPolicy("PoliticaCorsGestus", builder =>
            {
                var origensPermitidas = corsConfig.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                var cabecalhosPermitidos = corsConfig.GetSection("AllowedHeaders").Get<string[]>() ?? Array.Empty<string>();
                var metodosPermitidos = corsConfig.GetSection("AllowedMethods").Get<string[]>() ?? Array.Empty<string>();

                builder.WithOrigins(origensPermitidas)
                       .WithHeaders(cabecalhosPermitidos)
                       .WithMethods(metodosPermitidos)
                       .AllowCredentials();
            });
        });

        return services;
    }
}