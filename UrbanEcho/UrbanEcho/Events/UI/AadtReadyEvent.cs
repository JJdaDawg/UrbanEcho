using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Messages;

namespace UrbanEcho.Events.UI
{
    /// <summary>
    /// Sets if the road network has AADT values ready.
    /// </summary>
    public class AadtReadyEvent : IEventForUI
    {
        private readonly bool _hasRealAadt;

        public AadtReadyEvent(bool hasRealAadt) => _hasRealAadt = hasRealAadt;

        public void Run()
        {
            WeakReferenceMessenger.Default.Send(new AadtReadyMessage(_hasRealAadt));
        }
    }
}