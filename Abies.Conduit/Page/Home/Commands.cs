using Abies.Conduit.Services;
using System;
using System.Threading.Tasks;

namespace Abies.Conduit.Page.Home
{
    public static class HomeCommands
    {
        public static async Task<Message> LoadGlobalFeed(int offset = 0, int limit = 10)
        {
            try
            {
                var (articles, count) = await ArticleService.GetArticlesAsync(limit: limit, offset: offset);
                return new Message.ArticlesLoaded(articles, count);
            }
            catch (Exception)
            {
                // Return empty feed for error state
                return new Message.ArticlesLoaded(new System.Collections.Generic.List<Article>(), 0);
            }
        }
        
        public static async Task<Message> LoadUserFeed(int offset = 0, int limit = 10)
        {
            try
            {
                var (articles, count) = await ArticleService.GetFeedArticlesAsync(limit: limit, offset: offset);
                return new Message.ArticlesLoaded(articles, count);
            }
            catch (Exception)
            {
                // Return empty feed for error state
                return new Message.ArticlesLoaded(new System.Collections.Generic.List<Article>(), 0);
            }
        }
        
        public static async Task<Message> LoadTagFeed(string tag, int offset = 0, int limit = 10)
        {
            try
            {
                var (articles, count) = await ArticleService.GetArticlesAsync(tag: tag, limit: limit, offset: offset);
                return new Message.ArticlesLoaded(articles, count);
            }
            catch (Exception)
            {
                // Return empty feed for error state
                return new Message.ArticlesLoaded(new System.Collections.Generic.List<Article>(), 0);
            }
        }
        
        public static async Task<Message> LoadTags()
        {
            try
            {
                var tags = await TagService.GetTagsAsync();
                return new Message.TagsLoaded(tags);
            }
            catch (Exception)
            {
                // Return empty tags for error state
                return new Message.TagsLoaded(new System.Collections.Generic.List<string>());
            }
        }
        
        public static async Task<Message> ToggleFavorite(string slug, bool currentState)
        {
            try
            {
                await (currentState
                    ? ArticleService.UnfavoriteArticleAsync(slug)
                    : ArticleService.FavoriteArticleAsync(slug));
                    
                // Reload the feed after toggling favorite
                return await LoadGlobalFeed();
            }
            catch (Exception)
            {
                // Return empty feed for error state
                return new Message.ArticlesLoaded(new System.Collections.Generic.List<Article>(), 0);
            }
        }
    }
}
