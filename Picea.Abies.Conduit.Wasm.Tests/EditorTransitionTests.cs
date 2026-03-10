// =============================================================================
// Editor Transition Tests — Pure State Machine Verification
// =============================================================================
// Tests for the Editor page transitions in the Conduit MVU program.
// Verifies form input handling, tag management (add/remove/Enter key),
// article creation vs update dispatch, and API response processing.
// =============================================================================

using Picea.Abies.Conduit.App;
using FluentAssertions;

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

    [Fact]
    public void TitleChanged_UpdatesTitleField()
    {
        var model = CreateEditorModel();
        var (newModel, command) = ConduitProgram.Transition(model, new EditorTitleChanged("New Title"));

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.Title.Should().Be("New Title");
        command.Should().Be(Commands.None);
    }

    [Fact]
    public void DescriptionChanged_UpdatesDescriptionField()
    {
        var model = CreateEditorModel();
        var (newModel, _) = ConduitProgram.Transition(model, new EditorDescriptionChanged("New description"));

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.Description.Should().Be("New description");
    }

    [Fact]
    public void BodyChanged_UpdatesBodyField()
    {
        var model = CreateEditorModel();
        var (newModel, _) = ConduitProgram.Transition(model, new EditorBodyChanged("Article body content"));

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.Body.Should().Be("Article body content");
    }

    [Fact]
    public void TagInputChanged_UpdatesTagInputField()
    {
        var model = CreateEditorModel();
        var (newModel, _) = ConduitProgram.Transition(model, new EditorTagInputChanged("newtag"));

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagInput.Should().Be("newtag");
    }

    [Fact]
    public void AddTag_AppendsTagAndClearsInput()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withInput = model with
        {
            Page = new Page.Editor(editorPage.Data with { TagInput = "csharp" })
        };

        var (newModel, _) = ConduitProgram.Transition(withInput, new EditorAddTag());

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagList.Should().Contain("csharp");
        editor.Data.TagInput.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_EmptyInput_DoesNotAddTag()
    {
        var model = CreateEditorModel();

        var (newModel, _) = ConduitProgram.Transition(model, new EditorAddTag());

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagList.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_WhitespaceInput_DoesNotAddTag()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withWhitespace = model with
        {
            Page = new Page.Editor(editorPage.Data with { TagInput = "   " })
        };

        var (newModel, _) = ConduitProgram.Transition(withWhitespace, new EditorAddTag());

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagList.Should().BeEmpty();
    }

    [Fact]
    public void AddTag_DuplicateTag_DoesNotDuplicate()
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

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagList.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTag_RemovesSpecificTag()
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

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagList.Should().BeEquivalentTo(["csharp", "blazor"]);
    }

    [Fact]
    public void TagKeyDown_EnterKey_AddsTagAndClearsInput()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withInput = model with
        {
            Page = new Page.Editor(editorPage.Data with { TagInput = "functional" })
        };

        var (newModel, _) = ConduitProgram.Transition(withInput, new EditorTagKeyDown("Enter"));

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagList.Should().Contain("functional");
        editor.Data.TagInput.Should().BeEmpty();
    }

    [Fact]
    public void TagKeyDown_NonEnterKey_DoesNotAddTag()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var withInput = model with
        {
            Page = new Page.Editor(editorPage.Data with { TagInput = "partial" })
        };

        var (newModel, _) = ConduitProgram.Transition(withInput, new EditorTagKeyDown("a"));

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagList.Should().BeEmpty();
        editor.Data.TagInput.Should().Be("partial");
    }

    [Fact]
    public void Submitted_NoSlug_SendsCreateArticleCommand()
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

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.IsSubmitting.Should().BeTrue();
        editor.Data.Errors.Should().BeEmpty();

        var createCmd = command.Should().BeOfType<CreateArticle>().Subject;
        createCmd.Title.Should().Be("My Article");
        createCmd.Description.Should().Be("About stuff");
        createCmd.Body.Should().Be("Content here");
        createCmd.TagList.Should().BeEquivalentTo(["csharp", "dotnet"]);
        createCmd.Token.Should().Be(_testSession.Token);
    }

    [Fact]
    public void Submitted_WithSlug_SendsUpdateArticleCommand()
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

        var updateCmd = command.Should().BeOfType<UpdateArticle>().Subject;
        updateCmd.Slug.Should().Be("existing-article");
        updateCmd.Title.Should().Be("Updated Title");
        updateCmd.Description.Should().Be("Updated desc");
        updateCmd.Body.Should().Be("Updated body");
        updateCmd.TagList.Should().BeEquivalentTo(["updated"]);
    }

    [Fact]
    public void ArticleSaved_NavigatesToArticlePage()
    {
        var model = CreateEditorModel();

        var (newModel, _) = ConduitProgram.Transition(model, new ArticleSaved("my-new-article"));

        newModel.Page.Should().BeOfType<Page.Article>();
    }

    [Fact]
    public void ApiError_ShowsErrorsAndStopsSubmitting()
    {
        var model = CreateEditorModel();
        var editorPage = (Page.Editor)model.Page;
        var submitting = model with
        {
            Page = new Page.Editor(editorPage.Data with { IsSubmitting = true })
        };

        var errors = new List<string> { "Title can't be blank", "Body can't be blank" };
        var (newModel, _) = ConduitProgram.Transition(submitting, new ApiError(errors));

        var editor = newModel.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.Errors.Should().BeEquivalentTo(errors);
        editor.Data.IsSubmitting.Should().BeFalse();
    }

    [Fact]
    public void MultipleTags_AddedSequentially_AllPreserved()
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

        var editor = current.Page.Should().BeOfType<Page.Editor>().Subject;
        editor.Data.TagList.Should().BeEquivalentTo(tags);
    }
}
