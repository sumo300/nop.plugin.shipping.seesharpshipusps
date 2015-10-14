using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Nop.Plugin.Shipping.SeeSharpShipUsps.Models;
using Nop.Services.Configuration;
using Nop.Web.Framework.Controllers;
using SeeSharpShip.Models.Usps.Domestic;
using SeeSharpShip.Models.Usps.International.Request;
using SeeSharpShip.Services.Usps;
using SeeSharpShip.Utilities;
using System.Diagnostics;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps.Controllers {
    [AdminAuthorize]
    public class ShippingSeeSharpShipUspsController : BasePluginController {
        private readonly IRateService _rateService;
        private readonly ISettingService _settingService;
        private readonly SeeSharpShipUspsSettings _uspsSettings;

        public ShippingSeeSharpShipUspsController(SeeSharpShipUspsSettings uspsSettings, ISettingService settingService) {
            _uspsSettings = uspsSettings;
            _settingService = settingService;
            
            // ReSharper disable once CSharpWarnings::CS0618
            _rateService = _uspsSettings.Url == null ? new RateService() : new RateService(_uspsSettings.Url, new PostRequest());
        }

        [ChildActionOnly]
        public ActionResult Configure() {
            var model = new USPSShippingModel {
                Url = _uspsSettings.Url,
                Username = _uspsSettings.Username,
                Password = _uspsSettings.Password,
                AdditionalHandlingCharge = _uspsSettings.AdditionalHandlingCharge,
                ZipPostalCodeFrom = _uspsSettings.ZipPostalCodeFrom,
                MinimumShippingCharge = _uspsSettings.MinimumShippingCharge,
                InsuranceEnabled = _uspsSettings.InsuranceEnabled
            };

            LoadBaseDomesticServices(model);
            LoadBaseInternationalServices(model);

            // Need to have a valid USPS login here
            if (!string.IsNullOrWhiteSpace(_uspsSettings.Username) && !string.IsNullOrWhiteSpace(_uspsSettings.Password)) {
                try {
                    LoadDomesticServices(model);
                    LoadInternationalServices(model);
                } catch (Exception) {
                    ModelState.AddModelError(string.Empty, "Please enter valid USPS API credentials.");
                }
            }

            model.PluginVersion = GetPluginVersion();

            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Shipping.SeeSharpShipUsps/Views/ShippingSeeSharpShipUsps/Configure.cshtml", model);
        }

        private string GetPluginVersion() {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private void LoadBaseInternationalServices(USPSShippingModel model) {
            List<MailType> availableServices = EnumHelper.ToEnumerable<MailType>().ToList();
            foreach (MailType service in availableServices) {
                model.BaseInternationalServices.Add(new USPSSelectableService {
                    Id = (int) service,
                    Name = service.ToString()
                });
            }

            string enabledServicesCsv = _uspsSettings.BaseInternationalServicesSelected;
            if (string.IsNullOrWhiteSpace(enabledServicesCsv)) {
                return;
            }

            enabledServicesCsv = ClearSavedLegacyServices(enabledServicesCsv);
            SelectEnabledServices(enabledServicesCsv, model.BaseInternationalServices);
        }

        private void LoadInternationalServices(USPSShippingModel model) {
            IEnumerable<ServiceInfo> availableServices = _rateService.InternationalServices(_uspsSettings.Username, _uspsSettings.Password,
                _uspsSettings.ZipPostalCodeFrom);

            foreach (ServiceInfo service in availableServices) {
                model.InternationalServices.Add(new USPSSelectableService {
                    Id = int.Parse(service.Id),
                    Name = GetModifiedServiceName(service.FullName)
                });
            }

            string enabledServicesCsv = _uspsSettings.InternationalServicesSelected;
            if (string.IsNullOrWhiteSpace(enabledServicesCsv)) {
                return;
            }

            enabledServicesCsv = ClearSavedLegacyServices(enabledServicesCsv);
            SelectEnabledServices(enabledServicesCsv, model.InternationalServices);
        }

        private void LoadBaseDomesticServices(USPSShippingModel model) {
            List<ServiceTypes> availableServices = EnumHelper.ToEnumerable<ServiceTypes>().ToList();
            foreach (ServiceTypes service in availableServices) {
                model.BaseDomesticServices.Add(new USPSSelectableService {
                    Id = (int) service,
                    Name = service.ToString()
                });
            }

            string enabledServicesCsv = _uspsSettings.BaseDomesticServicesSelected;
            if (string.IsNullOrWhiteSpace(enabledServicesCsv)) {
                return;
            }

            enabledServicesCsv = ClearSavedLegacyServices(enabledServicesCsv);
            SelectEnabledServices(enabledServicesCsv, model.BaseDomesticServices);
        }

        private void LoadDomesticServices(USPSShippingModel model) {
            IEnumerable<ServiceInfo> availableServices = _rateService.DomesticServices(_uspsSettings.Username, _uspsSettings.Password,
                _uspsSettings.ZipPostalCodeFrom);

            foreach (ServiceInfo service in availableServices) {
                model.DomesticServices.Add(new USPSSelectableService {
                    Id = int.Parse(service.Id),
                    Name = GetModifiedServiceName(service.FullName)
                });
            }

            string enabledServicesCsv = _uspsSettings.DomesticServicesSelected;
            if (string.IsNullOrWhiteSpace(enabledServicesCsv)) {
                return;
            }

            enabledServicesCsv = ClearSavedLegacyServices(enabledServicesCsv);
            SelectEnabledServices(enabledServicesCsv, model.DomesticServices);
        }

        private static string GetModifiedServiceName(string service) {
            string serviceName = HttpUtility.HtmlDecode(service);
            const char reg = (char) 174;
            const char trade = (char) 8482;
            serviceName = serviceName.Replace("<sup>®</sup>", reg.ToString(CultureInfo.InvariantCulture));
            serviceName = serviceName.Replace("<sup>™</sup>", trade.ToString(CultureInfo.InvariantCulture));
            serviceName = serviceName.Replace(" 1-Day", string.Empty);
            serviceName = serviceName.Replace(" 2-Day", string.Empty);
            return serviceName;
        }

        private static void SelectEnabledServices(string enabledServicesCsv, IEnumerable<USPSSelectableService> availableServices) {
            enabledServicesCsv.Split(',')
                .Select(int.Parse)
                .Join(availableServices, enabled => enabled, available => available.Id, (enabled, available) => available)
                .ToList()
                .ForEach(s => s.IsChecked = true);
        }

        /// <summary>
        ///     Removes settings using the old method of storage
        /// </summary>
        private static string ClearSavedLegacyServices(string services) {
            if (services.Contains("[")) {
                services = string.Empty;
            }
            return services;
        }

        [HttpPost]
        [ChildActionOnly]
        public ActionResult Configure(USPSShippingModel model) {
            if (!ModelState.IsValid) {
                return Configure();
            }

            //save settings
            _uspsSettings.Url = model.Url;
            _uspsSettings.Username = model.Username;
            _uspsSettings.Password = model.Password;
            _uspsSettings.AdditionalHandlingCharge = model.AdditionalHandlingCharge;
            _uspsSettings.ZipPostalCodeFrom = model.ZipPostalCodeFrom;
            _uspsSettings.MinimumShippingCharge = model.MinimumShippingCharge;
            _uspsSettings.InsuranceEnabled = model.InsuranceEnabled;

            if (model.CheckedBaseDomesticServices != null) {
                _uspsSettings.BaseDomesticServicesSelected = model.CheckedBaseDomesticServices.Aggregate((a, b) => string.Format("{0},{1}", a, b));
            }

            if (model.CheckedDomesticServices != null) {
                _uspsSettings.DomesticServicesSelected = model.CheckedDomesticServices.Aggregate((a, b) => string.Format("{0},{1}", a, b));
            }

            if (model.CheckedBaseInternationalServices != null) {
                _uspsSettings.BaseInternationalServicesSelected = model.CheckedBaseInternationalServices.Aggregate((a, b) => string.Format("{0},{1}", a, b));
            }

            if (model.CheckedInternationalServices != null) {
                _uspsSettings.InternationalServicesSelected = model.CheckedInternationalServices.Aggregate((a, b) => string.Format("{0},{1}", a, b));
            }

            _settingService.SaveSetting(_uspsSettings);

            return Configure();
        }
    }
}
