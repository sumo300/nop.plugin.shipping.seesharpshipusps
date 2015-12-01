using System;
using System.Collections.Generic;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Shipping.SeeSharpShipUsps.Domain;
using Nop.Services.Directory;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps.Services {
    public class USPSPackageSplitterService
    {
        private readonly USPSVolumetricsService _uspsVolumetricsService;

        public USPSPackageSplitterService(IMeasureService measureService, IShippingService shippingService, MeasureSettings measureSettings) {
            _uspsVolumetricsService = new USPSVolumetricsService(measureService, shippingService, measureSettings);
        }

        public IEnumerable<List<USPSVolumetrics>> SplitByShipSeparately(IEnumerable<GetShippingOptionRequest.PackageItem> items) {
            var splitItems = new List<USPSVolumetrics>();

            foreach (GetShippingOptionRequest.PackageItem item in items) {
                decimal currentWeight = item.ShoppingCartItem.Product.Weight * item.ShoppingCartItem.Quantity;

                if (item.ShoppingCartItem.Product.ShipSeparately) {
                    foreach (var p in SplitItemIntoPackages(item, 1, currentWeight)) {
                        yield return p;
                    }

                    continue;
                }

                splitItems.Add(new USPSVolumetrics {
                    Height = (int)GetTotalHeightForCartItem(item),
                    Length = (int)GetTotalLengthForCartItem(item),
                    Weight = currentWeight,
                    Width = (int)GetTotalWidthForCartItem(item)
                });
            }

            if (splitItems.Count > 0) {
                yield return splitItems;
            }
        }

        /// <summary>
        ///     Splits a package by weight by yielding multple lists of volumetrics
        ///     using the MaxPackageWeight as the partition.
        /// </summary>
        public IEnumerable<List<USPSVolumetrics>> SplitByWeight(IEnumerable<GetShippingOptionRequest.PackageItem> items) {
            decimal weight = 0;
            var splitItems = new List<USPSVolumetrics>();

            foreach (GetShippingOptionRequest.PackageItem item in items) {
                decimal currentWeight = item.ShoppingCartItem.Product.Weight * item.ShoppingCartItem.Quantity;

                // Single item is overweight
                if (currentWeight > USPSConstants.MaxPackageWeight) {
                    int packagesNeeded = Convert.ToInt32(Math.Ceiling(currentWeight / USPSConstants.MaxPackageWeight));

                    foreach (var p in SplitItemIntoPackages(item, packagesNeeded, currentWeight)) {
                        yield return p;
                    }

                    continue;
                }

                if (weight + currentWeight > USPSConstants.MaxPackageWeight) {
                    yield return splitItems;
                    splitItems = new List<USPSVolumetrics>();
                }

                weight += currentWeight;

                splitItems.Add(new USPSVolumetrics {
                    Height = (int) GetTotalHeightForCartItem(item),
                    Length = (int) GetTotalLengthForCartItem(item),
                    Weight = currentWeight,
                    Width = (int) GetTotalWidthForCartItem(item)
                });
            }

            if (splitItems.Count > 0) {
                yield return splitItems;
            }
        }

        /// <summary>
        ///     Splits a package by its measurements by yielding multiple lists of
        ///     volumetrics using the LargestPackageSize as the partition.
        /// </summary>
        public IEnumerable<List<USPSVolumetrics>> SplitByMeasuredSize(IEnumerable<GetShippingOptionRequest.PackageItem> items) {
            int size = 0;
            var splitItems = new List<USPSVolumetrics>();

            foreach (GetShippingOptionRequest.PackageItem item in items) {
                int length = (int) item.ShoppingCartItem.Product.Length * item.ShoppingCartItem.Quantity;
                int height = (int) item.ShoppingCartItem.Product.Height * item.ShoppingCartItem.Quantity;
                int width = (int) item.ShoppingCartItem.Product.Width * item.ShoppingCartItem.Quantity;
                int currentPackageSize = _uspsVolumetricsService.TotalSize(length, height, width);
                decimal currentWeight = item.ShoppingCartItem.Product.Weight * item.ShoppingCartItem.Quantity;

                // Single item is oversized
                if (currentPackageSize > USPSConstants.LargestPackageSize) {
                    int packagesNeeded = Convert.ToInt32(Math.Ceiling(((decimal) currentPackageSize / USPSConstants.LargestPackageSize)));

                    foreach (var p in SplitItemIntoPackages(item, packagesNeeded, currentWeight)) {
                        yield return p;
                    }

                    continue;
                }

                if (size + currentPackageSize > USPSConstants.LargestPackageSize) {
                    yield return splitItems;
                    splitItems = new List<USPSVolumetrics>();
                }

                size += currentPackageSize;

                splitItems.Add(new USPSVolumetrics {
                    Height = height,
                    Length = length,
                    Weight = item.ShoppingCartItem.Product.Weight,
                    Width = width
                });
            }

            if (splitItems.Count > 0) {
                yield return splitItems;
            }
        }

        /// <summary>
        ///     Splits a single item into packages
        /// </summary>
        private IEnumerable<List<USPSVolumetrics>> SplitItemIntoPackages(GetShippingOptionRequest.PackageItem item, int packagesNeeded, decimal currentWeight) {
            int splitHeight = Convert.ToInt32((GetTotalHeightForCartItem(item)) / packagesNeeded);
            int splitLength = Convert.ToInt32((GetTotalLengthForCartItem(item)) / packagesNeeded);
            int splitWidth = Convert.ToInt32((GetTotalWidthForCartItem(item)) / packagesNeeded);
            int splitWeight = Convert.ToInt32(currentWeight / packagesNeeded);

            var splitItems = new List<USPSVolumetrics>();
            for (int i = 0; i < packagesNeeded; i++) {
                splitItems.Add(new USPSVolumetrics {
                    Height = splitHeight,
                    Length = splitLength,
                    Weight = splitWeight,
                    Width = splitWidth
                });

                yield return splitItems;
                splitItems = new List<USPSVolumetrics>();
            }
        }

        private decimal GetTotalWidthForCartItem(GetShippingOptionRequest.PackageItem item) {
            decimal width = item.ShoppingCartItem.Product.Width;
            return (width <= 0 ? 1 : width) * item.ShoppingCartItem.Quantity;
        }

        private decimal GetTotalLengthForCartItem(GetShippingOptionRequest.PackageItem item) {
            decimal length = item.ShoppingCartItem.Product.Length;
            return (length <= 0 ? 1 : length) * item.ShoppingCartItem.Quantity;
        }

        private decimal GetTotalHeightForCartItem(GetShippingOptionRequest.PackageItem item) {
            decimal height = item.ShoppingCartItem.Product.Height;
            return (height <= 0 ? 1 : height) * item.ShoppingCartItem.Quantity;
        }
    }
}
