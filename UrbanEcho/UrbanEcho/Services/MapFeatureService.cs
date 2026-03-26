using CommunityToolkit.Mvvm.Messaging;
using Mapsui;
using System.Linq;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Models.UI;
using UrbanEcho.Sim;

namespace UrbanEcho.Services
{
    public interface IMapFeatureService
    {
        IntersectionUI? MapIntersection(IFeature feature);

        VehicleReadOnly? MapVehicle(IFeature feature);
    }

    public class MapFeatureService : IMapFeatureService
    {
        public IntersectionUI? MapIntersection(IFeature feature)
        {
            var rawId = feature["OBJECTID"]?.ToString();

            var intersections = SimManager.Instance.RoadIntersections.ToList();
            var simIntersection = intersections.FirstOrDefault(i => i.Feature["OBJECTID"]?.ToString() == rawId);

            if (simIntersection is null)
            {
                WeakReferenceMessenger.Default.Send(new LogMessage($"Intersection {rawId} not found", LogSource.Map));
                return null;
            }

            return new IntersectionUI
            {
                Id = int.TryParse(rawId, out int id) ? id : 0,
                GeoId = feature["GeoID"]?.ToString() ?? string.Empty,
                Name = feature["Intersecti"]?.ToString() ?? "Unknown",
                Municipality = feature["Municipali"]?.ToString() ?? string.Empty,
                OwnedBy = feature["OwnedBy"]?.ToString() ?? string.Empty,
                MaintainedBy = feature["Maintained"]?.ToString() ?? string.Empty,
                Status = feature["LifeStatus"]?.ToString() ?? string.Empty,
                Type = feature["Intersec_1"]?.ToString() ?? string.Empty,
                ConnectingRoads = new[] { "StreetName", "StreetNa_1", "StreetNa_2", "StreetNa_3", "StreetNa_4" }
                    .Select(f => feature[f]?.ToString())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList()!
            };
        }

        public VehicleReadOnly? MapVehicle(IFeature feature)
        {
            var rawId = feature["VehicleNumber"]?.ToString();
            if (!int.TryParse(rawId, out int vehicleId)) return null;

            VehicleReadOnly? simVehicle = SimManager.Instance.GetVehicles().ElementAtOrDefault(vehicleId);

            if (simVehicle is null)
            {
                WeakReferenceMessenger.Default.Send(new LogMessage($"Vehicle {vehicleId} not found", LogSource.Map));
                return null;
            }
            return simVehicle;
        }
    }
}