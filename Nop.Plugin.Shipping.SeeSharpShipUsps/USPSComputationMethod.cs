//------------------------------------------------------------------------------
// Contributor(s): RJH 08/07/2009, mb 10/20/2010, AC 05/16/2011, MJS 01/27/2013
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Routing;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Plugin.Shipping.SeeSharpShipUsps.Domain;
using Nop.Plugin.Shipping.SeeSharpShipUsps.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;
using SeeSharpShip.Extensions;
using SeeSharpShip.Models.Usps;
using SeeSharpShip.Models.Usps.Domestic;
using SeeSharpShip.Models.Usps.Domestic.Request;
using SeeSharpShip.Models.Usps.Domestic.Response;
using SeeSharpShip.Models.Usps.International.Request;
using SeeSharpShip.Models.Usps.International.Response;
using SeeSharpShip.Services.Usps;
using Package = SeeSharpShip.Models.Usps.Domestic.Response.Package;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps {
    public class USPSComputationMethod : BasePlugin, IShippingRateComputationMethod {
        private readonly ILogger _loggerService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IRateService _rateService;
        private readonly ISettingService _settingService;
        private readonly IShippingService _shippingService;
        private readonly USPSPackageSplitterService _uspsPackageSplitter;
        private readonly SeeSharpShipUspsSettings _uspsSettings;
        private readonly USPSVolumetricsService _uspsVolumetricsService;

        public USPSComputationMethod(IMeasureService measureService, ISettingService settingService, IShippingService shippingService,
            SeeSharpShipUspsSettings uspsSettings,
            IPriceCalculationService priceCalculationService, MeasureSettings measureSettings, ILogger logger) {
            _uspsVolumetricsService = new USPSVolumetricsService(measureService, shippingService, measureSettings);
            _settingService = settingService;
            _shippingService = shippingService;
            _uspsSettings = uspsSettings;
            _priceCalculationService = priceCalculationService;
            _loggerService = logger;
            _uspsPackageSplitter = new USPSPackageSplitterService(measureService, shippingService, measureSettings);
            
            // ReSharper disable once CSharpWarnings::CS0618
            _rateService = string.IsNullOrWhiteSpace(_uspsSettings.Url) ? new RateService() : new RateService(_uspsSettings.Url, new PostRequest());
        }

        #region IShippingRateComputationMethod Members

        /// <summary>
        ///     Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest) {
            if (getShippingOptionRequest == null) {
                throw new ArgumentNullException(nameof(getShippingOptionRequest));
            }

            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null || getShippingOptionRequest.Items.Count == 0) {
                response.AddError("No shipment items");
                return response;
            }

            if (getShippingOptionRequest.ShippingAddress == null) {
                response.AddError("Shipping address is not set");
                return response;
            }

            if (string.IsNullOrEmpty(getShippingOptionRequest.ZipPostalCodeFrom)) {
                getShippingOptionRequest.ZipPostalCodeFrom = _uspsSettings.ZipPostalCodeFrom;
            }

            return GetShippingOptionsImpl(getShippingOptionRequest, IsDomesticRequest(getShippingOptionRequest));
        }

        /// <summary>
        ///     Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before
        ///     checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest) {
            return null;
        }

        /// <summary>
        ///     Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues) {
            actionName = "Configure";
            controllerName = "ShippingSeeSharpShipUsps";
            routeValues = new RouteValueDictionary {{"Namespaces", "Nop.Plugin.Shipping.SeeSharpShipUsps.Controllers"}, {"area", null}};
        }

        /// <summary>
        ///     Install plugin
        /// </summary>
        public override void Install() {
            //settings
            var settings = new SeeSharpShipUspsSettings {
                Url = "http://production.shippingapis.com/ShippingAPI.dll",
                Username = "123",
                Password = "456",
                AdditionalHandlingCharge = 0,
                ZipPostalCodeFrom = "10022",
                BaseDomesticServicesSelected = string.Empty,
                DomesticServicesSelected = string.Empty,
                BaseInternationalServicesSelected = string.Empty,
                InternationalServicesSelected = string.Empty,
                MinimumShippingCharge = 0,
                InsuranceEnabled = false
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Url", "URL");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Url.Hint", "Specify USPS URL.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Username", "Username");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Username.Hint", "Specify USPS username.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Password", "Password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Password.Hint", "Specify USPS password.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.AdditionalHandlingCharge", "Additional handling charge");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.AdditionalHandlingCharge.Hint",
                "Enter additional handling fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.ZipPostalCodeFrom", "Shipped from zip");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.ZipPostalCodeFrom.Hint", "Specify origin zip code.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.MinimumShippingCharge", "Minimum Shipping Charge");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.MinimumShippingCharge.Hint",
                "Enter minimum shipping rate to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.InsuranceEnabled", "Insurance Enabled");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.InsuranceEnabled.Hint",
                "Enables the addition of insurance charges for both domestic and international shipments.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseDomesticServices", "Domestic Service Types");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseDomesticServices.Hint",
                "Select the service types you want to offer to customers.  Service types only affect how rate request is made.  ALL and ONLINE do not support insurance.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.DomesticServices", "Domestic Services");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.DomesticServices.Hint",
                "Select the services you want to offer to customers.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseInternationalServices", "International Service Types");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseInternationalServices.Hint",
                "Select the service types you want to offer to customers.  Service types only affect how rate request is made.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.InternationalServices", "International Services");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.InternationalServices.Hint",
                "Select the services you want to offer to customers.");

            base.Install();
        }

        /// <summary>
        ///     Uninstall plugin
        /// </summary>
        public override void Uninstall() {
            //settings
            _settingService.DeleteSetting<SeeSharpShipUspsSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Url");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Url.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Username");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Username.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Password");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.Password.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.AdditionalHandlingCharge");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.AdditionalHandlingCharge.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.ZipPostalCodeFrom");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.ZipPostalCodeFrom.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.MinimumShippingCharge");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.MinimumShippingCharge.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.InsuranceEnabled");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.InsuranceEnabled.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseDomesticServices");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseDomesticServices.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.DomesticServices");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.DomesticServices.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseInternationalServices");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.BaseInternationalServices.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.InternationalServices");
            this.DeletePluginLocaleResource("Plugins.Shipping.SeeSharpShipUsps.Fields.InternationalServices.Hint");

            base.Uninstall();
        }

        /// <summary>
        ///     Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType => ShippingRateComputationMethodType.Realtime;

        /// <summary>
        ///     Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker => new USPSShipmentTracker(_loggerService, _uspsSettings);

        #endregion

        private GetShippingOptionResponse GetShippingOptionsImpl(GetShippingOptionRequest shipmentPackage, bool isDomestic) {
            decimal minimumShippingCharge = _uspsSettings.MinimumShippingCharge;
            string username = _uspsSettings.Username;
            string password = _uspsSettings.Password;
            var response = new GetShippingOptionResponse();

            try {
                List<ShippingOption> shippingOptions = isDomestic
                    ? DoDomesticRequest(shipmentPackage, minimumShippingCharge, password, username)
                    : DoInternationalRequest(shipmentPackage, minimumShippingCharge, password, username);

                AddPrefixToEachShippingOption(shippingOptions);
                response.ShippingOptions = shippingOptions;
            } catch (Exception ex) {
                response.AddError(ex.Message);
            }

            return response;
        }

        private static void AddPrefixToEachShippingOption(IEnumerable<ShippingOption> shippingOptions) {
            foreach (ShippingOption option in shippingOptions.Where(option => !option.Name.StartsWith("USPS", StringComparison.CurrentCultureIgnoreCase))) {
                option.Name = $"USPS {option.Name}";
            }
        }

        private List<ShippingOption> DoInternationalRequest(GetShippingOptionRequest shipmentPackage, decimal minimumShippingRate, string password,
            string username) {
            IRateResponse response;
            IntlRateV2Request internationalRequest = CreateInternationalRequest(shipmentPackage, username, password);

            try {
                response = _rateService.Get(internationalRequest);
            } catch (Exception e) {
                throw new Exception("Unhandled exception with international request:\n\n" + internationalRequest.ToXmlString(), e);
            }

            return response.Error == null
                ? InterpretShippingOptions((IntlRateV2Response) response, minimumShippingRate)
                : new List<ShippingOption> {new ShippingOption {Name = response.Error.Description}};
        }

        private List<ShippingOption> DoDomesticRequest(GetShippingOptionRequest shipmentPackage, decimal minimumShippingRate, string password, string username) {
            IRateResponse response;
            RateV4Request domesticRequest = CreateDomesticRequest(shipmentPackage, username, password);

            try {
                response = _rateService.Get(domesticRequest);
            } catch (Exception e) {
                throw new Exception("Unhandled exception with domestic request:\n\n" + domesticRequest.ToXmlString(), e);
            }

            return response.Error == null
                ? InterpretShippingOptions((RateV4Response) response, minimumShippingRate)
                : new List<ShippingOption> {new ShippingOption {Name = response.Error.Description}};
        }

        private List<ShippingOption> InterpretShippingOptions(RateV4Response response, decimal minimumShippingRate) {
            if (response == null) {
                throw new ArgumentNullException(nameof(response));
            }

            if (minimumShippingRate < 0) {
                throw new ArgumentOutOfRangeException(nameof(minimumShippingRate), minimumShippingRate, "minimumShippingRate must be greater than zero");
            }

            string[] carrierServicesOffered = _uspsSettings.DomesticServicesSelected.Split(',');
            decimal additionalHandlingCharge = _uspsSettings.AdditionalHandlingCharge;
            var options = new List<ShippingOption>();

            foreach (Package package in response.Packages) {
                // indicate a package error if there is one and skip to the next package
                if (package.Error != null) {
                    options.Add(new ShippingOption {Name = package.Error.Description});
                    continue;
                }

                foreach (Postage postage in package.Postages) {
                    // service doesn't match one that is enabled, move on to the next one
                    if (!carrierServicesOffered.Contains(postage.ClassId)) {
                        continue;
                    }

                    string serviceName = GetModifiedServiceName(postage.MailService);

                    SpecialService insurance = postage.SpecialServices?.FirstOrDefault(s => s.ServiceId == "1" || s.ServiceId == "11");
                    decimal rate = postage.Rate + (insurance?.Price ?? 0) + additionalHandlingCharge;
                    ShippingOption shippingOption = options.Find(o => o.Name == serviceName);

                    // Use min shipping amount if rate is less than minimum
                    rate = rate < minimumShippingRate ? minimumShippingRate : rate;

                    if (shippingOption == null) {
                        // service doesn't exist yet, so create a new one
                        shippingOption = new ShippingOption {
                            Name = serviceName,
                            Rate = rate,
                        };

                        options.Add(shippingOption);
                    } else {
                        // service is already in the list, so let's add the current postage rate to it
                        shippingOption.Rate += rate;
                    }
                }
            }

            return options;
        }

        private List<ShippingOption> InterpretShippingOptions(IntlRateV2Response response, decimal minimumShippingRate) {
            if (response == null) {
                throw new ArgumentNullException(nameof(response));
            }

            if (response.Error != null) {
                return new List<ShippingOption> {new ShippingOption {Name = response.Error.Description}};
            }

            if (minimumShippingRate < 0) {
                throw new ArgumentOutOfRangeException(nameof(minimumShippingRate), minimumShippingRate, "minimumShippingRate must be greater than zero");
            }

            string[] carrierServicesOffered = _uspsSettings.InternationalServicesSelected.Split(',');
            decimal additionalHandlingCharge = _uspsSettings.AdditionalHandlingCharge;
            var options = new List<ShippingOption>();

            foreach (SeeSharpShip.Models.Usps.International.Response.Package package in response.Packages) {
                // indicate a package error if there is one and skip to the next package
                if (package.Error != null) {
                    options.Add(new ShippingOption {Name = package.Error.Description});
                    continue;
                }

                foreach (Service service in package.Services) {
                    // service doesn't match one that is enabled, move on to the next one
                    if (!carrierServicesOffered.Contains(service.Id)) {
                        continue;
                    }

                    string serviceName = GetModifiedServiceName(service.SvcDescription);

                    ExtraService insurance = service.ExtraServices?.FirstOrDefault(s => s.ServiceId == "1");
                    decimal rate = service.Postage + (insurance?.Price ?? 0) + additionalHandlingCharge;
                    ShippingOption shippingOption = options.Find(o => o.Name == serviceName);

                    // Use min shipping amount if rate is less than minimum
                    rate = rate < minimumShippingRate ? minimumShippingRate : rate;

                    if (shippingOption == null) {
                        // service doesn't exist yet, so create a new one
                        shippingOption = new ShippingOption {
                            Name = serviceName,
                            Rate = rate,
                        };

                        options.Add(shippingOption);
                    } else {
                        // service is already in the list, so let's add the current postage rate to it
                        shippingOption.Rate += rate;
                    }
                }
            }

            return options;
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

        private IntlRateV2Request CreateInternationalRequest(GetShippingOptionRequest shipmentPackage, string username, string password) {
            var request = new IntlRateV2Request {UserId = username, Password = password, Packages = new List<InternationalPackage>()};
            decimal totalWeight = _shippingService.GetTotalWeight(shipmentPackage);
            IEnumerable<List<USPSVolumetrics>> splitVolumetrics = SplitShipmentByVolumetrics(shipmentPackage, totalWeight);

            foreach (var item in splitVolumetrics) {
                foreach (MailType baseService in EnabledBaseInternationalServices()) {
                    request.Packages.Add(CreateInternationalPackage(shipmentPackage, item, baseService));
                }
            }

            return request;
        }

        private RateV4Request CreateDomesticRequest(GetShippingOptionRequest shipmentPackage, string username, string password) {
            var request = new RateV4Request {UserId = username, Password = password, Packages = new List<DomesticPackage>()};
            decimal totalWeight = _shippingService.GetTotalWeight(shipmentPackage);
            IEnumerable<List<USPSVolumetrics>> splitVolumetrics = SplitShipmentByVolumetrics(shipmentPackage, totalWeight);

            foreach (var item in splitVolumetrics) {
                foreach (ServiceTypes baseService in EnabledBaseDomesticServices()) {
                    request.Packages.Add(CreateDomesticPackage(shipmentPackage, item, baseService));
                }
            }

            return request;
        }

        private DomesticPackage CreateDomesticPackage(GetShippingOptionRequest shipmentPackage, List<USPSVolumetrics> item, ServiceTypes baseService) {
            decimal weightSum = item.Sum(i => i.Weight);
            int pounds = _uspsVolumetricsService.GetWeightInPounds(weightSum);
            int ounces = _uspsVolumetricsService.GetWeightRemainderInOunces(pounds, weightSum);

            var package = new DomesticPackage {
                ZipOrigination = _uspsSettings.ZipPostalCodeFrom,
                ZipDestination = shipmentPackage.ShippingAddress.ZipPostalCode,
                Length = item.Sum(i => i.Length),
                Height = item.Sum(i => i.Height),
                Width = item.Sum(i => i.Width),
                Pounds = pounds,
                Ounces = ounces,
                Value = $"{GetPackageSubTotal(shipmentPackage):0.00}",
                Container = "RECTANGULAR",
                ShipDate = $"{DateTime.Now.Date:dd-MMM-yyyy}",
                SelectedServiceType = baseService,
                SpecialServices = GetSpecialServicesForInsurance(baseService)
            };
            return package;
        }

        private InternationalPackage CreateInternationalPackage(GetShippingOptionRequest shipmentPackage, List<USPSVolumetrics> item, MailType baseService) {
            decimal weightSum = item.Sum(i => i.Weight);
            int pounds = _uspsVolumetricsService.GetWeightInPounds(weightSum);
            int ounces = _uspsVolumetricsService.GetWeightRemainderInOunces(pounds, weightSum);

            var package = new InternationalPackage {
                OriginZip = _uspsSettings.ZipPostalCodeFrom,
                Country = shipmentPackage.ShippingAddress.Country.Name,
                Length = item.Sum(i => i.Length),
                Height = item.Sum(i => i.Height),
                Width = item.Sum(i => i.Width),
                Pounds = pounds,
                Ounces = ounces,
                ValueOfContents = $"{GetPackageSubTotal(shipmentPackage):0.00}",
                Container = "RECTANGULAR",
                SelectedMailType = baseService,
                ExtraServices = GetExtraServicesForInsurance()
            };

            return package;
        }

        /// <summary>
        ///     Gets USPS's extra services for international insurance
        /// </summary>
        private ExtraServices GetExtraServicesForInsurance() {
            // 1 = Insurance
            return !_uspsSettings.InsuranceEnabled ? null : new ExtraServices {ExtraService = new[] {"1"}};
        }

        private IEnumerable<List<USPSVolumetrics>> SplitShipmentByVolumetrics(GetShippingOptionRequest shipmentPackage, decimal totalWeight) {
            MeasureDimension usedMeasureDimension = _uspsVolumetricsService.GetUsedMeasureDimension();
            MeasureDimension baseUsedMeasureDimension = _uspsVolumetricsService.GetBaseUsedMeasureDimension();

            IList<GetShippingOptionRequest.PackageItem> items = GetShippableCartItems(shipmentPackage);
            
            decimal weight = totalWeight;
            int packageLength = _uspsVolumetricsService.GetLength(shipmentPackage, usedMeasureDimension, baseUsedMeasureDimension);
            int packageHeight = _uspsVolumetricsService.GetHeight(shipmentPackage, usedMeasureDimension, baseUsedMeasureDimension);
            int packageWidth = _uspsVolumetricsService.GetWidth(shipmentPackage, usedMeasureDimension, baseUsedMeasureDimension);

            // First, split items that must be shipped separately
            IList<List<USPSVolumetrics>> splitVolumetrics = _uspsPackageSplitter.SplitByShipSeparately(items).ToList();

            // Next, split items that are too heavy
            if (_uspsVolumetricsService.IsTooHeavy(weight)) {
                splitVolumetrics = _uspsPackageSplitter.SplitByWeight(items).ToList();
            }

            // Lastly, split items that are too large volumetrically
            if (_uspsVolumetricsService.IsTooLarge(packageLength, packageHeight, packageWidth)) {
                splitVolumetrics = splitVolumetrics.Concat(_uspsPackageSplitter.SplitByMeasuredSize(items)).ToList();
            }

            return splitVolumetrics;
        }

        private static IList<GetShippingOptionRequest.PackageItem> GetShippableCartItems(GetShippingOptionRequest shipmentPackage) {
            List<GetShippingOptionRequest.PackageItem> items = shipmentPackage.Items
                .Where(i => i.ShoppingCartItem.IsShipEnabled &&
                    i.ShoppingCartItem.Product.IsShipEnabled &&
                    !i.ShoppingCartItem.Product.IsGiftCard && 
                    !i.ShoppingCartItem.Product.IsDownload)
                .ToList();
            return items;
        }

        /// <summary>
        ///     Gets USPS's special services for domestic insurance
        /// </summary>
        private SpecialServices GetSpecialServicesForInsurance(ServiceTypes baseService) {
            if (!_uspsSettings.InsuranceEnabled) {
                return null;
            }

            var expressServices = new[] {
                ServiceTypes.Express, ServiceTypes.ExpressCommercial, ServiceTypes.ExpressHfp, ServiceTypes.ExpressHfpCommercial,
                ServiceTypes.ExpressSh, ServiceTypes.ExpressShCommercial
            };

            if (expressServices.Any(e => e == baseService)) {
                // 11 = Express Mail Insurance
                return new SpecialServices {SpecialService = new[] {"11"}};
            }

            var priorityServices = new[] {ServiceTypes.Priority, ServiceTypes.PriorityCommercial, ServiceTypes.PriorityHfpCommercial};
            return priorityServices.Any(p => p == baseService) ? new SpecialServices {SpecialService = new[] {"1"}} : null;
        }

        private IEnumerable<ServiceTypes> EnabledBaseDomesticServices() {
            string[] baseServicesOffered = _uspsSettings.BaseDomesticServicesSelected.Split(',');

            foreach (string item in baseServicesOffered) {
                ServiceTypes enabledService;
                if (Enum.TryParse(item, true, out enabledService)) {
                    yield return enabledService;
                }
            }
        }

        private IEnumerable<MailType> EnabledBaseInternationalServices() {
            string[] baseServicesOffered = _uspsSettings.BaseInternationalServicesSelected.Split(',');

            foreach (string item in baseServicesOffered) {
                MailType enabledService;
                if (Enum.TryParse(item, true, out enabledService)) {
                    yield return enabledService;
                }
            }
        }

        private decimal GetPackageSubTotal(GetShippingOptionRequest shipmentPackage) {
            return shipmentPackage.Items.Where(item => !item.ShoppingCartItem.IsFreeShipping).Sum(item => _priceCalculationService.GetSubTotal(item.ShoppingCartItem));
        }

        protected bool IsDomesticRequest(GetShippingOptionRequest shipmentPackage) {
            //Origin Country must be USA, Collect USA from list of countries
            if (shipmentPackage?.ShippingAddress?.Country == null) {
                return true;
            }
            return shipmentPackage.ShippingAddress.Country.ThreeLetterIsoCode == "USA";
        }
    }
}
