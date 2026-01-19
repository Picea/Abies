using System.Linq;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.DOM;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class AuthDomJourneyTests
{
    [Fact]
    public void Login_TypingEmailAndPassword_UpdatesModel()
    {
        // Arrange
    var model = Abies.Conduit.Page.Login.Page.Initialize(new Abies.Conduit.Page.Login.Message.EmailChanged(""));

        // Act
        var (m1, _) = MvuDomTestHarness.DispatchInput(
            model,
            Abies.Conduit.Page.Login.Page.View,
            Abies.Conduit.Page.Login.Page.Update,
            elementPredicate: el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Email"),
            value: "a@b.com");

        var (m2, _) = MvuDomTestHarness.DispatchInput(
            m1,
            Abies.Conduit.Page.Login.Page.View,
            Abies.Conduit.Page.Login.Page.Update,
            elementPredicate: el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Password"),
            value: "secret");

        // Assert
        Assert.Equal("a@b.com", m2.Email);
        Assert.Equal("secret", m2.Password);

        // Also ensure the DOM reflects the values
        var dom = Abies.Conduit.Page.Login.Page.View(m2);
        var emailInput = MvuDomTestHarness.FindFirstElement(dom,
            el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Email"));

        Assert.Contains(emailInput.Attributes, a => a.Name == "value" && a.Value == "a@b.com");
    }

    [Fact]
    public void Register_TypingFields_UpdatesModel()
    {
        // Arrange
    var model = Abies.Conduit.Page.Register.Page.Initialize(new Abies.Conduit.Page.Register.Message.UsernameChanged(""));

        // Act
        var (m1, _) = MvuDomTestHarness.DispatchInput(
            model,
            Abies.Conduit.Page.Register.Page.View,
            Abies.Conduit.Page.Register.Page.Update,
            elementPredicate: el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Username"),
            value: "alice");

        var (m2, _) = MvuDomTestHarness.DispatchInput(
            m1,
            Abies.Conduit.Page.Register.Page.View,
            Abies.Conduit.Page.Register.Page.Update,
            elementPredicate: el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Email"),
            value: "alice@example.com");

        var (m3, _) = MvuDomTestHarness.DispatchInput(
            m2,
            Abies.Conduit.Page.Register.Page.View,
            Abies.Conduit.Page.Register.Page.Update,
            elementPredicate: el => el.Tag == "input" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Password"),
            value: "pw");

        // Assert
        Assert.Equal("alice", m3.Username);
        Assert.Equal("alice@example.com", m3.Email);
        Assert.Equal("pw", m3.Password);
    }
}
