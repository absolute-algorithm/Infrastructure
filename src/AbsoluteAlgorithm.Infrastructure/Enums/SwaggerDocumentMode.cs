namespace AbsoluteAlgorithm.Infrastructure.Enums;

/// <summary>
/// Specifies how Swagger documents are generated.
/// </summary>
public enum SwaggerDocumentMode : byte
{
    /// <summary>
    /// Generates a single document for the entire API surface.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Generates a separate document for each configured API-version group.
    /// </summary>
    PerApiVersion
}