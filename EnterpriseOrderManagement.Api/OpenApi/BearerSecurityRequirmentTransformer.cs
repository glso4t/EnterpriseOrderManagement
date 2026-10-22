using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EnterpriseOrderManagement.Api.OpenApi;

//Αυτό το transformer καταγράφει το requirement για Bearer authentication 
// στα endpoints που δεν έχουν [AllowAnonymous].
//Το κρατάμε έτσι επειδή οι υπάρχοντες controllers σου απαιτούν authentication. 
// Όταν αργότερα δημιουργήσουμε Login και Register, θα χρησιμοποιήσουμε 
// [AllowAnonymous] σε αυτά τα endpoints, ώστε η τεκμηρίωση να μην απαιτεί 
// token για να συνδεθεί ένας νέος χρήστης.
internal sealed class BearerSecurityRequirementTransformer
    : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata =
            context.Description.ActionDescriptor.EndpointMetadata;
        // Public endpoints, such as a future login/register,
        // should not require a Bearer token.
        if (metadata.OfType<AllowAnonymousAttribute>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    "Bearer",
                    context.Document)] = []
            });

        return Task.CompletedTask;
    }
}