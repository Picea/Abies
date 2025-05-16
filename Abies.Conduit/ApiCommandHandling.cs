using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abies;

namespace Abies.Conduit
{
    /// <summary>
    /// Base interface for API commands
    /// </summary>
    public interface IApiCommand
    {
        Task ExecuteAsync();
    }

    /// <summary>
    /// Represents a command that can be executed to interact with the API without expecting a specific result.
    /// </summary>
    public abstract class ApiCommand : Command, IApiCommand
    {
        /// <summary>
        /// Executes the API command asynchronously.
        /// </summary>
        /// <returns>A task that completes when the command has been executed.</returns>
        public abstract Task ExecuteAsync();
    }

    /// <summary>
    /// Represents a command that can be executed to interact with the API and returns a specific result.
    /// </summary>
    /// <typeparam name="TResult">The type of message returned by the command execution.</typeparam>
    public abstract class ApiCommand<TResult> : Command, IApiCommand where TResult : Message
    {
        /// <summary>
        /// Executes the API command asynchronously.
        /// </summary>
        /// <returns>A task that resolves to the message result.</returns>
        public abstract Task<TResult> ExecuteAsync();

        /// <summary>
        /// Implementation of the IApiCommand interface.
        /// </summary>
        async Task IApiCommand.ExecuteAsync()
        {
            await ExecuteAsync();
        }
    }
    
    /// <summary>
    /// Static extension methods for handling API commands.
    /// </summary>
    public static class ApiCommandExtensions
    {
        /// <summary>
        /// Handles API commands during the update cycle.
        /// </summary>
        /// <param name="commands">The collection of commands to process.</param>
        /// <returns>The same commands for proper chaining with the Abies framework.</returns>
        public static IEnumerable<Command> HandleApiCommands(this IEnumerable<Command> commands)
        {
            foreach (var command in commands)
            {
                if (command is IApiCommand apiCommand)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await apiCommand.ExecuteAsync();
                        }
                        catch (Exception)
                        {
                            // Exception handling is done in the command itself
                        }
                    });
                }
            }
            
            // Return the original commands so they can be processed by the Abies framework
            return commands;
        }
    }
    
    /// <summary>
    /// Runtime helper to support dispatching messages from API commands.
    /// </summary>
    public static class RuntimeDispatcher
    {
        private static readonly System.Reflection.MethodInfo? _dispatchMethod;
        private static readonly object? _program;
        
        static RuntimeDispatcher()
        {
            try
            {
                var programField = typeof(Runtime).GetField("_program", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Static);
                
                _program = programField?.GetValue(null);
                
                if (_program != null)
                {
                    // Get the Dispatch method that takes a Message parameter
                    _dispatchMethod = _program.GetType().GetMethod("Dispatch", 
                        new[] { typeof(Message) });
                }
            }
            catch (Exception)
            {
                // Silently fail
            }
        }
        
        /// <summary>
        /// Dispatches a message to the Abies runtime.
        /// </summary>
        /// <param name="message">The message to dispatch.</param>
        public static async Task Dispatch(Message message)
        {
            if (_dispatchMethod != null && _program != null)
            {
                var task = (Task)_dispatchMethod.Invoke(_program, new object[] { message })!;
                if (task != null)
                {
                    await task;
                }
            }
            else
            {
                Console.WriteLine("Cannot dispatch message: Runtime not properly initialized");
            }
        }
    }
}
