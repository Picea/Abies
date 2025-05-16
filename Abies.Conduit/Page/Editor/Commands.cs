using Abies.Conduit.Services;
using System;
using System.Threading.Tasks;

namespace Abies.Conduit.Page.Editor
{
    public static class EditorCommands
    {
        public static async Task<Message> CreateArticle(string title, string description, string body, System.Collections.Generic.List<string> tagList)
        {
            try
            {
                var article = await ArticleService.CreateArticleAsync(title, description, body, tagList);
                return new Message.ArticleSubmitSuccess(article.Slug);
            }
            catch (ApiException ex)
            {
                return new Message.ArticleSubmitError(ex.Errors);
            }
            catch (Exception)
            {
                var errors = new System.Collections.Generic.Dictionary<string, string[]>
                {
                    { "error", new[] { "An unexpected error occurred" } }
                };
                return new Message.ArticleSubmitError(errors);
            }
        }
        
        public static async Task<Message> UpdateArticle(string slug, string title, string description, string body)
        {
            try
            {
                var article = await ArticleService.UpdateArticleAsync(slug, title, description, body);
                return new Message.ArticleSubmitSuccess(article.Slug);
            }
            catch (ApiException ex)
            {
                return new Message.ArticleSubmitError(ex.Errors);
            }
            catch (Exception)
            {
                var errors = new System.Collections.Generic.Dictionary<string, string[]>
                {
                    { "error", new[] { "An unexpected error occurred" } }
                };
                return new Message.ArticleSubmitError(errors);
            }
        }
        
        public static async Task<Message> LoadArticle(string slug)
        {
            try
            {
                var article = await ArticleService.GetArticleAsync(slug);
                return new Message.ArticleLoaded(article);
            }
            catch (Exception)
            {
                // Return to base state on error
                return new Message.ArticleSubmitError(new System.Collections.Generic.Dictionary<string, string[]>
                {
                    { "error", new[] { "Failed to load article" } }
                });
            }
        }
    }
}
