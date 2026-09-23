using AsciiArt.McpServer.Art;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<FiggleFontCatalog>();

// Expose this app as an MCP server over Streamable HTTP.
// WithToolsFromAssembly picks up every [McpServerToolType] in this assembly.
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "AsciiArt", Version = "1.0.0" };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// The MCP endpoint clients connect to: http://localhost:5124/mcp
app.MapMcp("/mcp");

// Browser demo page, served from wwwroot/demo.html. It is self-contained (inline CSS and JS)
// and drives the /fonts and /ascii endpoints below, so no static-file middleware is needed.
app.MapGet("/demo", (IWebHostEnvironment env) =>
        Results.File(Path.Combine(env.WebRootPath, "demo.html"), "text/html"))
    .WithName("GetDemoPage")
    .ExcludeFromDescription();

// Landing page, so hitting the root in a browser shows the server is alive
// and says where everything lives.
app.MapGet("/", (FiggleFontCatalog catalog) => Results.Ok(new
{
    service = "AsciiArt MCP Server",
    status = "running",
    fontsAvailable = catalog.Count,
    mcpEndpoint = "/mcp",
    tool = new
    {
        name = "generate_ascii_art",
        description = "Renders text as FIGlet ASCII art banner characters.",
        parameters = new
        {
            text = "string, required, up to 120 characters",
            font = $"string, optional, defaults to {FiggleFontCatalog.DefaultFont}"
        }
    },
    demoPage = "/demo",
    restEndpoints = new[] { "/fonts", "/ascii?text=Hello&font=Standard" }
}))
.WithName("GetServiceInfo")
.ExcludeFromDescription();

// Plain REST endpoints, handy for eyeballing output without an MCP client.
app.MapGet("/fonts", (FiggleFontCatalog catalog) => Results.Ok(new
{
    count = catalog.Count,
    suggested = FiggleFontCatalog.SuggestedFonts,
    all = catalog.FontNames
}))
.WithName("GetFonts");

app.MapGet("/ascii", (FiggleFontCatalog catalog, string text, string? font) =>
{
    if (string.IsNullOrWhiteSpace(text))
    {
        // text/plain throughout, so the /demo page can show errors verbatim
        // instead of a JSON-quoted string.
        return Results.Text("text is required.", "text/plain", null, 400);
    }

    var name = string.IsNullOrWhiteSpace(font) ? FiggleFontCatalog.DefaultFont : font.Trim();
    var figgleFont = catalog.Find(name);

    return figgleFont is null
        ? Results.Text($"Unknown font '{name}'. See /fonts.", "text/plain", null, 404)
        // text/plain so the art keeps its shape in a browser.
        : Results.Text(figgleFont.Render(text).TrimEnd(), "text/plain");
})
.WithName("GetAscii");

app.Run();
