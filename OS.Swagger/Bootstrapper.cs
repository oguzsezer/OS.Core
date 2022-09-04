using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace OS.Swagger
{
    public static class Bootstrapper
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services, Action<SwaggerOptions> optionsAction)
        {
            var options = new SwaggerOptions();
            optionsAction.Invoke(options);
            return services.AddEndpointsApiExplorer()
                .AddSwaggerGen(x =>
                {
                    x.UseInlineDefinitionsForEnums();
                    x.CustomSchemaIds(type => type.FullName);
                    foreach (var name in Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.AllDirectories))
                    {
                        x.IncludeXmlComments(name);
                    }

                    x.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = Assembly.GetExecutingAssembly().GetName().Name
                    });

                    if (options.AddBearerAuthorization)
                    {
                        var securitySchema = new OpenApiSecurityScheme
                        {
                            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                            Name = "Authorization",
                            In = ParameterLocation.Header,
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        };

                        x.AddSecurityDefinition("Bearer", securitySchema);
                        x.AddSecurityRequirement(new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } } });
                    }
                }).AddSwaggerGenNewtonsoftSupport();
        }

        /// <summary>
        /// Configures swagger for insensitive environments
        /// </summary>
        /// <param name="app"></param>
        /// <param name="path">apigateway path (istio virtualservice, etc.)</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSwagger(this WebApplication app, string path)
        {
            if (app.Environment.IsProduction() || app.Environment.IsStaging())
            {
                return app;
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseSwagger(x =>
                {
                    x.RouteTemplate = "swagger/{documentName}/swagger.json";
                    x.PreSerializeFilters.Add((swaggerDocument, httpRequest) =>
                    {
                        swaggerDocument.Servers = new List<OpenApiServer>
                        {
                            new() { Url = $"https://{httpRequest.Host.Value}/{path.Replace("/", "").Trim()}" }
                        };
                    });
                });
            }
            else
            {
                app.UseSwagger();
            }

            app.UseSwaggerUI(x => x.DisplayRequestDuration());

            return app;
        }
    }
}