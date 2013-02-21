namespace Nop.Plugin.Shipping.SeeSharpShipUsps.Domain
{
    public class USPSVolumetrics
    {
        public int Length { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public decimal Weight { get; set; }
    }
}