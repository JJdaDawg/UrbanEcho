using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Specialized;
using UrbanEcho.ViewModels;

namespace UrbanEcho;

public partial class ConsolePanel : UserControl
{
    public ConsolePanel()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ConsoleViewModel cvm)
        {
            cvm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(ConsoleViewModel.CurrentLogs))
                    SubscribeToCollection(cvm.CurrentLogs);
            };
            SubscribeToCollection(cvm.CurrentLogs);
        }
    }

    private void SubscribeToCollection(INotifyCollectionChanged collection)
    {
        collection.CollectionChanged -= LogItems_CollectionChanged;
        collection.CollectionChanged += LogItems_CollectionChanged;
    }

    private void LogItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var scrollViewer = ConsoleTextBox.FindDescendantOfType<ScrollViewer>();
                scrollViewer?.ScrollToEnd();
            });
        }
    }
}