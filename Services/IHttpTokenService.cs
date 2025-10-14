using System.Text.Json;

namespace Gestus.Services;

/// <summary>
/// Interface para abstração das chamadas HTTP relacionadas a tokens
/// </summary>
public interface IHttpTokenService
{
    /// <summary>
    /// Autentica usuário via endpoint de token
    /// </summary>
    Task<HttpTokenResponse> AuthenticateAsync(string baseUrl, string username, string password);

    /// <summary>
    /// Renova token usando refresh token
    /// </summary>
    Task<HttpTokenResponse> RefreshTokenAsync(string baseUrl, string refreshToken);

    /// <summary>
    /// Faz introspecção de um token
    /// </summary>
    Task<HttpIntrospectionResponse> IntrospectTokenAsync(string baseUrl, string token);
}

/// <summary>
/// Resposta de operações com tokens
/// </summary>
public class HttpTokenResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorContent { get; set; }
    public JsonElement? TokenData { get; set; }
}

/// <summary>
/// Resposta de introspecção de token
/// </summary>
public class HttpIntrospectionResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorContent { get; set; }
    public JsonElement? IntrospectionData { get; set; }
}

/// <summary>
/// Implementação real das chamadas HTTP para tokens
/// </summary>
public class HttpTokenService : IHttpTokenService
{
    private readonly HttpClient _httpClient;

    public HttpTokenService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpTokenResponse> AuthenticateAsync(string baseUrl, string username, string password)
    {
        var tokenEndpoint = $"{baseUrl}/connect/token";
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("client_id", "gestus_api"),
            new KeyValuePair<string, string>("client_secret", "gestus_api_secret_2024"),
            new KeyValuePair<string, string>("scope", "openid profile email roles offline_access")
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, tokenRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return new HttpTokenResponse 
            { 
                IsSuccess = false, 
                ErrorContent = errorContent 
            };
        }

        var tokenContent = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        
        return new HttpTokenResponse 
        { 
            IsSuccess = true, 
            TokenData = tokenData 
        };
    }

    public async Task<HttpTokenResponse> RefreshTokenAsync(string baseUrl, string refreshToken)
    {
        var tokenEndpoint = $"{baseUrl}/connect/token";
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", "gestus_api"),
            new KeyValuePair<string, string>("client_secret", "gestus_api_secret_2024"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, tokenRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return new HttpTokenResponse 
            { 
                IsSuccess = false, 
                ErrorContent = errorContent 
            };
        }

        var tokenContent = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        
        return new HttpTokenResponse 
        { 
            IsSuccess = true, 
            TokenData = tokenData 
        };
    }

    public async Task<HttpIntrospectionResponse> IntrospectTokenAsync(string baseUrl, string token)
    {
        var introspectionEndpoint = $"{baseUrl}/connect/introspect";
        var introspectionRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", token),
            new KeyValuePair<string, string>("token_type_hint", "refresh_token"),
            new KeyValuePair<string, string>("client_id", "gestus_api"),
            new KeyValuePair<string, string>("client_secret", "gestus_api_secret_2024")
        });

        var response = await _httpClient.PostAsync(introspectionEndpoint, introspectionRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return new HttpIntrospectionResponse 
            { 
                IsSuccess = false, 
                ErrorContent = errorContent 
            };
        }

        var introspectionContent = await response.Content.ReadAsStringAsync();
        var introspectionData = JsonSerializer.Deserialize<JsonElement>(introspectionContent);
        
        return new HttpIntrospectionResponse 
        { 
            IsSuccess = true, 
            IntrospectionData = introspectionData 
        };
    }
}