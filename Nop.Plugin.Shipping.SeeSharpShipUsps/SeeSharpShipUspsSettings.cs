using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps {
    public class SeeSharpShipUspsSettings : ISettings {
        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public decimal AdditionalHandlingCharge { get; set; }

        public string ZipPostalCodeFrom { get; set; }

        public decimal MinimumShippingCharge { get; set; }

        public bool InsuranceEnabled { get; set; }

        public string BaseDomesticServicesSelected { get; set; }

        public string DomesticServicesSelected { get; set; }

        public string BaseInternationalServicesSelected { get; set; }

        public string InternationalServicesSelected { get; set; }
    }
}
