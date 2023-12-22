using Radius;

namespace CaptivePortal
{
    public class RadiusAttributeParserService
    {
        public RadiusAttributeParser Parser { get; set; } = new();

        public RadiusAttributeParserService()
        {
            Parser.AddDefault();
        }
    }
}
