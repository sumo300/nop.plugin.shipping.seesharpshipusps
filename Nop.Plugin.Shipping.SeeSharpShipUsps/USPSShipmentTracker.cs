using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;
using Nop.Services.Shipping.Tracking;
using SeeSharpShip.Models.Usps;
using SeeSharpShip.Services.Usps;

namespace Nop.Plugin.Shipping.SeeSharpShipUsps {
    public class USPSShipmentTracker : IShipmentTracker {
        private readonly ITrackService _trackService;
        private readonly string _userId;
        private readonly Cache _cache = new Cache();

        public USPSShipmentTracker(ITrackService trackService, string userId) {
            _trackService = trackService;
            _userId = userId;
        }

        public bool IsMatch(string trackingNumber) {
            return GetShipmentEvents(trackingNumber) != null;
        }

        public string GetUrl(string trackingNumber) {
            return string.Format("https://tools.usps.com/go/TrackConfirmAction.action?tLabels={0}", trackingNumber);
        }

        public IList<ShipmentStatusEvent> GetShipmentEvents(string trackingNumber) {
            if (string.IsNullOrWhiteSpace(trackingNumber)) {
                throw new ArgumentNullException("trackingNumber", "Tracking number cannot be null or empty");
            }

            var events = _cache.Get(trackingNumber) as List<ShipmentStatusEvent>;
            if (events != null) {
                return events;
            }

            events = new List<ShipmentStatusEvent>();

            var request = new TrackRequest {
                TrackId = new TrackId {Id = trackingNumber},
                UserId = _userId
            };
            TrackResponse response = _trackService.Get(request);

            if (!response.TrackInfo.Any()) {
                return new[] {new ShipmentStatusEvent {EventName = "NOT FOUND"}};
            }
            TrackInfo info = response.TrackInfo.First();

            events.AddRange(info.TrackDetail.Select(e => new ShipmentStatusEvent {EventName = e.Value}));

            _cache.Add(trackingNumber, events, null, DateTime.Now.AddHours(1), TimeSpan.Zero, CacheItemPriority.Default, null);

            return events;
        }
    }
}