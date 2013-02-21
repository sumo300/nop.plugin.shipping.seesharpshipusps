using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Services.Directory;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps.Services
{
    public class USPSVolumetricsService
    {
        private readonly IMeasureService _measureService;
        private readonly MeasureSettings _measureSettings;

        public USPSVolumetricsService(IMeasureService measureService, MeasureSettings measureSettings)
        {
            _measureService = measureService;
            _measureSettings = measureSettings;
        }

        /// <summary>
        ///     Pieces may not measure more than 108 inches in length and girth combined
        /// </summary>
        public bool IsTooLarge(int length, int height, int width) { return TotalSize(length, height, width) > USPSConstants.LargestPackageSize; }

        /// <summary>
        ///     This calculation assumes that length, width, and height have been entered correctly with length being the longest side.
        /// </summary>
        public int TotalSize(int length, int height, int width)
        {
            int girth = 2 * (width + height);
            int total = girth + length;
            return total;
        }

        public bool IsTooHeavy(decimal weight) { return weight > USPSConstants.MaxPackageWeight; }

        public int GetWeightRemainderInOunces(int weightInPounds, decimal weight)
        {
            if (weightInPounds <= 0 && weight <= 0)
            {
                return 0;
            }

            // If weight is less than one, our remainder is the weight (e.g. 0.5)
            decimal remainder;
            if (weight < 1 && weightInPounds == 0)
            {
                remainder = weight;
            } else
            {
                remainder = decimal.Remainder(weight, weightInPounds);
            }
            
            return
                Convert.ToInt32(_measureService.ConvertWeight(remainder, _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId),
                                                              _measureService.GetMeasureWeightBySystemKeyword(USPSConstants.MeasureWeightSystemKeyword)));
        }

        public int GetWeightInPounds(decimal weight) { return weight <= 0 ? 0 : Convert.ToInt32(Math.Floor(weight * _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).Ratio)); }

        public int GetWidth(GetShippingOptionRequest shipmentPackage, MeasureDimension usedMeasureDimension,
                                   MeasureDimension baseUsedMeasureDimension)
        {
            decimal totalWidth = shipmentPackage.GetTotalWidth();
            int packageWidth = Convert.ToInt32(Math.Ceiling(totalWidth / (baseUsedMeasureDimension.Ratio * usedMeasureDimension.Ratio)));
            return packageWidth < 1 ? 1 : packageWidth;
        }

        public int GetHeight(GetShippingOptionRequest shipmentPackage, MeasureDimension usedMeasureDimension,
                                    MeasureDimension baseUsedMeasureDimension)
        {
            decimal totalHeight = shipmentPackage.GetTotalHeight();
            int packageHeight = Convert.ToInt32(Math.Ceiling(totalHeight / (baseUsedMeasureDimension.Ratio * usedMeasureDimension.Ratio)));
            return packageHeight < 1 ? 1 : packageHeight;
        }

        public int GetLength(GetShippingOptionRequest shipmentPackage, MeasureDimension usedMeasureDimension,
                                    MeasureDimension baseUsedMeasureDimension)
        {
            decimal totalLength = shipmentPackage.GetTotalLength();
            int packageLength = Convert.ToInt32(Math.Ceiling(totalLength / (baseUsedMeasureDimension.Ratio * usedMeasureDimension.Ratio)));
            return packageLength < 1 ? 1 : packageLength;
        }

        public decimal GetWeight(IEnumerable<ShoppingCartItem> items)
        {
            decimal totalWeight = items.Sum(i => i.ProductVariant.Weight * i.Quantity);
            return totalWeight * GetBaseUsedMeasureWeight().Ratio;
        }

        public MeasureDimension GetUsedMeasureDimension()
        {
            MeasureDimension usedMeasureDimension = _measureService.GetMeasureDimensionBySystemKeyword(USPSConstants.MeasureDimensionSystemKeyword);

            if (usedMeasureDimension == null)
            {
                throw new NopException(string.Format("USPS shipping service. Could not load \"{0}\" measure dimension (target) conversion ratio",
                                                     USPSConstants.MeasureDimensionSystemKeyword));
            }

            return usedMeasureDimension;
        }

        public MeasureDimension GetBaseUsedMeasureDimension()
        {
            MeasureDimension baseUsedMeasureDimension = _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);

            if (baseUsedMeasureDimension == null)
            {
                throw new NopException("USPS shipping service. Could not load default measure dimension (current) conversion ratio");
            }

            return baseUsedMeasureDimension;
        }

        private MeasureWeight GetBaseUsedMeasureWeight()
        {
            MeasureWeight baseUsedMeasureWeight = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);

            if (baseUsedMeasureWeight == null)
            {
                throw new NopException(string.Format("USPS shipping service.  Could not load default weight dimension (current) conversion ratio"));
            }

            return baseUsedMeasureWeight;
        }
    }
}
