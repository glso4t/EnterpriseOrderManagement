using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EnterpriseOrderManagement.Api.OpenApi;

//Αναγνωρίζει αν υπάρχει καταχωρισμένο JWT Bearer scheme και το προσθέτει στο OpenAPI document. 
// Έτσι η τεκμηρίωση γνωρίζει ότι το API χρησιμοποιεί Bearer authentication.
//Δεν δημιουργεί tokens και δεν εκδίδει JWT. Απλώς περιγράφει τον μηχανισμό authentication 
// στην τεκμηρίωση.
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes =
            await authenticationSchemeProvider.GetAllSchemesAsync();

        var bearerExists = schemes.Any(
            scheme =>
                scheme.Name ==
                JwtBearerDefaults.AuthenticationScheme);

        if (!bearerExists)
        {
            return;
        }

        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes =
            new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT"
                }
            };
    }
}