namespace Olve.MsdfAtlasGen.Models
{
    public class AtlasConfig
    {
        public string FontPath { get; set; } = string.Empty;
        public AtlasType Type { get; set; } = AtlasType.MSDF;
        public (int Width, int Height) Dimensions { get; set; } = (1024, 1024);
        public int GlyphSize { get; set; } = 48;
        public int PixelRange { get; set; } = 4;
        public string? CharsetFile { get; set; }
        public string? OutputImagePath { get; set; }
        public string? OutputJsonPath { get; set; }
    }
}
