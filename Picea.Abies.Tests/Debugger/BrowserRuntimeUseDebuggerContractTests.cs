using System.Reflection;

namespace Picea.Abies.Tests.Debugger;

public sealed class BrowserRuntimeUseDebuggerContractTests
{
    [Test]
    public async Task BrowserRuntimeRun_DoesNotExposeLegacyUseDebuggerParameter()
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

        if (hasUseDebuggerBoolean)
        {
            throw new InvalidOperationException(
                "Legacy browser debugger opt-in still exists. Expected debugger enablement to flow only through DebuggerConfiguration.ConfigureDebugger(...)."
            );
        }

        await Assert.That(hasUseDebuggerBoolean).IsFalse();
    }

    [Test]
    public async Task BrowserRuntime_DoesNotDefineLegacyBrowserRuntimeOptionsType()
    {
        var browserRuntimeType = GetBrowserRuntimeType();
        var optionsType = browserRuntimeType.Assembly.GetType("Picea.Abies.Browser.BrowserRuntimeOptions", throwOnError: false);

        if (optionsType is not null)
        {
            throw new InvalidOperationException(
                "Legacy BrowserRuntimeOptions type still exists. Expected debugger configuration to stay on the core DebuggerConfiguration API."
            );
        }

        await Assert.That(optionsType).IsNull();
    }

    [Test]
    public async Task DebuggerConfiguration_ExposesCurrentApi_WithEnabledDefaultInDebugBuild()
    {
        var abiesAssembly = System.Reflection.Assembly.Load("Picea.Abies");
        var debuggerConfigurationType = abiesAssembly.GetType("Picea.Abies.Debugger.DebuggerConfiguration", throwOnError: false);

        if (debuggerConfigurationType is null)
        {
            throw new InvalidOperationException(
                "DebuggerConfiguration type is missing from the Debug build. Expected Picea.Abies.Debugger.DebuggerConfiguration to be available."
            );
        }

        var configureMethod = debuggerConfigurationType.GetMethod(
            "ConfigureDebugger",
            BindingFlags.Public | BindingFlags.Static);
        var defaultProperty = debuggerConfigurationType.GetProperty(
            "Default",
            BindingFlags.Public | BindingFlags.Static);

        if (configureMethod is null || defaultProperty is null)
        {
            throw new InvalidOperationException(
                "DebuggerConfiguration is missing its mainline API surface. Expected Default and ConfigureDebugger(...)."
            );
        }

        var configureParameter = configureMethod.GetParameters().SingleOrDefault();
        var defaultOptions = defaultProperty.GetValue(null);
        var enabledProperty = defaultOptions?.GetType().GetProperty("Enabled", BindingFlags.Public | BindingFlags.Instance);
        var enabled = enabledProperty?.GetValue(defaultOptions) is true;

        await Assert.That(configureParameter?.ParameterType.FullName).IsEqualTo("Picea.Abies.Debugger.DebuggerOptions");
        await Assert.That(enabled).IsTrue();
    }

    private static Type GetBrowserRuntimeType()
    {
        var browserAssembly = System.Reflection.Assembly.Load("Picea.Abies.Browser");
        return browserAssembly.GetType("Picea.Abies.Browser.Runtime", throwOnError: true)!;
    }
}
