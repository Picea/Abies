using Abies.Conduit.Services;
using System;
using System.Threading.Tasks;

namespace Abies.Conduit.Page.Settings
{
    public static class SettingsCommands
    {
        public static async Task<Message> UpdateSettings(string image, string username, string bio, string email, string password)
        {
            try
            {
                var user = await AuthService.UpdateUserAsync(username, email, bio, image, password);
                return new Message.SettingsSuccess(user);
            }
            catch (ApiException ex)
            {
                return new Message.SettingsError(ex.Errors);
            }
            catch (Exception)
            {
                var errors = new System.Collections.Generic.Dictionary<string, string[]>
                {
                    { "error", new[] { "An unexpected error occurred" } }
                };
                return new Message.SettingsError(errors);
            }
        }
        
        public static Message LogoutUser()
        {
            AuthService.Logout();
            return new Message.LogoutRequested();
        }
    }
}
