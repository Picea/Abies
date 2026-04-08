namespace Picea.Abies.Testing;

/// <summary>
/// Guardrails used by <see cref="TestHarness{TProgram,TModel,TArgument}"/> to keep tests deterministic.
/// </summary>
/// <param name="MaxTransitions">Maximum number of state transitions allowed before failing.</param>
/// <param name="MaxDrainIterations">Maximum number of command-drain iterations before failing.</param>
public sealed record TestHarnessOptions(
    int MaxTransitions = 10_000,
    int MaxDrainIterations = 10_000)
{
    internal void Validate()
    {
        if (MaxTransitions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxTransitions), "MaxTransitions must be greater than zero.");
        }

        if (MaxDrainIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDrainIterations), "MaxDrainIterations must be greater than zero.");
        }
    }
}
