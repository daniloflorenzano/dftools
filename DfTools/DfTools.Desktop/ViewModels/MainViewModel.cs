using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DfTools.Desktop.Models;
using DfTools.Desktop.Services;
using DfTools.Sql;

namespace DfTools.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly QueryFormatter _sqlFormatter = new();

    [ObservableProperty]
    private AppSettings _settings;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // SQL Formatter Tool
    [ObservableProperty]
    private string _sqlInput = string.Empty;

    [ObservableProperty]
    private string _sqlOutput = string.Empty;

    // Command Palette Overlay
    [ObservableProperty]
    private bool _isCommandPaletteOpen;

    public List<CommandItem> AllCommands { get; } = new();

    public MainViewModel()
    {
        _settingsService = new SettingsService();
        _settings = _settingsService.LoadSettings();

        SqlInput = Settings.SqlFormatter.DefaultQuery;
        StatusMessage = Settings.Text.StatusReady;

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        AllCommands.Add(new CommandItem
        {
            Shortcut = "F5",
            Title = "Format SQL",
            Description = "Format SQL query in the editor",
            Action = FormatSql
        });

        AllCommands.Add(new CommandItem
        {
            Shortcut = "F6",
            Title = "Copy Formatted SQL",
            Description = "Copy formatted result to clipboard",
            Action = () => _ = CopySql()
        });

        AllCommands.Add(new CommandItem
        {
            Shortcut = "F7",
            Title = "Clear Input SQL",
            Description = "Clear input text area",
            Action = ClearSql
        });

        AllCommands.Add(new CommandItem
        {
            Shortcut = "F1 / Ctrl+P",
            Title = "Toggle Command Palette",
            Description = "Open keyboard command launcher",
            Action = ToggleCommandPalette
        });
    }

    [RelayCommand]
    public void SelectTab(int index)
    {
        SelectedTabIndex = index;
        IsCommandPaletteOpen = false;
    }

    [RelayCommand]
    public void FormatSql()
    {
        if (string.IsNullOrWhiteSpace(SqlInput))
        {
            SqlOutput = string.Empty;
            StatusMessage = Settings.Text.StatusReady;
            return;
        }

        try
        {
            SqlOutput = _sqlFormatter.Format(SqlInput, Settings.SqlFormatter.IndentString);
            StatusMessage = Settings.Text.StatusFormatted;
        }
        catch (Exception ex)
        {
            SqlOutput = $"-- Error formatting SQL: {ex.Message}";
            StatusMessage = "FORMAT ERROR";
        }
    }

    [RelayCommand]
    public void ClearSql()
    {
        SqlInput = string.Empty;
        SqlOutput = string.Empty;
        StatusMessage = Settings.Text.StatusCleared;
    }

    [RelayCommand]
    public async Task CopySql()
    {
        if (!string.IsNullOrEmpty(SqlOutput) && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(SqlOutput);
                StatusMessage = Settings.Text.StatusCopied;
            }
        }
    }

    [RelayCommand]
    public void ToggleCommandPalette()
    {
        IsCommandPaletteOpen = !IsCommandPaletteOpen;
    }
}