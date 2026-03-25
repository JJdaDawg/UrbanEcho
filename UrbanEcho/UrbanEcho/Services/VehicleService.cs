using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.Messages;
using UrbanEcho.Models;

namespace UrbanEcho.Services
{
    public interface IVehicleService
    {
        void Respawn(VehicleReadOnly vehicle);

        void ToggleStop(VehicleReadOnly vehicle);

        void TrackVehicle(VehicleReadOnly vehicle);

        void PickDestination(VehicleReadOnly vehicle);

        void CancelPickDestination();

        void StopTracking();

        void ShowPath(VehicleReadOnly vehicle);

        void HidePath();
    }

    public class VehicleService : IVehicleService
    {
        public void Respawn(VehicleReadOnly vehicle)
        {
            WeakReferenceMessenger.Default.Send(new LogMessage($"Vehicle {vehicle.Id()} despawned", LogSource.System));
            EventQueueForSim.Instance.Add(new ResetPositionEvent(vehicle));
        }

        public void ToggleStop(VehicleReadOnly vehicle)
        {
            bool stopCommand = !vehicle.IsForceStopped();
            EventQueueForSim.Instance.Add(new ForceStopEvent(vehicle, stopCommand));
            WeakReferenceMessenger.Default.Send(new LogMessage($"Vehicle {vehicle.Id()} {(stopCommand ? "stopped" : "started")}", LogSource.System));
        }

        public void TrackVehicle(VehicleReadOnly vehicle)
        {
            WeakReferenceMessenger.Default.Send(new TrackVehicleMessage(vehicle));
            WeakReferenceMessenger.Default.Send(new LogMessage($"Tracking vehicle {vehicle.Id()}", LogSource.System));
        }

        public void PickDestination(VehicleReadOnly vehicle)
        {
            WeakReferenceMessenger.Default.Send(new PickDestinationMessage(vehicle));
            WeakReferenceMessenger.Default.Send(new LogMessage($"Picking destination for vehicle {vehicle.Id()}", LogSource.System));
        }

        public void CancelPickDestination()
        {
            WeakReferenceMessenger.Default.Send(new PickDestinationMessage(null));
        }

        public void StopTracking()
        {
            WeakReferenceMessenger.Default.Send(new TrackVehicleMessage(null));
            WeakReferenceMessenger.Default.Send(new LogMessage("Stopped tracking vehicle", LogSource.System));
        }

        public void ShowPath(VehicleReadOnly vehicle)
        {
            WeakReferenceMessenger.Default.Send(new ShowVehiclePathMessage(vehicle));
            WeakReferenceMessenger.Default.Send(new LogMessage($"Showing path for vehicle {vehicle.Id()}", LogSource.System));
        }

        public void HidePath()
        {
            WeakReferenceMessenger.Default.Send(new HideVehiclePathMessage());
        }
    }
}