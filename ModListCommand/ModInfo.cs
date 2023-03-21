﻿using StardewModdingAPI;
using System.Collections.Immutable;

namespace ModListCommand;

public sealed record ModInfo
{
    public required string Name { get; init; }
    public required ISemanticVersion Version { get; init; }
    public required string Author { get; init; }
    public required string Description { get; init; }
    public required ImmutableList<string> UpdateUrls { get; init; }
}
