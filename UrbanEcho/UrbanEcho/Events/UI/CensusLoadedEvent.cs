using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Messages;

namespace UrbanEcho.Events.UI
{
    /// <summary>
    /// Sets if the census data has loaded.
    /// </summary>
    public class CensusLoadedEvent : IEventForUI
    {
        public void Run()
        {
            WeakReferenceMessenger.Default.Send(new CensusLoadedMessage());
        }
    }
}