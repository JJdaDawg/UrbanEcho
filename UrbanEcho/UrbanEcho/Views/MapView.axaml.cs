using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Messages;

namespace UrbanEcho
{
    public partial class MapView : UserControl
    {
        public MapView()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<PickDestinationMessage>(this, (r, m) =>
            {
                bool picking = m.Vehicle is not null;
                MyMapControl.Cursor = picking ? new Cursor(StandardCursorType.Cross) : new Cursor(StandardCursorType.Arrow);

                if (picking)
                {
                    WeakReferenceMessenger.Default.Send(new ShowToastMessage("Click on the map to set a destination"));
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new HideToastMessage());
                }
            });

            WeakReferenceMessenger.Default.Register<DestinationPickedMessage>(this, (r, m) =>
            {
                MyMapControl.Cursor = new Cursor(StandardCursorType.Arrow);
                WeakReferenceMessenger.Default.Send(new HideToastMessage());
            });

            WeakReferenceMessenger.Default.Register<ShowToastMessage>(this, (r, m) =>
            {
                ToastText.Text = m.Text;
                ToastBanner.IsVisible = true;
            });

            WeakReferenceMessenger.Default.Register<HideToastMessage>(this, (r, m) => ToastBanner.IsVisible = false);
        }
    }
}