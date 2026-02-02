using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WaybillManagementSystem.Swagger;

/// <summary>
/// Document filter to add security requirement for X-Tenant-ID header to all operations.
/// This makes the "Authorize" button work - when users authorize, the header is automatically included.
/// </summary>
public class TenantSecurityDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add security requirement to all paths and operations
        foreach (var pathItem in swaggerDoc.Paths.Values)
        {
            foreach (var operation in pathItem.Operations.Values)
            {
                if (operation.Security == null)
                {
                    operation.Security = new List<OpenApiSecurityRequirement>();
                }

                // Create security requirement using the scheme name as key (must match AddSecurityDefinition name)
                // Swagger UI will resolve this to the security scheme definition
                // For Swashbuckle 6.5.0, use OpenApiSecuritySchemeReference
                var securityRequirement = new OpenApiSecurityRequirement();
                var schemeRef = new OpenApiSecuritySchemeReference("TenantIdHeader");
                securityRequirement[schemeRef] = new List<string>();
                
                operation.Security.Add(securityRequirement);
            }
        }
    }
}
