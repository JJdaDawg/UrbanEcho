using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            intersection.ChangeSignalType(signalType);
        }
    }
}
