using Core.Application.Pipelines.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.WebAPI.Extensions.Swagger;

public class BearerSecurityRequirementOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.MethodInfo.GetParameters().Any(x => x.ParameterType.GetInterfaces().Contains(typeof(ISecuredRequest))))
            return;

        OpenApiSecurityRequirement authAttribute =
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    Array.Empty<string>()
                }
            };
        operation.Security.Add(authAttribute);
    }
}
