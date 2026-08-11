using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DfTools.Desktop.Models;
using DfTools.Desktop.Services;
using DfTools.Diff;
using DfTools.Sql;

namespace DfTools.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly QueryFormatter _sqlFormatter = new();
    private readonly ITextDiffer _textDiffer = new TextDiffer();

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

    // Text Differ Tool
    [ObservableProperty]
    private string _diffOldInput = string.Empty;

    [ObservableProperty]
    private string _diffNewInput = string.Empty;

    [ObservableProperty]
    private SideBySideDiffResult? _diffResult;

    [ObservableProperty]
    private bool _isDiffEditMode = true;

    // Command Palette Overlay
    [ObservableProperty]
    private bool _isCommandPaletteOpen;

    public List<CommandItem> AllCommands { get; } = new();

    public MainViewModel()
    {
        _settingsService = new SettingsService();
        _settings = _settingsService.LoadSettings();

        SqlInput = Settings.SqlFormatter.DefaultQuery;
        DiffOldInput = Settings.DiffTool.DefaultOldText;
        DiffNewInput = Settings.DiffTool.DefaultNewText;
        StatusMessage = Settings.Text.StatusReady;

        InitializeCommands();
        CompareDiff();
    }

    private void InitializeCommands()
    {
        AllCommands.Add(new CommandItem
        {
            Shortcut = "F5",
            Title = "Execute / Process",
            Description = "Format SQL query or compare text diff",
            Action = ExecuteCurrentTool
        });

        AllCommands.Add(new CommandItem
        {
            Shortcut = "F6",
            Title = "Copy Output",
            Description = "Copy formatted result or diff summary to clipboard",
            Action = () => _ = CopyCurrentToolOutput()
        });

        AllCommands.Add(new CommandItem
        {
            Shortcut = "F7",
            Title = "Clear Input",
            Description = "Clear input text areas for active tool",
            Action = ClearCurrentToolInput
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
    public void ExecuteCurrentTool()
    {
        if (SelectedTabIndex == 0)
        {
            FormatSql();
        }
        else if (SelectedTabIndex == 1)
        {
            CompareDiff();
        }
    }

    [RelayCommand]
    public async Task CopyCurrentToolOutput()
    {
        if (SelectedTabIndex == 0)
        {
            await CopySql();
        }
        else if (SelectedTabIndex == 1)
        {
            await CopyDiffSummary();
        }
    }

    [RelayCommand]
    public void ClearCurrentToolInput()
    {
        if (SelectedTabIndex == 0)
        {
            ClearSql();
        }
        else if (SelectedTabIndex == 1)
        {
            ClearDiff();
        }
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
    public void CompareDiff()
    {
        DiffResult = _textDiffer.CompareSideBySide(DiffOldInput, DiffNewInput);
        IsDiffEditMode = false;
        StatusMessage = Settings.Text.StatusDiffed;
    }

    [RelayCommand]
    public void EditDiffInputs()
    {
        IsDiffEditMode = true;
        StatusMessage = Settings.Text.StatusReady;
    }

    [RelayCommand]
    public void ClearDiff()
    {
        DiffOldInput = string.Empty;
        DiffNewInput = string.Empty;
        DiffResult = _textDiffer.CompareSideBySide(string.Empty, string.Empty);
        IsDiffEditMode = true;
        StatusMessage = Settings.Text.StatusCleared;
    }

    [RelayCommand]
    public async Task CopyDiffSummary()
    {
        if (DiffResult != null && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                var summary = $"DFTOOLS TEXT DIFF RESULT:\nHas Differences: {DiffResult.HasDifferences}\nOld Lines: {DiffResult.OldText.Lines.Count}\nNew Lines: {DiffResult.NewText.Lines.Count}";
                await topLevel.Clipboard.SetTextAsync(summary);
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