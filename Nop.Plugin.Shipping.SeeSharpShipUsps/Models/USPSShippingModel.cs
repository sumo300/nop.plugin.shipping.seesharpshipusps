using System.Collections.Generic;
using Nop.Web.Framework;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps.Models {
    public class USPSShippingModel {
        public USPSShippingModel() {
            BaseDomesticServices = new List<USPSSelectableService>();
            DomesticServices = new List<USPSSelectableService>();
            BaseInternationalServices = new List<USPSSelectableService>();
            InternationalServices = new List<USPSSelectableService>();
        }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.Url")]
        public string Url { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.Username")]
        public string Username { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.Password")]
        public string Password { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.AdditionalHandlingCharge")]
        public decimal AdditionalHandlingCharge { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.ZipPostalCodeFrom")]
        public string ZipPostalCodeFrom { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.MinimumShippingCharge")]
        public decimal MinimumShippingCharge { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.InsuranceEnabled")]
        public bool InsuranceEnabled { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseDomesticServices")]
        public IList<USPSSelectableService> BaseDomesticServices { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.DomesticServices")]
        public IList<USPSSelectableService> DomesticServices { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseInternationalServices")]
        public IList<USPSSelectableService> BaseInternationalServices { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.SeeSharpShipUsps.Fields.InternationalServices")]
        public IList<USPSSelectableService> InternationalServices { get; set; }

        public string[] CheckedBaseDomesticServices { get; set; }

        public string[] CheckedDomesticServices { get; set; }

        public string[] CheckedBaseInternationalServices { get; set; }

        public string[] CheckedInternationalServices { get; set; }

        public string PluginVersion { get; set; }
    }
}
