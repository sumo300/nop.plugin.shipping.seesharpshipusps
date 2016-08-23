using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Shipping.SeeSharpShipUsps.Services;
using Nop.Services.Directory;
using Nop.Services.Directory.Fakes;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Fakes;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps.Tests {
    [TestClass]
    public class USPSVolumetricsServiceTests
    {
        [TestMethod]
        public void IsTooLarge_NoDimentions_ReturnsFalse() {
            // Arrange
            IMeasureService measureService = new StubIMeasureService();
            IShippingService shippingService = new StubIShippingService();
            MeasureSettings measureSettings = new MeasureSettings {BaseDimensionId = 1, BaseWeightId = 1};
            var service = new USPSVolumetricsService(measureService, shippingService, measureSettings);

            // Act
            bool isTooLarge = service.IsTooLarge(0, 0, 0);

            // Assert
            Assert.IsFalse(isTooLarge);
        }

        [TestMethod]
        public void IsTooLarge_MaxDimentions_ReturnsTrue() {
            // Arrange
            IMeasureService measureService = new StubIMeasureService();
            IShippingService shippingService = new StubIShippingService();
            MeasureSettings measureSettings = new MeasureSettings {BaseDimensionId = 1, BaseWeightId = 1};
            var service = new USPSVolumetricsService(measureService, shippingService, measureSettings);

            // Act
            bool isTooLarge = service.IsTooLarge(int.MaxValue, int.MaxValue, int.MaxValue);

            // Assert
            Assert.IsTrue(isTooLarge);
        }

        [TestMethod]
        public void IsTooLarge_SquareDimensions_ReturnsFalse() {
            // Arrange
            IMeasureService measureService = new StubIMeasureService();
            IShippingService shippingService = new StubIShippingService();
            MeasureSettings measureSettings = new MeasureSettings {BaseDimensionId = 1, BaseWeightId = 1};
            var service = new USPSVolumetricsService(measureService, shippingService, measureSettings);

            // Act
            bool isTooLarge = service.IsTooLarge(10, 10, 10);

            // Assert
            Assert.IsFalse(isTooLarge);
        }

        [TestMethod]
        public void TotalSize_NoDimensions_ReturnsZero()
        {
            // Arrange
            IMeasureService measureService = new StubIMeasureService();
            IShippingService shippingService = new StubIShippingService();
            MeasureSettings measureSettings = new MeasureSettings { BaseDimensionId = 1, BaseWeightId = 1 };
            var service = new USPSVolumetricsService(measureService, shippingService, measureSettings);

            // Act
            int total = service.TotalSize(0, 0, 0);

            // Assert
            Assert.AreEqual(0, total);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TotalSize_MaxDimensions_ThrowsException()
        {
            // Arrange
            IMeasureService measureService = new StubIMeasureService();
            IShippingService shippingService = new StubIShippingService();
            MeasureSettings measureSettings = new MeasureSettings { BaseDimensionId = 1, BaseWeightId = 1 };
            var service = new USPSVolumetricsService(measureService, shippingService, measureSettings);

            // Act
            int total = service.TotalSize(int.MaxValue, int.MaxValue, int.MaxValue);

            // Assert - Expects Exception
        }
    }
}
