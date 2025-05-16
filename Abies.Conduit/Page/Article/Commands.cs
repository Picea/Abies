using Abies.Conduit.Services;
using System;
using System.Threading.Tasks;

namespace Abies.Conduit.Page.Article
{
    public static class ArticleCommands
    {
        public static async Task<Message> LoadArticle(string slug)
        {
            try
            {
                var article = await ArticleService.GetArticleAsync(slug);
                return new Message.ArticleLoaded(article);
            }
            catch (Exception)
            {
                // Return empty article for error state
                return new Message.ArticleLoaded(new Home.Article("", "", "", "", [], "", "", false, 0, new Home.Profile("", "", "", false)));
            }
        }
        
        public static async Task<Message> LoadComments(string slug)
        {
            try
            {
                var comments = await ArticleService.GetCommentsAsync(slug);
                return new Message.CommentsLoaded(comments);
            }
            catch (Exception)
            {
                // Return empty comments list for error state
                return new Message.CommentsLoaded(new System.Collections.Generic.List<Comment>());
            }
        }
        
        public static async Task<Message> SubmitComment(string slug, string body)
        {
            try
            {
                var comment = await ArticleService.AddCommentAsync(slug, body);
                return new Message.CommentSubmitted(comment);
            }
            catch (Exception)
            {
                // Return to non-submitting state on error
                return new Message.SubmitComment();
            }
        }
        
        public static async Task<Message> DeleteComment(string slug, string commentId)
        {
            try
            {
                await ArticleService.DeleteCommentAsync(slug, commentId);
                return new Message.CommentDeleted(commentId);
            }
            catch (Exception)
            {
                // Return empty id as error signal
                return new Message.CommentDeleted("");
            }
        }
        
        public static async Task<Message> ToggleFavorite(string slug, bool currentState)
        {
            try
            {
                var article = currentState
                    ? await ArticleService.UnfavoriteArticleAsync(slug)
                    : await ArticleService.FavoriteArticleAsync(slug);
                    
                return new Message.ArticleLoaded(article);
            }
            catch (Exception)
            {
                // Return current article as error signal
                return new Message.ToggleFavorite();
            }
        }
        
        public static async Task DeleteArticle(string slug)
        {
            try
            {
                await ArticleService.DeleteArticleAsync(slug);
            }
            catch (Exception)
            {
                // Swallow exception
            }
        }
    }
}
