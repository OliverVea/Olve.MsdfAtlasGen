# Olve.MsdfAtlasGen

C# wrapper for [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen) with cross-platform native binaries included.

## Installation

```bash
dotnet add package Olve.MsdfAtlasGen
```

## Usage

```csharp
using Olve.MsdfAtlasGen;
using Olve.MsdfAtlasGen.Models;

var result = AtlasGenerator.Generate(new AtlasConfig
{
    FontPath = "Roboto-Regular.ttf",
    Type = AtlasType.MSDF,
    Dimensions = (1024, 1024),
    GlyphSize = 48,
    PixelRange = 4,
    OutputImagePath = "atlas.png",
    OutputJsonPath = "atlas.json"
});

Console.WriteLine($"Generated {result.Glyphs.Count} glyphs");
Console.WriteLine($"Line height: {result.Metrics.LineHeight}");
```

## Configuration Options

- **FontPath**: Path to the TTF/OTF font file (required)
- **Type**: Atlas type (HardMask, SoftMask, SDF, PSDF, MSDF, MTSDF) - default: MSDF
- **Dimensions**: Atlas texture dimensions (width, height) - default: (1024, 1024)
- **GlyphSize**: Size of each glyph in pixels - default: 48
- **PixelRange**: Distance field pixel range - default: 4
- **CharsetFile**: Path to a file containing the characters to include (optional)
- **OutputImagePath**: Path for the output PNG image (optional, uses temp path if not specified)
- **OutputJsonPath**: Path for the output JSON metadata (optional, uses temp path if not specified)

## What is MSDF?

Multi-channel signed distance fields (MSDF) is a technique for rendering sharp, scalable text in games and graphics applications. Unlike traditional bitmap fonts that look blurry when scaled, MSDF fonts maintain crisp edges at any size, making them perfect for games that need to support different resolutions.

## Credits

This is a wrapper around Viktor Chlumský's excellent [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen) tool.
All atlas generation is performed by the native msdf-atlas-gen binary.

## License

MIT License. See LICENSE file.

The msdf-atlas-gen tool is licensed under its own terms - see the [upstream repository](https://github.com/Chlumsky/msdf-atlas-gen).