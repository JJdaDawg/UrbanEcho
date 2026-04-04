using CommunityToolkit.Mvvm.Messaging;
using Mapsui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanEcho.Events.Sim;
using UrbanEcho.FileManagement;
using UrbanEcho.Messages;
using UrbanEcho.Models;
using static UrbanEcho.Models.RoadIntersection;

namespace UrbanEcho.Services
{
    public interface IIntersectionService
    {
        void ShowIntersectionOverlay(RoadIntersection intersection);

        void HideIntersectionOverlay();

        void SetSignalType(RoadIntersection intersection, SignalType signalType);

        void SetStopSignAssignment(RoadIntersection intersection, List<(EdgeTrafficRule edge, bool hasStopSign)> assignments);
    }

    public class IntersectionService : IIntersectionService
    {
        public void ShowIntersectionOverlay(RoadIntersection intersection)
        {
            WeakReferenceMessenger.Default.Send(new ShowIntersectionOverlayMessage(intersection));
            WeakReferenceMessenger.Default.Send(new LogMessage($"Showing overlay for intersection {intersection.Name}", LogSource.System));
        }

        public void HideIntersectionOverlay()
        {
            WeakReferenceMessenger.Default.Send(new HideIntersectionOverlayMessage());
        }

        public void SetSignalType(RoadIntersection intersection, SignalType signalType)
        {
            EventQueueForSim.Instance.Add(new ChangeSignalTypeEvent(intersection, signalType));
        }

        public void SetStopSignAssignment(RoadIntersection intersection, List<(EdgeTrafficRule edge, bool hasStopSign)> assignments)
        {
            EventQueueForSim.Instance.Add(new ApplyStopSignAssignmentsEvent(intersection, assignments));
        }
    }
}