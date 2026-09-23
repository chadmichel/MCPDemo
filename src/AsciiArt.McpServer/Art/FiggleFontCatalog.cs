using System.Reflection;
using Figgle;
using Figgle.Fonts;

namespace AsciiArt.McpServer.Art;

/// <summary>
/// Resolves Figgle fonts by name.
/// </summary>
/// <remarks>
/// Figgle exposes fonts two ways, and neither alone is enough:
/// <list type="bullet">
/// <item>265 static properties on <see cref="FiggleFonts"/>, named in PascalCase
/// ("Standard", "ThreeD", "Banner3D"). These we can enumerate, but not look up by string.</item>
/// <item><see cref="FiggleFonts.TryGetByName"/>, which takes the original FIGlet file name
/// ("standard", "3-d", "banner3-D", "ascii_new_roman"). That lookup is case-sensitive and the
/// names are inconsistently punctuated, so "Standard" and "banner3d" both miss.</item>
/// </list>
/// So we index the properties by name for case-insensitive matching and full enumeration, and
/// fall back to the file-name lookup for anyone passing a genuine FIGlet name like "3-d".
/// </remarks>
public sealed class FiggleFontCatalog
{
    public const string DefaultFont = "Standard";

    // Property name -> PropertyInfo. Deliberately NOT invoked here: reading all 265 would parse
    // every font up front. We invoke only the one that matches.
    private static readonly IReadOnlyDictionary<string, PropertyInfo> FontProperties =
        typeof(FiggleFonts)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(FiggleFont))
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every font name available, alphabetically.</summary>
    public IReadOnlyList<string> FontNames { get; } = [.. FontProperties.Values.Select(p => p.Name).Order(StringComparer.OrdinalIgnoreCase)];

    /// <summary>A short list of fonts that read well, for callers who don't want to scan all 265.</summary>
    public static IReadOnlyList<string> SuggestedFonts { get; } =
    [
        "Standard", "Big", "Slant", "Small", "Block", "Shadow", "Banner3D", "Doom",
        "Isometric1", "Larry3d", "Ogre", "Rounded", "Script", "Speed", "StarWars", "ThreeD"
    ];

    public int Count => FontNames.Count;

    /// <summary>
    /// Looks up a font by PascalCase property name (case-insensitive) or by its original
    /// FIGlet file name. Returns null when no font matches.
    /// </summary>
    public FiggleFont? Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();

        if (FontProperties.TryGetValue(trimmed, out var property))
        {
            return (FiggleFont?)property.GetValue(null);
        }

        // Fall back to the original FIGlet names, which are lowercase far more often than not.
        return FiggleFonts.TryGetByName(trimmed)
            ?? FiggleFonts.TryGetByName(trimmed.ToLowerInvariant());
    }
}
