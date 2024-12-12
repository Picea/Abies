using System;

namespace Abies;

public interface Option<T>;

public readonly record struct Some<T>(T Value) : Option<T>;
public readonly struct None<T> : Option<T>;
