using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using UrbanEcho.Messages;
using UrbanEcho.Models;

namespace UrbanEcho.ViewModels.Properties
{
    public partial class SpawnerPropertiesViewModel : ObservableObject, IPropertiesViewModel
    {
        private readonly SpawnPoint _spawnPoint;

        public string Title => "Spawner";
        public string Subtitle => $"ID: {_spawnPoint.Id[..8]}";

        public string SpawnPointId => _spawnPoint.Id;
        public double X => _spawnPoint.X;
        public double Y => _spawnPoint.Y;
        public int NearestNodeId => _spawnPoint.NearestNodeId;

        [ObservableProperty]
        private int _vehiclesPerMinute;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _isMoving;

        public RelayCommand DeleteCommand { get; }
        public RelayCommand MoveCommand { get; }

        public SpawnerPropertiesViewModel(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint;
            _vehiclesPerMinute = spawnPoint.VehiclesPerMinute;

            DeleteCommand = new RelayCommand(() =>
            {
                WeakReferenceMessenger.Default.Send(new DeleteSpawnerMessage(_spawnPoint));
            });

            MoveCommand = new RelayCommand(() =>
            {
                IsMoving = !IsMoving;
                if (IsMoving)
                    WeakReferenceMessenger.Default.Send(new MoveSpawnerMessage(_spawnPoint));
                else
                    WeakReferenceMessenger.Default.Send(new CancelMoveSpawnerMessage());
            });

            WeakReferenceMessenger.Default.Register<SpawnerMovedMessage>(this, (r, m) => IsMoving = false);
        }

        partial void OnVehiclesPerMinuteChanged(int value)
        {
            if (!IsEditing) return;
            if (value < 1) value = 1;
            _spawnPoint.VehiclesPerMinute = value;
        }

        public void UpdatePropertyView()
        {
        }
    }
}
