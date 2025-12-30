using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HotReloadDemo.Controls;

internal sealed partial class ToDoItemControl : UserControl
{
    public ToDoItemControl()
    {
        InitializeComponent();
        InitializeComponentState();
    }

    private void InitializeComponentState()
        => Debug.WriteLine("Initializing {0}#{1}...", this, GetHashCode());

    private void CheckBox_Click(object? sender, RoutedEventArgs e)
        => Debug.WriteLine("Clicked {0}#{1}", this, GetHashCode());
}
