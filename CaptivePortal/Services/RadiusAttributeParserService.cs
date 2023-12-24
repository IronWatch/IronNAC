using Radius;

namespace CaptivePortal.Services
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
