using System.ComponentModel;
using AsciiArt.McpServer.Art;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AsciiArt.McpServer.Tools;

[McpServerToolType]
public sealed class AsciiArtTools
{
    private const int MaxTextLength = 120;

    private readonly FiggleFontCatalog _catalog;

    public AsciiArtTools(FiggleFontCatalog catalog) => _catalog = catalog;

    [McpServerTool(Name = "generate_ascii_art")]
    [Description("Renders text as FIGlet ASCII art banner characters. Returns the art as a single multi-line string.")]
    public AsciiArtResult GenerateAsciiArt(
        [Description("The text to render as ASCII art.")]
        string text,
        [Description("Font name, e.g. Standard, Big, Slant, Block, Shadow, Doom, StarWars, Isometric1. Case-insensitive. Defaults to Standard.")]
        string font = FiggleFontCatalog.DefaultFont)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new McpException("text must not be empty.");
        }

        if (text.Length > MaxTextLength)
        {
            throw new McpException(
                $"text is {text.Length} characters; the limit is {MaxTextLength}. " +
                "Longer strings render hundreds of columns wide and are unreadable.");
        }

        var resolvedName = string.IsNullOrWhiteSpace(font) ? FiggleFontCatalog.DefaultFont : font.Trim();
        var figgleFont = _catalog.Find(resolvedName)
            ?? throw new McpException(
                $"Unknown font '{resolvedName}'. Try one of: {string.Join(", ", FiggleFontCatalog.SuggestedFonts)}. " +
                $"GET /fonts lists all {_catalog.Count}.");

        var art = figgleFont.Render(text).TrimEnd();
        var lines = art.Split('\n');

        return new AsciiArtResult(
            Text: text,
            Font: resolvedName,
            Art: art,
            Width: lines.Length == 0 ? 0 : lines.Max(l => l.TrimEnd().Length),
            Height: lines.Length);
    }
}

/// <param name="Text">The text that was rendered.</param>
/// <param name="Font">The font used.</param>
/// <param name="Art">The ASCII art, newline separated.</param>
/// <param name="Width">Width of the widest line, in characters.</param>
/// <param name="Height">Number of lines.</param>
public sealed record AsciiArtResult(string Text, string Font, string Art, int Width, int Height);
