using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Olve.MsdfAtlasGen.Internal
{
    internal class AtlasJsonData
    {
        [JsonPropertyName("atlas")]
        public AtlasInfo Atlas { get; set; } = new();

        [JsonPropertyName("metrics")]
        public MetricsInfo Metrics { get; set; } = new();

        [JsonPropertyName("glyphs")]
        public List<GlyphData> Glyphs { get; set; } = [];

        [JsonPropertyName("kerning")]
        public List<KerningData> Kerning { get; set; } = [];
    }

    internal class AtlasInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("distanceRange")]
        public float DistanceRange { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    internal class MetricsInfo
    {
        [JsonPropertyName("emSize")]
        public float EmSize { get; set; }

        [JsonPropertyName("lineHeight")]
        public float LineHeight { get; set; }

        [JsonPropertyName("ascender")]
        public float Ascender { get; set; }

        [JsonPropertyName("descender")]
        public float Descender { get; set; }

        [JsonPropertyName("underlineY")]
        public float UnderlineY { get; set; }

        [JsonPropertyName("underlineThickness")]
        public float UnderlineThickness { get; set; }
    }

    internal class GlyphData
    {
        [JsonPropertyName("unicode")]
        public int Unicode { get; set; }

        [JsonPropertyName("advance")]
        public float Advance { get; set; }

        [JsonPropertyName("planeBounds")]
        public BoundsData? PlaneBounds { get; set; }

        [JsonPropertyName("atlasBounds")]
        public BoundsData? AtlasBounds { get; set; }
    }

    internal class BoundsData
    {
        [JsonPropertyName("left")]
        public float Left { get; set; }

        [JsonPropertyName("bottom")]
        public float Bottom { get; set; }

        [JsonPropertyName("right")]
        public float Right { get; set; }

        [JsonPropertyName("top")]
        public float Top { get; set; }
    }

    internal class KerningData
    {
        [JsonPropertyName("unicode1")]
        public int Unicode1 { get; set; }

        [JsonPropertyName("unicode2")]
        public int Unicode2 { get; set; }

        [JsonPropertyName("advance")]
        public float Advance { get; set; }
    }
}
