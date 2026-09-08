namespace Picea.Abies.Tests;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SpecAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}
