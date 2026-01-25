using MedEdge.Dashboard.Models;

namespace MedEdge.Dashboard.Services;

/// <summary>
/// Service for managing user authentication
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    Task<AuthenticationResult> LoginAsync(string username, string password);

    /// <summary>
    /// Logs out the current user
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Checks if a user is currently authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current authenticated username
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Event raised when authentication state changes
    /// </summary>
    event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationChanged;
}

/// <summary>
/// Authentication result
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Token { get; set; }
}

/// <summary>
/// Event arguments for authentication state changes
/// </summary>
public class AuthenticationStateChangedEventArgs : EventArgs
{
    public bool IsAuthenticated { get; set; }
    public string? Username { get; set; }
}
