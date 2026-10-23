using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NArchitecture.Core.Security.WebApi.Swagger.Extensions;

/// <summary>
/// Adds a "Bearer" security requirement to every operation. The scheme itself must be declared once with
/// <c>options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" })</c>.
/// </summary>
public class BearerSecurityRequirementOperationFilter : IOperationFilter
{
    public const string SecuritySchemeName = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Security ??= [];
        operation.Security.Add(
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(SecuritySchemeName, context.Document)] = [] }
        );
    }
}
