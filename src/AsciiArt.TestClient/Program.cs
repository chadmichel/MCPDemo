using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

// Usage: dotnet run [text] [font] [serverUrl]
var text = args.Length > 0 ? args[0] : "Hello";
var font = args.Length > 1 ? args[1] : "Standard";
var serverUrl = args.Length > 2 ? args[2] : "http://localhost:5124/mcp";

Console.WriteLine($"Connecting to MCP server at {serverUrl} ...");

await using var transport = new HttpClientTransport(new HttpClientTransportOptions
{
    Name = "AsciiArt",
    Endpoint = new Uri(serverUrl)
});

await using var client = await McpClient.CreateAsync(transport);

Console.WriteLine($"Connected to: {client.ServerInfo.Name} v{client.ServerInfo.Version}");
Console.WriteLine();

var tools = await client.ListToolsAsync();
Console.WriteLine("Tools advertised by the server:");
foreach (var tool in tools)
{
    Console.WriteLine($"  - {tool.Name}: {tool.Description}");
}
Console.WriteLine();

Console.WriteLine($"Calling generate_ascii_art with text = \"{text}\", font = \"{font}\" ...");
Console.WriteLine();

var result = await client.CallToolAsync(
    "generate_ascii_art",
    new Dictionary<string, object?> { ["text"] = text, ["font"] = font });

if (result.IsError == true)
{
    Console.Error.WriteLine("The tool returned an error:");
    foreach (var block in result.Content.OfType<TextContentBlock>())
    {
        Console.Error.WriteLine(block.Text);
    }
    return 1;
}

// The tool returns its result as JSON. Pull out the "art" field so the banner prints with
// real newlines instead of escaped ones. Prefer structuredContent when the server sends it,
// and otherwise parse the text block, which always carries the same payload.
string? art = null;

if (result.StructuredContent is { } structured &&
    structured.TryGetProperty("art", out var structuredArt))
{
    art = structuredArt.GetString();
}
else
{
    var json = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
    if (json is not null)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("art", out var textArt))
            {
                art = textArt.GetString();
            }
        }
        catch (JsonException)
        {
            // Not JSON after all; fall through and print it raw.
        }
    }

    art ??= json;
}

Console.WriteLine(art);

return 0;
