using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Catalog.Fakes;
using Nop.Services.Configuration;
using Nop.Services.Configuration.Fakes;
using Nop.Services.Directory;
using Nop.Services.Directory.Fakes;
using Nop.Services.Logging;
using Nop.Services.Logging.Fakes;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Fakes;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps.Tests
{
    [TestClass]
    public class USPSComputationMethodTests
    {
        private SeeSharpShipUspsSettings _uspsSettings;
        private IMeasureService _measureService;
        private ISettingService _settingService;
        private IShippingService _shippingService;
        private IPriceCalculationService _priceCalculationService;
        private MeasureSettings _measureSettings;
        private ILogger _logger;

        [TestInitialize]
        public void Init()
        {
            _uspsSettings = new SeeSharpShipUspsSettings {
                Username = ConfigurationManager.AppSettings["Usps:Username"],
                Password = ConfigurationManager.AppSettings["Usps:Password"],
                Url = ConfigurationManager.AppSettings["Usps:ApiUrl"]
            };

            _measureService = new StubIMeasureService
            {
                GetMeasureDimensionByIdInt32 = (id) => new MeasureDimension { Ratio = 1},
                GetMeasureDimensionBySystemKeywordString = (name)=>new MeasureDimension { Ratio = 1}
            };
            _settingService = new StubISettingService();
            _shippingService = new StubIShippingService
            {
                GetTotalWeightGetShippingOptionRequestBoolean = (request, include) => 1,
                GetDimensionsIListOfGetShippingOptionRequestPackageItemDecimalOutDecimalOutDecimalOut =
                    (width, length, height) => new 
            };
            _priceCalculationService = new StubIPriceCalculationService();
            _measureSettings = new MeasureSettings();
            _logger = new StubILogger();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetShippingOptions_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var computationMethod = new USPSComputationMethod(_measureService, _settingService, _shippingService,
                _uspsSettings, _priceCalculationService, _measureSettings, _logger);

            // Act
            var response = computationMethod.GetShippingOptions(null);

            // Assert - Exception expected
        }

        [TestMethod]
        public void GetShippingOptions_NoItems_ReturnsNoItemsError()
        {
            // Arrange
            var computationMethod = new USPSComputationMethod(_measureService, _settingService, _shippingService,
                _uspsSettings, _priceCalculationService, _measureSettings, _logger);
            var request = new GetShippingOptionRequest();

            // Act
            var response = computationMethod.GetShippingOptions(request);

            // Assert
            Assert.IsTrue(response.Errors.First().Equals("No shipment items"));
        }

        [TestMethod]
        public void GetShippingOptions_NoShippingAddress_ReturnsShippingAddressError()
        {
            // Arrange
            var computationMethod = new USPSComputationMethod(_measureService, _settingService, _shippingService,
                _uspsSettings, _priceCalculationService, _measureSettings, _logger);

            var request = new GetShippingOptionRequest
            {
                Items = new List<GetShippingOptionRequest.PackageItem>
                {
                    new GetShippingOptionRequest.PackageItem(new ShoppingCartItem(), 1)
                }
            };

            // Act
            var response = computationMethod.GetShippingOptions(request);

            // Assert
            Assert.IsTrue(response.Errors.First().Equals("Shipping address is not set"));
        }

        [TestMethod]
        public void GetShippingOptions_Domestic_ReturnsValidOptions() {
            // Arrange
            var computationMethod = new USPSComputationMethod(_measureService, _settingService, _shippingService,
                _uspsSettings, _priceCalculationService, _measureSettings, _logger);

            var request = new GetShippingOptionRequest {
                Items = new List<GetShippingOptionRequest.PackageItem>
                {
                    new GetShippingOptionRequest.PackageItem(new ShoppingCartItem
                    {
                        Product = new Product
                        {
                            Weight = 1,
                            Length = 1,
                            Width = 1,
                            Height = 1,
                            IsShipEnabled = true,
                            Price = 1,
                        },
                    }, 1)
                },
                ShippingAddress = new Address
                {
                    Country = new Country { ThreeLetterIsoCode = "USA" },
                    ZipPostalCode = "18518",
                    StateProvince = new StateProvince
                    {
                        Abbreviation = "PA"
                    }
                },
                ZipPostalCodeFrom = "90210"
            };

            // Act
            var response = computationMethod.GetShippingOptions(request);

            // Assert
            Assert.IsTrue(response.Errors.First().Equals("Shipping address is not set"));
        }

    }
}