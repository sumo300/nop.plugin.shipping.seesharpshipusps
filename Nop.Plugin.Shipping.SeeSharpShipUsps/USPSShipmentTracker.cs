using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Caching;
using Nop.Services.Logging;
using Nop.Services.Shipping.Tracking;
using SeeSharpShip.Models.Usps;
using SeeSharpShip.Services.Usps;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps {
    public class USPSShipmentTracker : IShipmentTracker {
        private readonly Cache _cache = new Cache();
        private readonly ILogger _logger;
        private readonly ITrackService _trackService;
        private readonly SeeSharpShipUspsSettings _uspsSettings;

        public USPSShipmentTracker(ILogger logger, SeeSharpShipUspsSettings uspsSettings) {
            _logger = logger;
            _uspsSettings = uspsSettings;

            // ReSharper disable once CSharpWarnings::CS0618
            _trackService = string.IsNullOrWhiteSpace(uspsSettings.Url) ? new TrackService() : new TrackService(uspsSettings.Url, new PostRequest());
        }

        #region IShipmentTracker Members

        /// <summary>
        ///     Matches the following formats:
        ///     ^(7\d{19})$
        ///     ^((92|93|94)\d{20})$
        ///     ^(82\d{8})$
        ///     ^((M|EC|EA|CP|RA)\d{9}US)$
        ///     ^((14|23|03)\d{18})$
        /// </summary>
        /// <param name="trackingNumber"></param>
        /// <returns></returns>
        public virtual bool IsMatch(string trackingNumber) {
            if (string.IsNullOrWhiteSpace(trackingNumber)) {
                return false;
            }

            var patterns = new[] {
                @"^(7\d{19})$",
                @"^((92|93|94)\d{20})$",
                @"^(82\d{8})$",
                @"^((M|EC|EA|CP|RA)\d{9}US)$",
                @"^((14|23|03)\d{18})$"
            };

            return patterns.Any(pattern => Regex.IsMatch(trackingNumber, pattern, RegexOptions.IgnoreCase));
        }

        public virtual string GetUrl(string trackingNumber) {
            return string.Format("https://tools.usps.com/go/TrackConfirmAction.action?tLabels={0}", trackingNumber);
        }

        public virtual IList<ShipmentStatusEvent> GetShipmentEvents(string trackingNumber) {
            var events = new List<ShipmentStatusEvent>();

            if (string.IsNullOrWhiteSpace(trackingNumber)) {
                _logger.Information("Tracking number cannot be null or empty");
                return events;
            }

            try {
                var request = new TrackRequest {
                    TrackId = new TrackId {Id = trackingNumber},
                    UserId = _uspsSettings.Username
                };
                TrackResponse response = _trackService.Get(request);
                TrackInfo info = response.TrackInfo.FirstOrDefault();

                if (info != null) {
                    events.AddRange(info.TrackDetail.Select(e => new ShipmentStatusEvent { EventName = e.Value }));
                }
            } catch (Exception e) {
                _logger.Error(string.Format("Error while getting USPS shipment tracking info - {0}", trackingNumber), e);
            }

            return events;
        }

        #endregion
    }
}
