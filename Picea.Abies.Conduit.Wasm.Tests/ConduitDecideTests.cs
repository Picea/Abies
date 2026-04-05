using Picea.Abies.Conduit.App;

namespace Picea.Abies.Conduit.Wasm.Tests;

public class ConduitDecideTests
{
    private static readonly Session _session = new(
        Token: "test-token",
        Username: "tester",
        Email: "tester@example.com",
        Bio: "bio",
        Image: null);

    [Test]
    public async Task Decide_LoginSubmitted_OnNonLoginPage_ReturnsErrApiError()
    {
        var model = new Model(
            Page: new Page.Home(new HomeModel(FeedTab.Global, null, [], 0, 1, [], false)),
            Session: null,
            ApiUrl: "http://localhost:5000");

        var result = ConduitProgram.Decide(model, new LoginSubmitted());

        await Assert.That(result.IsErr).IsTrue();
        var error = await Assert.That(result.Error).IsTypeOf<ApiError>();
        await Assert.That(error!.Errors).Contains("Login form is not active.");
    }

    [Test]
    public async Task Decide_LoginSubmitted_WithValidLoginState_ReturnsOkEvent()
    {
        var model = new Model(
            Page: new Page.Login(new LoginModel("user@example.com", "hunter2", [], false)),
            Session: null,
            ApiUrl: "http://localhost:5000");

        var result = ConduitProgram.Decide(model, new LoginSubmitted());

        await Assert.That(result.IsErr).IsFalse();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        await Assert.That(result.Value[0]).IsTypeOf<LoginSubmitted>();
    }

    [Test]
    public async Task Decide_EditorAddTag_WithDuplicateTag_ReturnsErrApiError()
    {
        var model = new Model(
            Page: new Page.Editor(new EditorModel(
                Slug: null,
                Title: "Title",
                Description: "Description",
                Body: "Body",
                TagInput: "csharp",
                TagList: ["csharp"],
                Errors: [],
                IsSubmitting: false)),
            Session: _session,
            ApiUrl: "http://localhost:5000");

        var result = ConduitProgram.Decide(model, new EditorAddTag());

        await Assert.That(result.IsErr).IsTrue();
        var error = await Assert.That(result.Error).IsTypeOf<ApiError>();
        await Assert.That(error!.Errors).Contains("Tag must be non-empty and unique.");
    }
}
