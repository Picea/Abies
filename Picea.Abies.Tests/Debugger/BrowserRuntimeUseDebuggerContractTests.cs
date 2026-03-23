using System.Reflection;

namespace Picea.Abies.Tests.Debugger;

public sealed class BrowserRuntimeUseDebuggerContractTests
{
    [Test]
    public async Task BrowserRuntimeRun_ExposesUseDebuggerOptIn()
    {
        var browserRuntimeType = GetBrowserRuntimeType();

        var runMethod = browserRuntimeType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "Run" && m.IsGenericMethodDefinition);

        if (runMethod is null)
        {
            throw new InvalidOperationException("Picea.Abies.Browser.Runtime.Run generic entrypoint was not found.");
        }

        var hasUseDebuggerBoolean = runMethod.GetParameters()
            .Any(parameter => parameter.Name == "useDebugger" && parameter.ParameterType == typeof(bool));

        if (!hasUseDebuggerBoolean)
        {
            throw new InvalidOperationException(
                "Missing debugger opt-in on browser runtime surface. Expected Run<TProgram,TModel,TArgument>(..., bool useDebugger = false)."
            );
        }

        await Assert.That(hasUseDebuggerBoolean).IsTrue();
    }

    [Test]
    public async Task BrowserRuntimeOptions_DefinesUseDebuggerSwitch()
    {
        var browserRuntimeType = GetBrowserRuntimeType();
        var optionsType = browserRuntimeType.Assembly.GetType("Picea.Abies.Browser.BrowserRuntimeOptions", throwOnError: false);

        if (optionsType is null)
        {
            throw new InvalidOperationException(
                "Missing BrowserRuntimeOptions type. Expected public type Picea.Abies.Browser.BrowserRuntimeOptions with bool UseDebugger { get; init; }."
            );
        }

        var useDebuggerProperty = optionsType.GetProperty("UseDebugger", BindingFlags.Public | BindingFlags.Instance);
        var hasExpectedUseDebuggerProperty = useDebuggerProperty is not null && useDebuggerProperty.PropertyType == typeof(bool);

        await Assert.That(hasExpectedUseDebuggerProperty).IsTrue();
    }

    private static Type GetBrowserRuntimeType()
    {
        var browserAssembly = System.Reflection.Assembly.Load("Picea.Abies.Browser");
        return browserAssembly.GetType("Picea.Abies.Browser.Runtime", throwOnError: true)!;
    }
}
