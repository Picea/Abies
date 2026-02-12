namespace Abies.Conduit.IntegrationTests.Testing;

internal static class ConduitApiFixtures
{
    public static object ArticlesResponse(int totalCount, params object[] articles)
        => new
        {
            articles,
            articlesCount = totalCount
        };

    public static object Article(
        string slug,
        string title,
        string description,
        string body,
        IEnumerable<string>? tagList = null,
        string createdAt = "2025-01-01T00:00:00.000Z",
        string updatedAt = "2025-01-01T00:00:00.000Z",
        bool favorited = false,
        int favoritesCount = 0,
        string authorUsername = "tester",
        bool authorFollowing = false,
        string authorBio = "",
        string authorImage = "")
        => new
        {
            slug,
            title,
            description,
            body,
            tagList = tagList is null ? (string[])[] : [.. tagList],
            createdAt,
            updatedAt,
            favorited,
            favoritesCount,
            author = new
            {
                username = authorUsername,
                bio = authorBio,
                image = authorImage,
                following = authorFollowing
            }
        };
}
