using Abies.Conduit.Main;
using Abies.Conduit.Services;
using System.Threading.Tasks;

namespace Abies.Conduit.Services;

public static class AuthService
{
    private static User? _currentUser;

    public static User? GetCurrentUser() => _currentUser;

    public static async Task<User> LoginAsync(string email, string password)
    {
        var response = await ApiClient.LoginAsync(email, password);
        
        _currentUser = new User(
            new UserName(response.User.Username),
            new Email(response.User.Email),
            new Token(response.User.Token)
        );
        
        ApiClient.SetAuthToken(response.User.Token);
        
        return _currentUser;
    }

    public static async Task<User> RegisterAsync(string username, string email, string password)
    {
        var response = await ApiClient.RegisterAsync(username, email, password);
        
        _currentUser = new User(
            new UserName(response.User.Username),
            new Email(response.User.Email),
            new Token(response.User.Token)
        );
        
        ApiClient.SetAuthToken(response.User.Token);
        
        return _currentUser;
    }

    public static async Task<User?> LoadUserFromTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }
        
        try
        {
            ApiClient.SetAuthToken(token);
            var response = await ApiClient.GetCurrentUserAsync();
            
            _currentUser = new User(
                new UserName(response.User.Username),
                new Email(response.User.Email),
                new Token(response.User.Token)
            );
            
            return _currentUser;
        }
        catch
        {
            ApiClient.SetAuthToken(null);
            return null;
        }
    }

    public static void Logout()
    {
        _currentUser = null;
        ApiClient.SetAuthToken(null);
    }

public static async Task<User> UpdateUserAsync(string username, string email, string bio, string image, string? password = null)
    {
        var response = await ApiClient.UpdateUserAsync(email, username, bio, image, password);
        
        _currentUser = new User(
            new UserName(response.User.Username),
            new Email(response.User.Email),
            new Token(response.User.Token)
        );
        
        ApiClient.SetAuthToken(response.User.Token);
        
        return _currentUser;
    }
}
