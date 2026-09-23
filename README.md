# ASCII Art MCP Server

A .NET 10 MCP server that renders text as FIGlet ASCII art, using
[Figgle](https://github.com/drewnoakes/figgle).

| Project | What it is |
| --- | --- |
| [src/AsciiArt.McpServer](src/AsciiArt.McpServer) | ASP.NET Core Web API hosting the MCP server over HTTP |
| [src/AsciiArt.TestClient](src/AsciiArt.TestClient) | Console app that calls the tool over MCP |

## Run it

```bash
dotnet run --project src/AsciiArt.McpServer --launch-profile http
```

Listens on `http://localhost:5124`. Open **http://localhost:5124/demo** to try fonts
in a browser, or hit the API directly:

```bash
curl "http://localhost:5124/ascii?text=Hello&font=Slant"
```

## Try the MCP tool

With the server running, in a second terminal:

```bash
dotnet run --project src/AsciiArt.TestClient -- "Hello" "Slant"
```

```
Connected to: AsciiArt v1.0.0

Tools advertised by the server:
  - generate_ascii_art: Renders text as FIGlet ASCII art banner characters. Returns the art as a single multi-line string.

Calling generate_ascii_art with text = "Hello", font = "Slant" ...

    __  __     ____    
   / / / /__  / / /___ 
  / /_/ / _ \/ / / __ \
 / __  /  __/ / / /_/ /
/_/ /_/\___/_/_/\____/
```

## Add it to Claude Code

The server must be running first — Claude Code health-checks it on connect.

```bash
claude mcp add --transport http ascii-art http://localhost:5124/mcp
```

Then ask Claude something like *"write HELLO in ASCII art using the Big font"* and it
will call `generate_ascii_art`.

Useful follow-ups:

```bash
claude mcp list              # see it and whether it connected
claude mcp get ascii-art     # details and health check
claude mcp remove ascii-art  # unregister
```

Notes:

- `--transport http` matters. Without it the CLI assumes a stdio server and tries to run
  the URL as a command.
- Scope defaults to `local` (this project, just you). Use `-s project` to commit it to
  `.mcp.json` for the team, or `-s user` for all your projects.
- Re-registering is only needed if the URL or name changes, not when you rebuild.
- Inside Claude Code, `/mcp` shows connection status.

## The tool

`generate_ascii_art`:

| Parameter | Type | Default | Notes |
| --- | --- | --- | --- |
| `text` | string | *(required)* | Max 120 characters. |
| `font` | string | `Standard` | Case-insensitive. 265 available. |

Returns `{ text, font, art, width, height }`.

Font names work in either style — Figgle's PascalCase (`StarWars`, `Banner3D`, `ThreeD`)
or the original FIGlet file names (`starwars`, `banner3-D`, `3-d`). `GET /fonts` lists
them all. Good ones to start with: `Standard`, `Big`, `Slant`, `Block`, `Shadow`, `Doom`,
`Isometric1`, `StarWars`.

## Endpoints

| Route | Purpose |
| --- | --- |
| `/mcp` | MCP endpoint. POST-based JSON-RPC — a browser will not show art here. |
| `/demo` | Interactive page. Linkable: `/demo?text=Hello&font=Slant` |
| `/ascii?text=Hi&font=Big` | Art as `text/plain` |
| `/fonts` | All 265 font names |
| `/` | Service status |
