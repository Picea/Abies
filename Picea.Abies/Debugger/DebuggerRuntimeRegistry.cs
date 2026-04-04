// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if DEBUG

namespace Picea.Abies.Debugger;

/// <summary>
/// Global registry for message capture injection into the debugger.
/// This allows the Runtime and HandlerRegistry to wire capture hooks without direct dependencies.
/// </summary>
internal static class DebuggerRuntimeRegistry
{
    public static DebuggerMachine? CurrentDebugger { get; set; }
}

#endif
