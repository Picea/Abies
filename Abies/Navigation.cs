namespace Abies;

public static class Navigation
{
    public interface Command : Abies.Command
    {
        public record struct Back(int times) : Command;

        public record struct Forward(int times) : Command;

        public record struct Go(int steps) : Command;

        public record struct Reload : Command;

        public record struct Load(Url Url) : Command;

        public record struct PushState(Url Url) : Command;

        public record struct ReplaceState(Url Url) : Command; 
    }

    internal static void Navigate(Command command)
    {
        switch (command)
        {
            case Command.Back back:
                Interop.Back(back.times);
                break;
            case Command.Forward forward:
                Interop.Forward(forward.times);
                break;
            case Command.Go go:
                Interop.Go(go.steps);
                break;
            case Command.Reload reload:
                Interop.Reload();
                break;
            case Command.Load load:
                Interop.Load(load.Url.ToString());
                break;
            case Command.PushState pushState:
                Interop.PushState(pushState.Url.ToString());
                break;
            case Command.ReplaceState replaceState:
                Interop.ReplaceState(replaceState.Url.ToString());
                break;
            default:
                throw new NotImplementedException();
        }
    }
}
