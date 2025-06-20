using Abies.Conduit.Services;
using System;
using System.Threading.Tasks;

namespace Abies.Conduit.Page.Profile
{
    public static class ProfileCommands
    {
        public static async Task<Message> LoadProfile(string username)
        {
            try
            {
                var profile = await ProfileService.GetProfileAsync(username);
                return new Message.ProfileLoaded(profile);
            }
            catch (Exception)
            {
                // Return default profile for error state
                return new Message.ProfileLoaded(new Home.Profile(username, "", "", false));
            }
        }
        
        public static async Task<Message> LoadUserArticles(string username, int offset = 0, int limit = 10)
        {
            try
            {
                var (articles, count) = await ArticleService.GetArticlesAsync(author: username, limit: limit, offset: offset);
                return new Message.ArticlesLoaded(articles, count);
            }
            catch (Exception)
            {
                // Return empty articles for error state
                return new Message.ArticlesLoaded(new System.Collections.Generic.List<Home.Article>(), 0);
            }
        }
        
        public static async Task<Message> LoadFavoritedArticles(string username, int offset = 0, int limit = 10)
        {
            try
            {
                var (articles, count) = await ArticleService.GetArticlesAsync(favoritedBy: username, limit: limit, offset: offset);
                return new Message.ArticlesLoaded(articles, count);
            }
            catch (Exception)
            {
                // Return empty articles for error state
                return new Message.ArticlesLoaded(new System.Collections.Generic.List<Home.Article>(), 0);
            }
        }
        
        public static async Task<Message> ToggleFollow(string username, bool currentState)
        {
            try
            {
                var profile = currentState
                    ? await ProfileService.UnfollowUserAsync(username)
                    : await ProfileService.FollowUserAsync(username);
                    
                return new Message.ProfileLoaded(profile);
            }
            catch (Exception)
            {
                // Return current profile state for error
                return new Message.ToggleFollow();
            }
        }
    }
}
