using Olve.MsdfAtlasGen;
using Olve.MsdfAtlasGen.Models;

Console.WriteLine("=== Olve.MsdfAtlasGen Demo ===\n");

// Find the font file (works from multiple locations)
var fontPath = "Roboto-Regular.ttf";
if (!File.Exists(fontPath))
{
    fontPath = "resources/Roboto-Regular.ttf";
}
if (!File.Exists(fontPath))
{
    fontPath = Path.Combine(AppContext.BaseDirectory, "Roboto-Regular.ttf");
}

// Generate atlas from the included Roboto font
var result = AtlasGenerator.Generate(new AtlasConfig
{
    FontPath = fontPath,
    Type = AtlasType.MSDF,
    Dimensions = (512, 512),
    GlyphSize = 64,
    PixelRange = 4,
    OutputImagePath = "output-atlas.png",
    OutputJsonPath = "output-atlas.json"
});

Console.WriteLine($"✓ Atlas generated successfully!");
Console.WriteLine($"  Image: {result.ImagePath}");
Console.WriteLine($"  Size: {result.Width}x{result.Height}");
Console.WriteLine($"  Glyphs: {result.Glyphs.Count}");
Console.WriteLine($"  Kerning pairs: {result.Kerning.Count}");
Console.WriteLine($"\nFont Metrics:");
Console.WriteLine($"  Line height: {result.Metrics.LineHeight:F2}");
Console.WriteLine($"  Ascender: {result.Metrics.Ascender:F2}");
Console.WriteLine($"  Descender: {result.Metrics.Descender:F2}");
Console.WriteLine($"\nSample glyphs:");
foreach (var glyph in result.Glyphs.Take(10))
{
    Console.WriteLine($"  '{glyph.Character}' (U+{glyph.Unicode:X4}) - advance: {glyph.Advance:F2}");
}