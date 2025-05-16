using Abies.Conduit.Services;
using System;
using System.Threading.Tasks;

namespace Abies.Conduit.Page.Register
{
    public static class RegisterCommands
    {
        public static async Task<Message> SubmitRegistration(string username, string email, string password)
        {
            try
            {
                var user = await AuthService.RegisterAsync(username, email, password);
                return new Message.RegisterSuccess(user);
            }
            catch (ApiException ex)
            {
                return new Message.RegisterError(ex.Errors);
            }
            catch (Exception)
            {
                var errors = new System.Collections.Generic.Dictionary<string, string[]>
                {
                    { "error", new[] { "An unexpected error occurred" } }
                };
                return new Message.RegisterError(errors);
            }
        }
    }
}
