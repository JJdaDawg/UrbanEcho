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
        RoadIntersection? MapIntersection(IFeature feature);

        VehicleReadOnly? MapVehicle(IFeature feature);
    }

    public class MapFeatureService : IMapFeatureService
    {
        public RoadIntersection? MapIntersection(IFeature feature)
        {
            var rawId = feature["OBJECTID"]?.ToString();

            var intersections = SimManager.Instance.RoadIntersections.ToList();
            var simIntersection = intersections.FirstOrDefault(i => i.Feature["OBJECTID"]?.ToString() == rawId);

            if (simIntersection is null)
            {
                WeakReferenceMessenger.Default.Send(new LogMessage($"Intersection {rawId} not found", LogSource.Map));
                return null;
            }

            return simIntersection;
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