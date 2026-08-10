using Avalonia.Controls;
using Avalonia.Input;
using DfTools.Desktop.ViewModels;

namespace DfTools.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnWindowKeyDown;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        // Ctrl + 1: Switch to SQL Formatter
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.D1)
        {
            vm.SelectTab(0);
            e.Handled = true;
        }
        // Ctrl + 2: Switch to Settings
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.D2)
        {
            vm.SelectTab(1);
            e.Handled = true;
        }
        // Esc: Close command palette if open
        else if (e.Key == Key.Escape && vm.IsCommandPaletteOpen)
        {
            vm.IsCommandPaletteOpen = false;
            e.Handled = true;
        }
    }
}