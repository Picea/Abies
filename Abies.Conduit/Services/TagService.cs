namespace Abies.Conduit.Services;

public static class TagService
{
    public static async Task<List<string>> GetTagsAsync()
    {
        var response = await ApiClient.GetTagsAsync();
        return response.Tags;
    }
}
