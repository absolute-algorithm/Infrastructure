using System.Data;
using System.ComponentModel;
using System.Text;
using AbsoluteAlgorithm.Infrastructure.ResilienceFactories;
using AbsoluteAlgorithm.Core.Constraints;
using AbsoluteAlgorithm.Core.Exceptions;
using AbsoluteAlgorithm.Core.Enums;
using AbsoluteAlgorithm.Core.Models.Database;
using AbsoluteAlgorithm.Core.Models.Pagination;
using Dapper;
using Microsoft.AspNetCore.Http;
using AbsoluteAlgorithm.Core.Concurrency;

namespace AbsoluteAlgorithm.Infrastructure.Database;

/// <summary>
/// Provides Dapper-based data access for a configured database policy.
/// </summary>
/// <remarks>
/// Instances of this type participate in the request-scoped transaction managed by the library middleware.
/// </remarks>
public class Repository
{
    private readonly DatabasePolicy _policy;
    private readonly string _connectionString;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository"/> class.
    /// </summary>
    /// <param name="policy">The database policy.</param>
    /// <param name="httpContextAccessor">The accessor used to obtain the current HTTP context.</param>
    public Repository(DatabasePolicy policy, IHttpContextAccessor httpContextAccessor)
    {
        _policy = policy;
        _connectionString = Environment.GetEnvironmentVariable(policy.ConnectionStringName!) ?? throw new InvalidOperationException($"Database secret '{policy.ConnectionStringName}' is missing.");
        _httpContextAccessor = httpContextAccessor;
    }

    private IDbConnection GetConnection()
    {
        var connection = DbConnectionFactory.Create(_policy, _connectionString);

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        return connection;
    }

    private Dictionary<string, (IDbConnection Connection, IDbTransaction Transaction)> GetOrCreateConnectionStore(HttpContext context)
    {
        if (context.Items.TryGetValue(DATABASE.CONNECTIONSKEY, out var obj) &&
            obj is Dictionary<string, (IDbConnection Connection, IDbTransaction Transaction)> connections)
        {
            return connections;
        }

        var createdConnections = new Dictionary<string, (IDbConnection Connection, IDbTransaction Transaction)>();
        context.Items[DATABASE.CONNECTIONSKEY] = createdConnections;

        return createdConnections;
    }

    private (IDbConnection Connection, IDbTransaction Transaction) BeginTransaction()
    {
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext found.");

        var connections = GetOrCreateConnectionStore(context);

        if (connections.TryGetValue(_policy.Name, out var existing))
        {
            return existing;
        }

        var connection = DbConnectionFactory.Create(_policy, _connectionString);
        connection.Open();

        var transaction = connection.BeginTransaction();

        var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
        var correlationId = _httpContextAccessor.HttpContext?.Request?.Headers[HEADER.CORRELATIONID].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        string sessionSql = _policy.DatabaseProvider switch
        {
            DatabaseProvider.PostgreSQL =>
                "SELECT set_config('app.user_id', @userId, true), set_config('app.correlation_id', @correlationId, true);",

            DatabaseProvider.MSSQL =>
                "EXEC sp_set_session_context @key=N'user_id', @value=@userId; EXEC sp_set_session_context @key=N'correlation_id', @value=@correlationId;",

            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(sessionSql))
        {
            connection.Execute(sessionSql, new { userId, correlationId }, transaction);
        }

        connections[_policy.Name] = (connection, transaction);

        return (connection, transaction);
    }

    private (IDbConnection Connection, IDbTransaction? Transaction) GetRequestResources()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No HttpContext found.");

        var connections = GetOrCreateConnectionStore(context);

        if (connections.TryGetValue(_policy.Name, out var resources))
        {
            return resources;
        }

