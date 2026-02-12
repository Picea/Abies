using Abies.Conduit.IntegrationTests.Testing;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class EditorDomJourneyTests
{
    [Fact]
    public void Editor_TypingTitleDescriptionBody_UpdatesModelAndDom()
    {
        // Arrange
        var model = Page.Editor.Page.Initialize(new Page.Editor.Message.NoOp());

        // Act
        var (m1, _) = MvuDomTestHarness.DispatchInput(
            model,
            Page.Editor.Page.View,
            Page.Editor.Page.Update,
            el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Article Title"),
            value: "My Title");

        var (m2, _) = MvuDomTestHarness.DispatchInput(
            m1,
            Page.Editor.Page.View,
            Page.Editor.Page.Update,
            el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "What's this article about?"),
            value: "Desc");

        var (m3, _) = MvuDomTestHarness.DispatchInput(
            m2,
            Page.Editor.Page.View,
            Page.Editor.Page.Update,
            el => el.Tag == "textarea" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Write your article (in markdown)"),
            value: "Body");

        // Assert model
        Assert.Equal("My Title", m3.Title);
        Assert.Equal("Desc", m3.Description);
        Assert.Equal("Body", m3.Body);

        // Assert DOM reflects model values
        var dom = Page.Editor.Page.View(m3);
        var titleInput = MvuDomTestHarness.FindFirstElement(dom,
            el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Article Title"));
        Assert.Contains(titleInput.Attributes, a => a.Name == "value" && a.Value == "My Title");
    }

    [Fact]
    public void Editor_ClickingRemoveTag_RemovesFromModel()
    {
        // Arrange: start with model containing a tag so the remove icon exists.
        var model = new Page.Editor.Model(TagList: ["one"], TagInput: "");

        // Act: click the remove icon (ion-close-round) which dispatches RemoveTag(tag)
        var (m1, _) = MvuDomTestHarness.DispatchClick(
            model,
            Page.Editor.Page.View,
            Page.Editor.Page.Update,
            el => el.Tag == "i" && el.Attributes.Any(a => a.Name == "class" && a.Value.Contains("ion-close-round")));

        // Assert
        Assert.NotNull(m1.TagList);
        Assert.DoesNotContain("one", m1.TagList!);
    }
}
