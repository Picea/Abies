namespace Picea.Abies.Conduit.App.Views;

internal static class Avatar
{
    private const string DefaultAvatarUrl = "https://static.productionready.io/images/smiley-cyrus.jpg";

    public static string Url(string? image) =>
        string.IsNullOrWhiteSpace(image) ? DefaultAvatarUrl : image;
}
