using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for building and sending outbound HTTP requests.
/// </summary>
public static class HttpRequestUtility
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = JsonUtility.CreateDefaultOptions();


    // ─────────────── Client Factory ───────────────

    /// <summary>
    /// Creates a named <see cref="HttpClient"/> from the factory.
    /// inherits the configured base address, timeout, default headers, and Polly resilience policy.
    /// </summary>
    /// <param name="factory">The HTTP client factory.</param>
    /// <param name="name">The registered client name.</param>
    /// <returns>A configured <see cref="HttpClient"/> instance.</returns>
    public static HttpClient CreateClient(IHttpClientFactory factory, string name)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return factory.CreateClient(name);
    }

    // ─────────────── Request Builders ───────────────

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> for the given method and URL.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <returns>A configured <see cref="HttpRequestMessage"/>.</returns>
    public static HttpRequestMessage CreateRequest(HttpMethod method, string url, Dictionary<string, string>? headers = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var request = new HttpRequestMessage(method, url);
        ApplyHeaders(request, headers);
        return request;
    }

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> with a JSON body.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="body">The object to serialize as JSON content.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>A configured <see cref="HttpRequestMessage"/> with JSON content.</returns>
    public static HttpRequestMessage CreateJsonRequest(HttpMethod method, string url, object body, Dictionary<string, string>? headers = null, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(body);

        var request = new HttpRequestMessage(method, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, options ?? DefaultJsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        ApplyHeaders(request, headers);
        return request;
    }

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> with form URL-encoded content.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="formData">The form fields.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <returns>A configured <see cref="HttpRequestMessage"/> with form content.</returns>
    public static HttpRequestMessage CreateFormRequest(HttpMethod method, string url, Dictionary<string, string> formData, Dictionary<string, string>? headers = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(formData);

        var request = new HttpRequestMessage(method, url)
        {
            Content = new FormUrlEncodedContent(formData)
        };

        ApplyHeaders(request, headers);
        return request;
    }

    // ─────────────── Send Methods ───────────────

    /// <summary>
    /// Sends an <see cref="HttpRequestMessage"/> and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends a GET request and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public static Task<HttpResponseMessage> GetAsync(HttpClient client, string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, url, headers);
        return SendAsync(client, request, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with a JSON body and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="body">The object to serialize as JSON content.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public static Task<HttpResponseMessage> PostAsync(HttpClient client, string url, object body, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = CreateJsonRequest(HttpMethod.Post, url, body, headers);
        return SendAsync(client, request, cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with a JSON body and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="body">The object to serialize as JSON content.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public static Task<HttpResponseMessage> PutAsync(HttpClient client, string url, object body, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = CreateJsonRequest(HttpMethod.Put, url, body, headers);
        return SendAsync(client, request, cancellationToken);
    }

    /// <summary>
    /// Sends a PATCH request with a JSON body and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="body">The object to serialize as JSON content.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public static Task<HttpResponseMessage> PatchAsync(HttpClient client, string url, object body, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = CreateJsonRequest(HttpMethod.Patch, url, body, headers);
        return SendAsync(client, request, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Delete, url, headers);
        return SendAsync(client, request, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with form URL-encoded content and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="formData">The form fields.</param>
    /// <param name="headers">Optional headers to attach.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public static Task<HttpResponseMessage> PostFormAsync(HttpClient client, string url, Dictionary<string, string> formData, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = CreateFormRequest(HttpMethod.Post, url, formData, headers);
        return SendAsync(client, request, cancellationToken);
    }

    // ─────────────── Response Readers ───────────────

    /// <summary>
    /// Reads the response body as a string.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response body as a string.</returns>
    public static Task<string> ReadContentAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Reads the response body and deserializes it as JSON.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response body.</returns>
    public static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, options ?? DefaultJsonOptions, cancellationToken);
    }

    /// <summary>
    /// Reads the response body as a byte array.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response body as a byte array.</returns>
    public static Task<byte[]> ReadBytesAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Reads the response body as a stream.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response body as a stream.</returns>
    public static Task<Stream> ReadStreamAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the response status code indicates success (2xx).
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns><see langword="true"/> when the status code is in the 200–299 range.</returns>
    public static bool IsSuccess(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Gets the integer status code from the response.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>The HTTP status code as an integer.</returns>
    public static int GetStatusCode(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return (int)response.StatusCode;
    }

    /// <summary>
    /// Gets a response header value.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="headerName">The header name.</param>
    /// <returns>The header value when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetResponseHeader(HttpResponseMessage response, string headerName)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

        if (response.Headers.TryGetValues(headerName, out var values))
        {
            return string.Join(", ", values);
        }

        if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
        {
            return string.Join(", ", contentValues);
        }

        return null;
    }

    // ─────────────── Header Helpers ───────────────

    /// <summary>
    /// Sets the Authorization header with a Bearer token on the request.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="token">The bearer token.</param>
    public static void SetBearerToken(HttpRequestMessage request, string token)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // ─────────────── Private Helpers ───────────────

    private static void ApplyHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers is null) return;

        foreach (var (key, value) in headers)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;

            if (!request.Headers.TryAddWithoutValidation(key, value))
            {
                request.Content?.Headers.TryAddWithoutValidation(key, value);
            }
        }
    }
}