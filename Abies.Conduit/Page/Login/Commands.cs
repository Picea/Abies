using Abies.Conduit.Services;
using System;
using System.Threading.Tasks;

namespace Abies.Conduit.Page.Login
{
    public static class LoginCommands
    {
        public static async Task<Message> SubmitLogin(string email, string password)
        {
            try
            {
                var user = await AuthService.LoginAsync(email, password);
                return new Message.LoginSuccess(user);
            }
            catch (ApiException ex)
            {
                string[] errors = { "Invalid email or password" };
                return new Message.LoginError(errors);
            }
            catch (Exception)
            {
                string[] errors = { "An unexpected error occurred" };
                return new Message.LoginError(errors);
            }
        }
    }
}
