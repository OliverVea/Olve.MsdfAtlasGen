namespace Olve.MsdfAtlasGen.Models
{
    public class GlyphInfo
    {
        public char Character { get; set; }
        public int Unicode { get; set; }
        public float Advance { get; set; }
        public (float Left, float Bottom, float Right, float Top) PlaneBounds { get; set; }
        public (float Left, float Bottom, float Right, float Top) AtlasBounds { get; set; }
    }
}
