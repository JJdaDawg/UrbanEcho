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
        void Despawn(Vehicle vehicle);
        void ToggleStop(Vehicle vehicle);
    }

    public class VehicleService : IVehicleService
    {
        public void Despawn(Vehicle vehicle)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage($"Vehicle {vehicle.VehicleUI.Id} despawned", LogSource.System));
            vehicle.ResetVehicleToNewPos();
        }

        public void ToggleStop(Vehicle vehicle)
        {
            vehicle.IsForceStopped = !vehicle.IsForceStopped;
            WeakReferenceMessenger.Default.Send(new LogMessage($"Vehicle {vehicle.VehicleUI.Id} {(vehicle.IsForceStopped ? "stopped" : "started")}", LogSource.System));
        }
    }
}