        return BeginTransaction();
    }

    private static CommandDefinition CreateInterpolatedCommand(
        FormattableString sql,
        IDbTransaction? transaction,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var (commandText, parameters) = BuildParameterizedSql(sql);

        return new CommandDefinition(
            commandText: commandText,
            parameters: parameters,
            transaction: transaction,
            commandTimeout: commandTimeout,
            commandType: commandType,
            cancellationToken: cancellationToken);
    }

    private static (string CommandText, DynamicParameters Parameters) BuildParameterizedSql(FormattableString sql)
    {
        var format = sql.Format;
        var arguments = sql.GetArguments();
        var parameters = new DynamicParameters();
        var builder = new StringBuilder(format.Length + (arguments.Length * 4));

        for (var index = 0; index < format.Length; index++)
        {
            char current = format[index];

            if (current == '{')
            {
                if (index + 1 < format.Length && format[index + 1] == '{')
                {
                    builder.Append('{');
                    index++;
                    continue;
                }

                var endIndex = format.IndexOf('}', index + 1);
                if (endIndex < 0)
                {
                    throw new FormatException("Invalid interpolated SQL format string.");
                }

                var token = format[(index + 1)..endIndex];
                var separatorIndex = token.IndexOfAny([',', ':']);
                var argumentIndexText = separatorIndex >= 0 ? token[..separatorIndex] : token;

                if (!int.TryParse(argumentIndexText, out var argumentIndex) || argumentIndex < 0 || argumentIndex >= arguments.Length)
                {
                    throw new FormatException("Invalid interpolated SQL format string.");
                }

                var parameterName = $"p{argumentIndex}";
                builder.Append('@').Append(parameterName);
                parameters.Add(parameterName, arguments[argumentIndex]);
                index = endIndex;
                continue;
            }

            if (current == '}' && index + 1 < format.Length && format[index + 1] == '}')
            {
                builder.Append('}');
                index++;
                continue;
            }

            builder.Append(current);
        }

        return (builder.ToString(), parameters);
    }

    /// <summary>
    /// Executes an interpolated query and maps the result set to the specified type.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="sql">The interpolated SQL query. Interpolated values are converted to parameters automatically.</param>
    /// <param name="commandTimeout">The command timeout, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A sequence of mapped results.</returns>
    /// <remarks>
    /// This is the preferred query overload for values that originate outside the application boundary. SQL identifiers such as table names and column names cannot be parameterized and must not come from untrusted input.
    /// </remarks>
    public virtual async Task<IEnumerable<T>> QueryInterpolatedAsync<T>(FormattableString sql, int? commandTimeout = null, CancellationToken cancellationToken = default) where T : class
    {
        var (connection, transaction) = GetRequestResources();

        return await ExecuteWithResilienceAsync(
            token =>
            {
                var command = CreateInterpolatedCommand(sql, transaction, commandTimeout, cancellationToken: token);
                return connection.QueryAsync<T>(command);
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes a query and maps the result set to the specified type.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">The parameter values to apply to the query.</param>
    /// <param name="commandTimeout">The command timeout, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A sequence of mapped results.</returns>
    /// <remarks>
    /// Advanced usage only. Use this overload when you need direct control over SQL text and parameters. Callers are responsible for parameterizing all user input correctly. For standard value interpolation scenarios, prefer <see cref="QueryInterpolatedAsync{T}(FormattableString, int?, CancellationToken)"/>.
    /// </remarks>
    public virtual async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where T : class
    {
        var (connection, transaction) = GetRequestResources();

        return await ExecuteWithResilienceAsync(
            token =>
            {
                var command = new CommandDefinition(commandText: sql, parameters: parameters, transaction: transaction, commandTimeout: commandTimeout, cancellationToken: token);
                return connection.QueryAsync<T>(command);
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes a query and maps the result set to the specified type.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">The parameter values to apply to the query.</param>
    /// <param name="commandTimeout">The command timeout, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A sequence of mapped results.</returns>
    /// <remarks>
    /// Compatibility shim for the misspelled legacy API. Use <see cref="QueryAsync{T}(string, object?, int?, CancellationToken)"/> for raw SQL or <see cref="QueryInterpolatedAsync{T}(FormattableString, int?, CancellationToken)"/> for the preferred parameterized path.
    /// </remarks>
    [Obsolete("Use QueryAsync instead. This misspelled overload is retained for compatibility.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual Task<IEnumerable<T>> QueryAysnc<T>(string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where T : class
    {
        return QueryAsync<T>(sql, parameters, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Executes a command within the current request-scoped transaction.
    /// </summary>
    /// <param name="sql">The SQL command to execute.</param>
    /// <param name="parameters">The parameter values to apply to the command.</param>
    /// <param name="commandTimeout">The command timeout, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    /// <remarks>
    /// Advanced usage only. Use this overload when you need direct control over SQL text and parameters. Callers are responsible for parameterizing all user input correctly. For standard value interpolation scenarios, prefer <see cref="ExecuteInterpolatedAsync(FormattableString, int?, CancellationToken)"/>.
    /// </remarks>
    public virtual async Task<int> ExecuteAsync(string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = GetRequestResources();

        return await ExecuteWithResilienceAsync(
            token =>
            {
                var command = new CommandDefinition(commandText: sql, parameters: parameters, transaction: transaction, commandTimeout: commandTimeout, cancellationToken: token);
                return connection.ExecuteAsync(command);
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes an interpolated command within the current request-scoped transaction.
    /// </summary>
    /// <param name="sql">The interpolated SQL command. Interpolated values are converted to parameters automatically.</param>
    /// <param name="commandTimeout">The command timeout, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    /// <remarks>
    /// This is the preferred command overload for values that originate outside the application boundary. SQL identifiers such as table names and column names cannot be parameterized and must not come from untrusted input.
    /// </remarks>
    public virtual async Task<int> ExecuteInterpolatedAsync(FormattableString sql, int? commandTimeout = null, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = GetRequestResources();

        return await ExecuteWithResilienceAsync(
            token =>
            {
                var command = CreateInterpolatedCommand(sql, transaction, commandTimeout, cancellationToken: token);
                return connection.ExecuteAsync(command);
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes a stored procedure within the current request-scoped transaction.
    /// </summary>
    /// <param name="procedureName">The stored procedure name.</param>
    /// <param name="parameters">The parameter values to apply to the procedure.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual async Task<int> ExecuteStoredProcedureAsync(string procedureName, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = GetRequestResources();

        return await ExecuteWithResilienceAsync(
            token =>
            {
                var command = new CommandDefinition(commandText: procedureName, parameters: parameters, transaction: transaction, commandType: CommandType.StoredProcedure, cancellationToken: token);
                return connection.ExecuteAsync(command);
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes a scalar query and returns the first column of the first row.
    /// </summary>
    /// <typeparam name="T">The scalar value type.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">The parameter values to apply to the query.</param>
    /// <param name="commandTimeout">The command timeout, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scalar value.</returns>
    /// <remarks>
    /// Advanced usage only. Callers are responsible for parameterizing all user input correctly.
    /// </remarks>
    public virtual async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = GetRequestResources();

        return await ExecuteWithResilienceAsync(
            token =>
            {
                var command = new CommandDefinition(commandText: sql, parameters: parameters, transaction: transaction, commandTimeout: commandTimeout, cancellationToken: token);
                return connection.ExecuteScalarAsync<T?>(command);
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes a paged query by applying validated search, filter, sort, and paging instructions to trusted SQL fragments.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="query">The paged query definition.</param>
    /// <param name="request">The paging request.</param>
    /// <param name="parameters">The base SQL parameters.</param>
    /// <param name="commandTimeout">The command timeout, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result containing the current page and total row count.</returns>
    public virtual async Task<PagedResult<T>> QueryPageAsync<T>(RepositoryPagedQuery query, PagedRequest? request = null, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.SelectSql);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.CountSql);

        var effectiveRequest = request ?? new PagedRequest();
        var safePageNumber = Math.Max(1, effectiveRequest.PageNumber);
        var safePageSize = Math.Max(1, effectiveRequest.PageSize);
        var sqlParameters = CreateDynamicParameters(parameters);
        var whereFragments = BuildWhereFragments(query, effectiveRequest, sqlParameters);
        var selectSql = AppendWhereClause(query.SelectSql, whereFragments);
        var countSql = AppendWhereClause(query.CountSql, whereFragments);
        var orderByClause = BuildOrderByClause(query, effectiveRequest);

        sqlParameters.Add("__pageOffset", Math.Max(0, (safePageNumber - 1) * safePageSize));
        sqlParameters.Add("__pageSize", safePageSize);

        var pagedSelectSql = selectSql + orderByClause + BuildPagingClause(orderByClause);
        var totalCount = await ExecuteScalarAsync<long>(countSql, sqlParameters, commandTimeout, cancellationToken);
        var items = (await QueryAsync<T>(pagedSelectSql, sqlParameters, commandTimeout, cancellationToken)).ToList();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    /// <summary>
    /// Executes an update statement that follows an optimistic concurrency pattern and surfaces not-found, precondition, and conflict outcomes consistently.
    /// </summary>
    /// <param name="definition">The optimistic update definition.</param>
    /// <param name="parameters">The SQL parameters used by the supplied statements.</param>
    /// <param name="commandTimeout">The command timeout, in seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The successful update result, including the latest version token when available.</returns>
    public virtual async Task<RepositoryOptimisticUpdateResult> ExecuteOptimisticUpdateAsync(RepositoryOptimisticUpdateDefinition definition, object? parameters = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.UpdateSql);

        if (definition.RequireIfMatchHeader && string.IsNullOrWhiteSpace(definition.CurrentVersionSql))
        {
            throw new InvalidOperationException("CurrentVersionSql is required when If-Match validation is enabled.");
        }

        var resourceName = string.IsNullOrWhiteSpace(definition.ResourceName) ? "resource" : definition.ResourceName.Trim();
        string? currentVersionToken = null;

        if (!string.IsNullOrWhiteSpace(definition.CurrentVersionSql))
        {
            currentVersionToken = await ExecuteScalarAsync<string>(definition.CurrentVersionSql, parameters, commandTimeout, cancellationToken);
            if (string.IsNullOrWhiteSpace(currentVersionToken))
            {
                throw ApplicationExceptions.Notfound(resourceName);
            }

            if (definition.RequireIfMatchHeader)
            {
                var request = _httpContextAccessor.HttpContext?.Request ?? throw new InvalidOperationException("No HttpContext found.");
                var currentEtag = OptimisticConcurrencyUtility.CreateETag(currentVersionToken);
                OptimisticConcurrencyUtility.RequireIfMatch(request, currentEtag, resourceName);
            }
        }

        var rowsAffected = await ExecuteAsync(definition.UpdateSql, parameters, commandTimeout, cancellationToken);
        if (rowsAffected > 0)
        {
            var updatedVersionToken = string.IsNullOrWhiteSpace(definition.CurrentVersionSql)
                ? null
                : await ExecuteScalarAsync<string>(definition.CurrentVersionSql, parameters, commandTimeout, cancellationToken);

            return new RepositoryOptimisticUpdateResult
            {
                RowsAffected = rowsAffected,
                CurrentVersionToken = updatedVersionToken,
                CurrentEtag = string.IsNullOrWhiteSpace(updatedVersionToken)
                    ? null
                    : OptimisticConcurrencyUtility.CreateETag(updatedVersionToken)
            };
        }

        var resourceExists = await ResourceExistsAsync(definition, parameters, commandTimeout, cancellationToken);
        if (!resourceExists)
        {
            throw ApplicationExceptions.Notfound(resourceName);
        }

        throw ApplicationExceptions.Conflict($"The {resourceName} was modified by another request.");
    }

    /// <summary>
    /// Executes a generic database action with resilience.
    /// Used by QueryAsync and QueryInterpolatedAsync.
    /// </summary>
    private Task<TResult> ExecuteWithResilienceAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
    {
        // 1. Create the Generic Policy for the specific result type
        var policy = DBResiliencePolicyFactory.CreateDbPolicy<TResult>(
            _policy.DatabaseProvider, 
            _policy.ResiliencePolicy);

        return policy.ExecuteAsync(token => action(token), cancellationToken);
    }

    /// <summary>
    /// Executes a database command (like ExecuteAsync or ExecuteStoredProcedureAsync) with resilience.
    /// </summary>
    private Task<int> ExecuteCommandWithResilienceAsync(Func<CancellationToken, Task<int>> action, CancellationToken cancellationToken)
    {
        // 2. Create the Non-Generic Policy specifically for 'int' results or void commands
        var policy = DBResiliencePolicyFactory.CreateDbCommandPolicy(
            _policy.DatabaseProvider, 
            _policy.ResiliencePolicy);

        // Note: IAsyncPolicy (non-generic) can execute a Task<int> via this overload
        return policy.ExecuteAsync(token => action(token), cancellationToken);
    }

    private async Task<bool> ResourceExistsAsync(RepositoryOptimisticUpdateDefinition definition, object? parameters, int? commandTimeout, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(definition.ExistsSql))
        {
            return await ExecuteScalarAsync<bool>(definition.ExistsSql, parameters, commandTimeout, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(definition.CurrentVersionSql))
        {
            var versionToken = await ExecuteScalarAsync<string>(definition.CurrentVersionSql, parameters, commandTimeout, cancellationToken);
            return !string.IsNullOrWhiteSpace(versionToken);
        }

        return true;
    }

    private DynamicParameters CreateDynamicParameters(object? parameters)
    {
        var dynamicParameters = new DynamicParameters();

        if (parameters is not null)
        {
            dynamicParameters.AddDynamicParams(parameters);
        }

        return dynamicParameters;
    }

    private List<string> BuildWhereFragments(RepositoryPagedQuery query, PagedRequest request, DynamicParameters parameters)
    {
        var fragments = new List<string>();

        var searchFragment = BuildSearchFragment(query, request, parameters);
        if (!string.IsNullOrWhiteSpace(searchFragment))
        {
            fragments.Add(searchFragment);
        }

        for (var index = 0; index < request.Filters.Count; index++)
        {
            var filter = request.Filters[index];
            if (string.IsNullOrWhiteSpace(filter.Field))
            {
                continue;
            }

            var column = ResolveMappedField(query.FilterColumns, filter.Field, "filter");
            fragments.Add(BuildFilterFragment(filter, column, index, parameters));
        }

        return fragments;
    }

    private string BuildSearchFragment(RepositoryPagedQuery query, PagedRequest request, DynamicParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm) || query.SearchColumns.Count == 0)
        {
            return string.Empty;
        }

        parameters.Add("__searchTerm", $"%{request.SearchTerm.Trim().ToLowerInvariant()}%");

        return "(" + string.Join(" OR ", query.SearchColumns.Select(column => $"LOWER({column}) LIKE @__searchTerm")) + ")";
    }

    private string BuildFilterFragment(FilterDescriptor filter, string column, int index, DynamicParameters parameters)
    {
        var parameterName = $"__filter{index}";

        return filter.Operator switch
        {
            FilterOperator.Equals => AddUnaryFilter(parameters, parameterName, column, "=", filter.Value),
            FilterOperator.NotEquals => AddUnaryFilter(parameters, parameterName, column, "<>", filter.Value),
            FilterOperator.Contains => AddLikeFilter(parameters, parameterName, column, filter.Value, prefix: "%", suffix: "%"),
            FilterOperator.StartsWith => AddLikeFilter(parameters, parameterName, column, filter.Value, prefix: string.Empty, suffix: "%"),
            FilterOperator.EndsWith => AddLikeFilter(parameters, parameterName, column, filter.Value, prefix: "%", suffix: string.Empty),
            FilterOperator.GreaterThan => AddUnaryFilter(parameters, parameterName, column, ">", filter.Value),
            FilterOperator.GreaterThanOrEqual => AddUnaryFilter(parameters, parameterName, column, ">=", filter.Value),
            FilterOperator.LessThan => AddUnaryFilter(parameters, parameterName, column, "<", filter.Value),
            FilterOperator.LessThanOrEqual => AddUnaryFilter(parameters, parameterName, column, "<=", filter.Value),
            FilterOperator.In => AddInFilter(parameters, parameterName, column, filter),
            FilterOperator.Between => AddBetweenFilter(parameters, parameterName, column, filter),
            _ => throw ApplicationExceptions.Badrequest($"Unsupported filter operator '{filter.Operator}'.")
        };
    }

    private static string AddUnaryFilter(DynamicParameters parameters, string parameterName, string column, string comparison, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw ApplicationExceptions.Badrequest($"A value is required for the '{comparison}' filter.");
        }

        parameters.Add(parameterName, value.Trim());
        return $"{column} {comparison} @{parameterName}";
    }

    private static string AddLikeFilter(DynamicParameters parameters, string parameterName, string column, string? value, string prefix, string suffix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw ApplicationExceptions.Badrequest("A text value is required for the supplied filter.");
        }

        parameters.Add(parameterName, $"{prefix}{value.Trim().ToLowerInvariant()}{suffix}");
        return $"LOWER({column}) LIKE @{parameterName}";
    }

    private static string AddInFilter(DynamicParameters parameters, string parameterName, string column, FilterDescriptor filter)
    {
        var values = filter.Values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray()
            ?? Array.Empty<string>();

        if (values.Length == 0)
        {
            throw ApplicationExceptions.Badrequest("At least one value is required for the 'In' filter.");
        }

        parameters.Add(parameterName, values);
        return $"{column} IN @{parameterName}";
    }

    private static string AddBetweenFilter(DynamicParameters parameters, string parameterName, string column, FilterDescriptor filter)
    {
        var values = filter.Values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Take(2).ToArray()
            ?? Array.Empty<string>();

        if (values.Length < 2)
        {
            throw ApplicationExceptions.Badrequest("Two values are required for the 'Between' filter.");
        }

        parameters.Add($"{parameterName}Start", values[0]);
        parameters.Add($"{parameterName}End", values[1]);
        return $"{column} BETWEEN @{parameterName}Start AND @{parameterName}End";
    }

    private string BuildOrderByClause(RepositoryPagedQuery query, PagedRequest request)
    {
        if (request.Sorts.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(query.DefaultOrderBy))
            {
                return _policy.DatabaseProvider == DatabaseProvider.MSSQL ? " ORDER BY (SELECT 1)" : string.Empty;
            }

            return NormalizeOrderByClause(query.DefaultOrderBy);
        }

        var segments = request.Sorts
            .Where(sort => !string.IsNullOrWhiteSpace(sort.Field))
            .Select(sort =>
            {
                var column = ResolveMappedField(query.SortColumns, sort.Field, "sort");
                var direction = sort.Direction == SortDirection.Descending ? "DESC" : "ASC";
                return $"{column} {direction}";
            })
            .ToArray();

        if (segments.Length == 0)
        {
            return _policy.DatabaseProvider == DatabaseProvider.MSSQL ? " ORDER BY (SELECT 1)" : string.Empty;
        }

        return " ORDER BY " + string.Join(", ", segments);
    }

    private string BuildPagingClause(string orderByClause)
    {
        return _policy.DatabaseProvider switch
        {
            DatabaseProvider.MSSQL when string.IsNullOrWhiteSpace(orderByClause) => " ORDER BY (SELECT 1) OFFSET @__pageOffset ROWS FETCH NEXT @__pageSize ROWS ONLY",
            DatabaseProvider.MSSQL => " OFFSET @__pageOffset ROWS FETCH NEXT @__pageSize ROWS ONLY",
            _ => " LIMIT @__pageSize OFFSET @__pageOffset"
        };
    }

    private static string AppendWhereClause(string sql, IReadOnlyList<string> fragments)
    {
        if (fragments.Count == 0)
        {
            return sql.TrimEnd();
        }

        var conjunction = ContainsWhereClause(sql) ? " AND " : " WHERE ";
        return sql.TrimEnd() + conjunction + string.Join(" AND ", fragments);
    }

    private static bool ContainsWhereClause(string sql)
    {
        var normalized = sql.ReplaceLineEndings(" ");
        return normalized.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase)
            || normalized.TrimStart().StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeOrderByClause(string orderByClause)
    {
        return orderByClause.TrimStart().StartsWith("ORDER BY ", StringComparison.OrdinalIgnoreCase)
            ? " " + orderByClause.Trim()
            : " ORDER BY " + orderByClause.Trim();
    }

    private static string ResolveMappedField(IReadOnlyDictionary<string, string> map, string field, string usage)
    {
        if (map.TryGetValue(field, out var expression) && !string.IsNullOrWhiteSpace(expression))
        {
            return expression;
        }

        foreach (var entry in map)
        {
            if (string.Equals(entry.Key, field, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(entry.Value))
            {
                return entry.Value;
            }
        }

        throw ApplicationExceptions.Badrequest($"The field '{field}' is not allowed for {usage} operations.");
    }
}
