using System.Data;
using FileFormula.Api.Infrastructure.Constraints;
using Microsoft.AspNetCore.Http;

namespace FileFormula.Api.Infrastructure.Middlewares;

/// <summary>
/// Commits or rolls back request-scoped database transactions.
/// </summary>
public class DatabaseTransactionMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTransactionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public DatabaseTransactionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the current request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);

            if (context.Items.TryGetValue(DATABASE.CONNECTIONSKEY, out var obj) &&
                obj is Dictionary<string, (IDbConnection Connection, IDbTransaction Transaction)> connections)
            {
                foreach (var (_, (_, transaction)) in connections)
                {
                    if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                    {
                        transaction.Commit();
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
            }
        }
        catch
        {
            if (context.Items.TryGetValue(DATABASE.CONNECTIONSKEY, out var obj) &&
                obj is Dictionary<string, (IDbConnection Connection, IDbTransaction Transaction)> connections)
            {
                foreach (var (_, (_, transaction)) in connections)
                {
                    transaction.Rollback();
                }
            }

            throw;
        }
        finally
        {
            if (context.Items.TryGetValue(DATABASE.CONNECTIONSKEY, out var obj) &&
                obj is Dictionary<string, (IDbConnection Connection, IDbTransaction Transaction)> connections)
            {
                foreach (var (_, (connection, transaction)) in connections)
                {
                    transaction.Dispose();
                    connection.Dispose();
                }
            }
        }
    }
}