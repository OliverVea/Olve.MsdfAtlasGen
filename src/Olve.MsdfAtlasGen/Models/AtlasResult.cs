using System.Collections.Generic;

namespace Olve.MsdfAtlasGen.Models
{
    public class AtlasResult
    {
        public string ImagePath { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public float DistanceRange { get; set; }
        public FontMetrics Metrics { get; set; } = new();
        public List<GlyphInfo> Glyphs { get; set; } = [];
        public List<KerningPair> Kerning { get; set; } = [];
    }
}
