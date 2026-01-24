using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MedEdge.Dashboard.Models;
using System.Security.Cryptography;
using System.Text;

namespace MedEdge.Dashboard.Services;

/// <summary>
/// Implementation of authentication service with environment-based credential validation
/// Credentials are loaded from injected environment variables via JavaScript
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private const string SessionKey = "mededge_auth_session";

    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;
    private bool _isAuthenticated;
    private string? _username;
    private string? _loadedUsername;
    private string? _loadedPasswordHash;

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            if (_isAuthenticated != value)
            {
                _isAuthenticated = value;
                OnAuthenticationChanged();
            }
        }
    }

    public string? Username
    {
        get => _username;
        private set
        {
            _username = value;
            OnAuthenticationChanged();
        }
    }

    public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationChanged;

    public AuthenticationService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;

        // Load authentication state asynchronously in OnInitializedAsync equivalent
        _ = LoadAuthenticationStateAsync();
    }

    private async Task LoadCredentialsAsync()
    {
        try
        {
            // Get credentials from injected environment configuration via JavaScript
            var credentials = await _jsRuntime.InvokeAsync<CredentialsInfo>("MedEdgeAuth.getCredentials");
            if (credentials != null && credentials.Username != null)
            {
                _loadedUsername = credentials.Username;
                // Use JS to hash the password for comparison
                _loadedPasswordHash = await _jsRuntime.InvokeAsync<string>("MedEdgeAuth.hashPassword", credentials.Password ?? string.Empty);
            }
            else
            {
                // No credentials configured - authentication will fail
                _loadedUsername = null;
                _loadedPasswordHash = null;
            }
        }
        catch
        {
            // No credentials available - authentication will fail
            _loadedUsername = null;
            _loadedPasswordHash = null;
        }
    }

    public async Task<AuthenticationResult> LoginAsync(string username, string password)
    {
        await Task.Delay(500); // Simulate network delay for better UX

        // Ensure credentials are loaded
        if (_loadedUsername == null)
        {
            await LoadCredentialsAsync();
        }

        // Validate credentials
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Username and password are required"
            };
        }

        // Use JavaScript to validate credentials (matching the JS implementation)
        try
        {
            var validationResult = await _jsRuntime.InvokeAsync<ValidationResult>("MedEdgeAuth.validateCredentials", username, password);

            if (validationResult != null && validationResult.Valid)
            {
                // Successful login
                Username = validationResult.Username ?? username;
                IsAuthenticated = true;

                // Save session via JavaScript
                var token = GenerateToken(username);
                await _jsRuntime.InvokeVoidAsync("MedEdgeAuth.saveSession", Username, token);

                return new AuthenticationResult
                {
                    Success = true,
                    Token = token
                };
            }
        }
        catch
        {
            // Fallback to C# validation
            var passwordHash = HashPassword(password);
            if (username.Equals(_loadedUsername, StringComparison.OrdinalIgnoreCase) &&
                passwordHash == _loadedPasswordHash)
            {
                Username = username;
                IsAuthenticated = true;
                var token = GenerateToken(username);
                return new AuthenticationResult
                {
                    Success = true,
                    Token = token
                };
            }
        }

        return new AuthenticationResult
        {
            Success = false,
            ErrorMessage = "Invalid username or password"
        };
    }

    public async Task LogoutAsync()
    {
        // Clear session via JavaScript
        try
        {
            await _jsRuntime.InvokeVoidAsync("MedEdgeAuth.clearSession");
        }
        catch { }

        Username = null;
        IsAuthenticated = false;
        _navigationManager.NavigateTo("/login", forceLoad: true);
    }

    private string HashPassword(string password)
    {
        // Simple hash for demonstration (in production, use proper password hashing)
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + "MedEdge_Salt_2024");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).Substring(0, 32);
    }

    private string GenerateToken(string username)
    {
        // Generate a simple session token
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var data = $"{username}:{timestamp}:MedEdge";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    private async Task LoadAuthenticationStateAsync()
    {
        try
        {
            var session = await _jsRuntime.InvokeAsync<SessionInfo>("MedEdgeAuth.loadSession");
            if (session != null && !string.IsNullOrEmpty(session.Username))
            {
                Username = session.Username;
                IsAuthenticated = true;
            }
        }
        catch
        {
            IsAuthenticated = false;
            Username = null;
        }
    }

    private void OnAuthenticationChanged()
    {
        AuthenticationChanged?.Invoke(this, new AuthenticationStateChangedEventArgs
        {
            IsAuthenticated = IsAuthenticated,
            Username = Username
        });
    }

    // Classes for JavaScript interop
    private class CredentialsInfo
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    private class ValidationResult
    {
        public bool Valid { get; set; }
        public string? Username { get; set; }
    }

    private class SessionInfo
    {
        public string? Username { get; set; }
        public string? Token { get; set; }
    }
}
