using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using UrbanEcho.Sim;

namespace UrbanEcho.Services
{
    public interface IVehicleService
    {
        void Respawn(Vehicle vehicle);
        void ToggleStop(Vehicle vehicle);
        void TrackVehicle(Vehicle vehicle);
        void StopTracking();
    }

    public class VehicleService : IVehicleService
    {
        public void Respawn(Vehicle vehicle)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage($"Vehicle {vehicle.VehicleUI.Id} despawned", LogSource.System));
            vehicle.ResetVehicleToNewPos();
        }

        public void ToggleStop(Vehicle vehicle)
        {
            vehicle.IsForceStopped = !vehicle.IsForceStopped;
            WeakReferenceMessenger.Default.Send(new LogMessage($"Vehicle {vehicle.VehicleUI.Id} {(vehicle.IsForceStopped ? "stopped" : "started")}", LogSource.System));
        }

        public void TrackVehicle(Vehicle vehicle)
        {
            WeakReferenceMessenger.Default.Send(new TrackVehicleMessage(vehicle));
            WeakReferenceMessenger.Default.Send(new LogMessage($"Tracking vehicle {vehicle.VehicleUI.Id}", LogSource.System));
        }

        public void StopTracking()
        {
            WeakReferenceMessenger.Default.Send(new TrackVehicleMessage(null));
            WeakReferenceMessenger.Default.Send(new LogMessage("Stopped tracking vehicle", LogSource.System));
        }
    }
}
