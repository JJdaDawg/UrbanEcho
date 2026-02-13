using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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

        if (DataContext is MainViewModel vm)
        {
            vm.CurrentLogs.CollectionChanged -= LogItems_CollectionChanged;
            vm.CurrentLogs.CollectionChanged += LogItems_CollectionChanged;
        }
    }

    private void LogItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Scroll to end when new console log item is received
                var scrollViewer = ConsoleListBox.FindDescendantOfType<ScrollViewer>();
                scrollViewer?.ScrollToEnd();
            });
        }
    }
}
