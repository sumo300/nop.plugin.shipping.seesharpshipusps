using System;
using System.Collections.Generic;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Shipping.SeeSharpShipUsps.Domain;
using Nop.Services.Directory;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps.Services
{
    public class USPSPackageSplitterService
    {
        private readonly USPSVolumetricsService _uspsVolumetricsService;

        public USPSPackageSplitterService(IMeasureService measureService, MeasureSettings measureSettings) { _uspsVolumetricsService = new USPSVolumetricsService(measureService, measureSettings); }

        /// <summary>
        ///     Splits a package by weight by yielding multple lists of volumetrics
        ///     using the MaxPackageWeight as the partition.
        /// </summary>
        public IEnumerable<List<USPSVolumetrics>> SplitByWeight(IEnumerable<ShoppingCartItem> items)
        {
            decimal weight = 0;
            var splitItems = new List<USPSVolumetrics>();

            foreach (var item in items)
            {
                decimal currentWeight = item.ProductVariant.Weight * item.Quantity;

                // Single item is overweight
                if (currentWeight > USPSConstants.MaxPackageWeight)
                {
                    int packagesNeeded = Convert.ToInt32(Math.Ceiling(currentWeight / USPSConstants.MaxPackageWeight));
                    
                    foreach (var p in SplitItemIntoPackages(item, packagesNeeded, currentWeight))
                    {
                        yield return p;
                    }

                    continue;
                }

                if (weight + currentWeight > USPSConstants.MaxPackageWeight)
                {
                    yield return splitItems;
                    splitItems = new List<USPSVolumetrics>();
                }

                weight += currentWeight;

                splitItems.Add(new USPSVolumetrics
                {
                    Height = (int) GetTotalHeightForCartItem(item),
                    Length = (int) GetTotalLengthForCartItem(item),
                    Weight = currentWeight,
                    Width = (int) GetTotalWidthForCartItem(item)
                });
            }

            if (splitItems.Count > 0)
            {
                yield return splitItems;
            }
        }

        /// <summary>
        ///     Splits a package by its measurements by yielding multiple lists of
        ///     volumetrics using the LargestPackageSize as the partition.
        /// </summary>
        public IEnumerable<List<USPSVolumetrics>> SplitByMeasuredSize(IEnumerable<ShoppingCartItem> items)
        {
            int size = 0;
            var splitItems = new List<USPSVolumetrics>();

            foreach (var item in items)
            {
                var length = (int) item.ProductVariant.Length * item.Quantity;
                var height = (int) item.ProductVariant.Height * item.Quantity;
                var width = (int) item.ProductVariant.Width * item.Quantity;
                int currentPackageSize = _uspsVolumetricsService.TotalSize(length, height, width);
                decimal currentWeight = item.ProductVariant.Weight * item.Quantity;

                // Single item is oversized
                if (currentPackageSize > USPSConstants.LargestPackageSize)
                {
                    int packagesNeeded = Convert.ToInt32(Math.Ceiling(((decimal)currentPackageSize / USPSConstants.LargestPackageSize)));

                    foreach (var p in SplitItemIntoPackages(item, packagesNeeded, currentWeight))
                    {
                        yield return p;
                    }

                    continue;
                }

                if (size + currentPackageSize > USPSConstants.LargestPackageSize)
                {
                    yield return splitItems;
                    splitItems = new List<USPSVolumetrics>();
                }

                size += currentPackageSize;

                splitItems.Add(new USPSVolumetrics
                {
                    Height = height,
                    Length = length,
                    Weight = item.ProductVariant.Weight,
                    Width = width
                });
            }

            if (splitItems.Count > 0)
            {
                yield return splitItems;
            }
        }

        /// <summary>
        /// Splits a single item into packages
        /// </summary>
        private IEnumerable<List<USPSVolumetrics>> SplitItemIntoPackages(ShoppingCartItem item, int packagesNeeded, decimal currentWeight)
        {
            int splitHeight = Convert.ToInt32((GetTotalHeightForCartItem(item)) / packagesNeeded);
            int splitLength = Convert.ToInt32((GetTotalLengthForCartItem(item)) / packagesNeeded);
            int splitWidth = Convert.ToInt32((GetTotalWidthForCartItem(item)) / packagesNeeded);
            int splitWeight = Convert.ToInt32(currentWeight / packagesNeeded);

            var splitItems = new List<USPSVolumetrics>();
            for (var i = 0; i < packagesNeeded; i++)
            {
                splitItems.Add(new USPSVolumetrics
                {
                    Height = splitHeight,
                    Length = splitLength,
                    Weight = splitWeight,
                    Width = splitWidth
                });

                yield return splitItems;
                splitItems = new List<USPSVolumetrics>();
            }
        }

        private decimal GetTotalWidthForCartItem(ShoppingCartItem item)
        {
            decimal width = item.ProductVariant.Width;
            return (width <= 0 ? 1 : width) * item.Quantity;
        }

        private decimal GetTotalLengthForCartItem(ShoppingCartItem item)
        {
            decimal length = item.ProductVariant.Length;
            return (length <= 0 ? 1 : length) * item.Quantity;
        }

        private decimal GetTotalHeightForCartItem(ShoppingCartItem item)
        {
            decimal height = item.ProductVariant.Height;
            return (height <= 0 ? 1 : height) * item.Quantity;
        }
    }
}
