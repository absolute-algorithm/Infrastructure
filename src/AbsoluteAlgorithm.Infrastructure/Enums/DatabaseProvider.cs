namespace AbsoluteAlgorithm.Infrastructure.Enums;

/// <summary>
/// Specifies the relational database provider.
/// </summary>
public enum DatabaseProvider : byte
{
    /// <summary>
    /// PostgreSQL.
    /// </summary>
    PostgreSQL = 1,

    /// <summary>
    /// Microsoft SQL Server.
    /// </summary>
    MSSQL
}