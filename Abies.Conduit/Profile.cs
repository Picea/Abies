using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.DOM;
using System.Collections.Generic;

namespace Abies.Conduit.Profile;

public interface Message : Abies.Message
{

}

public record Model(UserName UserName);

public class Page : Element<Model, Message>
{
    public static Model Initialize(Message argument)
    {
        throw new System.NotImplementedException();
    }

    public static Subscription Subscriptions(Model model)
    {
        throw new System.NotImplementedException();
    }

    public static (Model model, IEnumerable<Command> commands) Update(Abies.Message message, Model model)
        => message switch
        {
            _ => (model, [])
        };

    public static Node View(Model model)
         => h1([], [text($"Profile - {model.UserName}")]);
}