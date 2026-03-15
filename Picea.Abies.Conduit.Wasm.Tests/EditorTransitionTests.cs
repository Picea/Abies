// =============================================================================
// Editor Transition Tests — Pure State Machine Verification
// =============================================================================
// Tests for the Editor page transitions in the Conduit MVU program.
// Verifies form input handling, tag management (add/remove/Enter key),
// article creation vs update dispatch, and API response processing.
// =============================================================================

using Picea.Abies.Conduit.App;

namespace Picea.Abies.Conduit.Wasm.Tests;

public class EditorTransitionTests
{
    private static readonly Session _testSession = new(
        Token: "test-token",
        Username: "testuser",
        Email: "test@example.com",
        Bio: "Test bio",
        Image: null);

    /// <summary>Creates a model with the Editor page for new or existing article.</summary>
    private static Model CreateEditorModel(string? slug = null) =>
        new(
            Page: new Page.Editor(new EditorModel(
                Slug: slug,
                Title: "",
                Description: "",
                Body: "",
                TagInput: "",
                TagList: [],
                Errors: [],
                IsSubmitting: false)),
            Session: _testSession,
            ApiUrl: "http://localhost:5000");

    [Test]
    public async Task TitleChanged_UpdatesTitleField()
    {
        var model = CreateEditorModel();
        var (newModel, command) = ConduitProgram.Transition(model, new EditorTitleChanged("New Title"));

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.Title).IsEqualTo("New Title");
        await Assert.That(command).IsEqualTo(Commands.None);
    }

    [Test]
    public async Task DescriptionChanged_UpdatesDescriptionField()
    {
        var model = CreateEditorModel();
        var (newModel, _) = ConduitProgram.Transition(model, new EditorDescriptionChanged("New description"));

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.Description).IsEqualTo("New description");
    }

    [Test]
    public async Task BodyChanged_UpdatesBodyField()
    {
        var model = CreateEditorModel();
        var (newModel, _) = ConduitProgram.Transition(model, new EditorBodyChanged("Article body content"));

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.Body).IsEqualTo("Article body content");
    }

    [Test]
    public async Task TagInputChanged_UpdatesTagInputField()
    {
        var model = CreateEditorModel();
        var (newModel, _) = ConduitProgram.Transition(model, new EditorTagInputChanged("newtag"));

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagInput).IsEqualTo("newtag");
    }

    [Test]
    public async Task AddTag_AppendsTagAndClearsInput()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withInput = model with
        {
            Page = new Page.Editor(editorPage.Data with { TagInput = "csharp" })
        };

        var (newModel, _) = ConduitProgram.Transition(withInput, new EditorAddTag());

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagList).Contains("csharp");
        await Assert.That(editor.Data.TagInput).IsEmpty();
    }

    [Test]
    public async Task AddTag_EmptyInput_DoesNotAddTag()
    {
        var model = CreateEditorModel();

        var (newModel, _) = ConduitProgram.Transition(model, new EditorAddTag());

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagList).IsEmpty();
    }

    [Test]
    public async Task AddTag_WhitespaceInput_DoesNotAddTag()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withWhitespace = model with
        {
            Page = new Page.Editor(editorPage.Data with { TagInput = "   " })
        };

        var (newModel, _) = ConduitProgram.Transition(withWhitespace, new EditorAddTag());

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagList).IsEmpty();
    }

    [Test]
    public async Task AddTag_DuplicateTag_DoesNotDuplicate()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withExistingTag = model with
        {
            Page = new Page.Editor(editorPage.Data with
            {
                TagList = ["csharp"],
                TagInput = "csharp"
            })
        };

        var (newModel, _) = ConduitProgram.Transition(withExistingTag, new EditorAddTag());

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagList).Count().IsEqualTo(1);
    }

    [Test]
    public async Task RemoveTag_RemovesSpecificTag()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withTags = model with
        {
            Page = new Page.Editor(editorPage.Data with
            {
                TagList = ["csharp", "dotnet", "blazor"]
            })
        };

        var (newModel, _) = ConduitProgram.Transition(withTags, new EditorRemoveTag("dotnet"));

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagList).IsEquivalentTo(new[] { "csharp", "blazor" });
    }

    [Test]
    public async Task TagKeyDown_EnterKey_AddsTagAndClearsInput()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withInput = model with
        {
            Page = new Page.Editor(editorPage.Data with { TagInput = "functional" })
        };

        var (newModel, _) = ConduitProgram.Transition(withInput, new EditorTagKeyDown("Enter"));

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagList).Contains("functional");
        await Assert.That(editor.Data.TagInput).IsEmpty();
    }

    [Test]
    public async Task TagKeyDown_NonEnterKey_DoesNotAddTag()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withInput = model with
        {
            Page = new Page.Editor(editorPage.Data with { TagInput = "partial" })
        };

        var (newModel, _) = ConduitProgram.Transition(withInput, new EditorTagKeyDown("a"));

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagList).IsEmpty();
        await Assert.That(editor.Data.TagInput).IsEqualTo("partial");
    }

    [Test]
    public async Task Submitted_NoSlug_SendsCreateArticleCommand()
    {
        var model = CreateEditorModel(slug: null);
        var editorPage = (Page.Editor)model.Page;
        var withContent = model with
        {
            Page = new Page.Editor(editorPage.Data with
            {
                Title = "My Article",
                Description = "About stuff",
                Body = "Content here",
                TagList = ["csharp", "dotnet"]
            })
        };

        var (newModel, command) = ConduitProgram.Transition(withContent, new EditorSubmitted());

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.IsSubmitting).IsTrue();
        await Assert.That(editor.Data.Errors).IsEmpty();

        var createCmd = await Assert.That(command).IsTypeOf<CreateArticle>();
        await Assert.That(createCmd!.Title).IsEqualTo("My Article");
        await Assert.That(createCmd.Description).IsEqualTo("About stuff");
        await Assert.That(createCmd.Body).IsEqualTo("Content here");
        await Assert.That(createCmd.TagList).IsEquivalentTo(new[] { "csharp", "dotnet" });
        await Assert.That(createCmd.Token).IsEqualTo(_testSession.Token);
    }

    [Test]
    public async Task Submitted_WithSlug_SendsUpdateArticleCommand()
    {
        var model = CreateEditorModel(slug: "existing-article");
        var editorPage = (Page.Editor)model.Page;
        var withContent = model with
        {
            Page = new Page.Editor(editorPage.Data with
            {
                Title = "Updated Title",
                Description = "Updated desc",
                Body = "Updated body",
                TagList = ["updated"]
            })
        };

        var (_, command) = ConduitProgram.Transition(withContent, new EditorSubmitted());

        var updateCmd = await Assert.That(command).IsTypeOf<UpdateArticle>();
        await Assert.That(updateCmd!.Slug).IsEqualTo("existing-article");
        await Assert.That(updateCmd.Title).IsEqualTo("Updated Title");
        await Assert.That(updateCmd.Description).IsEqualTo("Updated desc");
        await Assert.That(updateCmd.Body).IsEqualTo("Updated body");
        await Assert.That(updateCmd.TagList).IsEquivalentTo(new[] { "updated" });
    }

    [Test]
    public async Task ArticleSaved_NavigatesToArticlePage()
    {
        var model = CreateEditorModel();

        var (newModel, _) = ConduitProgram.Transition(model, new ArticleSaved("my-new-article"));

        await Assert.That(newModel.Page).IsTypeOf<Page.Article>();
    }

    [Test]
    public async Task ApiError_ShowsErrorsAndStopsSubmitting()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var submitting = model with
        {
            Page = new Page.Editor(editorPage.Data with { IsSubmitting = true })
        };

        var errors = new List<string> { "Title can't be blank", "Body can't be blank" };
        var (newModel, _) = ConduitProgram.Transition(submitting, new ApiError(errors));

        var editor = await Assert.That(newModel.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.Errors).IsEquivalentTo(errors);
        await Assert.That(editor.Data.IsSubmitting).IsFalse();
    }

    [Test]
    public async Task MultipleTags_AddedSequentially_AllPreserved()
    {
        var model = CreateEditorModel();
        var tags = new[] { "csharp", "dotnet", "blazor", "wasm" };

        var current = model;
        foreach (var tag in tags)
        {
            var editorPage = (Page.Editor)current.Page;
            current = current with
            {
                Page = new Page.Editor(editorPage.Data with { TagInput = tag })
            };
            (current, _) = ConduitProgram.Transition(current, new EditorAddTag());
        }

        var editor = await Assert.That(current.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor!.Data.TagList).IsEquivalentTo(tags);
    }
}
