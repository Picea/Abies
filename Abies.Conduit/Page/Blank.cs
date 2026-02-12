using Abies.Conduit.Main;
using Abies.DOM;

namespace Abies.Conduit.Page.Blank;

public interface Message : Abies.Message;

public record Model(Slug Slug, User? CurrentUser = null);

public class Page : Element<Model, Message>
{
    public static Model Initialize(Message argument)
    {
        throw new NotImplementedException();
    }

    public static Subscription Subscriptions(Model model)
    {
        return SubscriptionModule.None;
    }

    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
            _ => (model, Commands.None)
        };

    public static Node View(Model model)
         => text("");
}
