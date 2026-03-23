using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Messages;

namespace UrbanEcho.Events.UI
{
    public class CensusLoadedEvent : IEventForUI
    {
        public void Run()
        {
            WeakReferenceMessenger.Default.Send(new CensusLoadedMessage());
        }
    }
}
