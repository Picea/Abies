using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Abies;
using Abies.DOM;

namespace Abies
{
    public interface Element<TModel, in TArgument>
    {
        public static abstract Node View(TModel model);
        public static abstract (TModel model, Command command) Update(Message message, TModel model);
        public static abstract TModel Initialize(TArgument argument);
        public static abstract Subscription Subscriptions(TModel model);
        
    }

    public interface Program<TModel, in TArgument> 
    {
        public static abstract (TModel, Command) Initialize(Url url, TArgument argument);
        public static abstract (TModel model, Command command) Update(Message message, TModel model);
        public static abstract Document View(TModel model);
        public static abstract Message OnUrlChanged(Url url);
        public static abstract Message OnLinkClicked(UrlRequest urlRequest);
        public static abstract Subscription Subscriptions(TModel model);
        
        public static abstract Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch);
    }

    public interface UrlRequest : Message
    {
        public sealed record Internal(Url Url) : UrlRequest;
        public sealed record External(string Url) : UrlRequest;
    }


    public record Subscription
    {

    }

    public interface Message;

    public interface Command
    {
        public record struct None : Command;
        public record struct Batch(IEnumerable<Command> Commands) : Command;
                
    }

    public static class Commands
    {
        public static Command.None None = new(); 
        public static Command.Batch Batch(IEnumerable<Command> commands) => new(commands);
    }

    
}

   

namespace Abies.DOM
{
    

    // Operations class moved to a dedicated file for clarity
}