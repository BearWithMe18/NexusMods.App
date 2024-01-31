using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Installers.DTO.Files;

/// <summary>
/// Denotes any file whose location does not change and is not generated by the game at runtime.
/// </summary>
public abstract record AStaticModFile : AModFile
{
    /// <summary>
    /// The hash of the file itself in question.
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// Length of the file in bytes.
    /// </summary>
    public required Size Size { get; init; }
}