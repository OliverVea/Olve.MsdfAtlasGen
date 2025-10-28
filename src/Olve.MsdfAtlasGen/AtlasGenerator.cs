using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Olve.MsdfAtlasGen.Internal;
using Olve.MsdfAtlasGen.Models;

namespace Olve.MsdfAtlasGen
{
    public static class AtlasGenerator
    {
        public static AtlasResult Generate(AtlasConfig config)
        {
            if (string.IsNullOrEmpty(config.FontPath))
                throw new ArgumentException("FontPath is required", nameof(config));

            if (!File.Exists(config.FontPath))
                throw new FileNotFoundException($"Font file not found: {config.FontPath}");

            // Locate native binary
            var binaryPath = NativeBinaryLocator.GetBinaryPath();

            // Generate temp paths if not specified
            var outputImage = config.OutputImagePath ?? Path.Combine(Path.GetTempPath(), $"atlas_{Guid.NewGuid()}.png");
            var outputJson = config.OutputJsonPath ?? Path.Combine(Path.GetTempPath(), $"atlas_{Guid.NewGuid()}.json");

            // Build command-line arguments
            var args = BuildArguments(config, outputImage, outputJson);

            // Execute msdf-atlas-gen
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = binaryPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"msdf-atlas-gen failed with exit code {process.ExitCode}: {error}");
            }

            // Parse JSON output
            if (!File.Exists(outputJson))
            {
                throw new FileNotFoundException($"Output JSON not generated: {outputJson}");
            }

            var json = File.ReadAllText(outputJson);
            var data = JsonSerializer.Deserialize<AtlasJsonData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data == null)
            {
                throw new Exception("Failed to parse atlas JSON data");
            }

            // Convert to result model
            return new AtlasResult
            {
                ImagePath = outputImage,
                Width = data.Atlas.Width,
                Height = data.Atlas.Height,
                DistanceRange = data.Atlas.DistanceRange,
                Metrics = new FontMetrics
                {
                    EmSize = data.Metrics.EmSize,
                    LineHeight = data.Metrics.LineHeight,
                    Ascender = data.Metrics.Ascender,
                    Descender = data.Metrics.Descender,
                    UnderlineY = data.Metrics.UnderlineY,
                    UnderlineThickness = data.Metrics.UnderlineThickness
                },
                Glyphs = data.Glyphs.Select(g => new GlyphInfo
                {
                    Character = (char)g.Unicode,
                    Unicode = g.Unicode,
                    Advance = g.Advance,
                    PlaneBounds = g.PlaneBounds != null
                        ? (g.PlaneBounds.Left, g.PlaneBounds.Bottom, g.PlaneBounds.Right, g.PlaneBounds.Top)
                        : (0, 0, 0, 0),
                    AtlasBounds = g.AtlasBounds != null
                        ? (g.AtlasBounds.Left, g.AtlasBounds.Bottom, g.AtlasBounds.Right, g.AtlasBounds.Top)
                        : (0, 0, 0, 0)
                }).ToList(),
                Kerning = data.Kerning.Select(k => new KerningPair
                {
                    First = (char)k.Unicode1,
                    Second = (char)k.Unicode2,
                    Advance = k.Advance
                }).ToList()
            };
        }

        private static string BuildArguments(AtlasConfig config, string outputImage, string outputJson)
        {
            var sb = new StringBuilder();

            sb.Append($"-font \"{config.FontPath}\" ");
            sb.Append($"-type {config.Type.ToString().ToLower()} ");
            sb.Append($"-dimensions {config.Dimensions.Width} {config.Dimensions.Height} ");
            sb.Append($"-size {config.GlyphSize} ");
            sb.Append($"-pxrange {config.PixelRange} ");
            sb.Append($"-format png ");
            sb.Append($"-imageout \"{outputImage}\" ");
            sb.Append($"-json \"{outputJson}\" ");

            if (!string.IsNullOrEmpty(config.CharsetFile))
            {
                sb.Append($"-charset \"{config.CharsetFile}\" ");
            }

            return sb.ToString();
        }
    }
}
