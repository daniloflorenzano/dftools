using System.Collections.Generic;

namespace DfTools.Desktop.Models;

public class AppSettings
{
    public ThemeSettings Theme { get; set; } = new();
    public TextSettings Text { get; set; } = new();
    public KeyboardSettings Keyboard { get; set; } = new();
    public SqlFormatterSettings SqlFormatter { get; set; } = new();
    public DiffToolSettings DiffTool { get; set; } = new();
}

public class ThemeSettings
{
    public string BackgroundColor { get; set; } = "#000000";
    public string SurfaceColor { get; set; } = "#121212";
    public string PanelBackgroundColor { get; set; } = "#181818";
    public string BorderColor { get; set; } = "#333333";
    public string PrimaryTextColor { get; set; } = "#FFFFFF";
    public string SecondaryTextColor { get; set; } = "#888888";
    public string AccentColor { get; set; } = "#FFFFFF";
    public string ButtonBackgroundColor { get; set; } = "#222222";
    public string ButtonHoverColor { get; set; } = "#333333";
    public string ButtonPressedColor { get; set; } = "#444444";
    public string ButtonTextColor { get; set; } = "#FFFFFF";
    public string ActiveTabBackgroundColor { get; set; } = "#FFFFFF";
    public string ActiveTabTextColor { get; set; } = "#000000";
    public string InactiveTabBackgroundColor { get; set; } = "#121212";
    public string InactiveTabTextColor { get; set; } = "#888888";
    public string EditorBackgroundColor { get; set; } = "#080808";
    public string StatusOkColor { get; set; } = "#00FF66";
    public string StatusErrorColor { get; set; } = "#FF4444";
    public string DiffInsertedBgColor { get; set; } = "#1e3a1e";
    public string DiffInsertedFgColor { get; set; } = "#4caf50";
    public string DiffDeletedBgColor { get; set; } = "#3a1e1e";
    public string DiffDeletedFgColor { get; set; } = "#f44336";
    public string DiffModifiedBgColor { get; set; } = "#3a331e";
    public string DiffModifiedFgColor { get; set; } = "#ffb74d";
    public string DiffSubPieceInsertedBgColor { get; set; } = "#2e6f30";
    public string DiffSubPieceDeletedBgColor { get; set; } = "#6f2e2e";
    public string FontFamily { get; set; } = "DejaVu Sans Mono, Consolas, Courier New, Monospace";
    public int FontSize { get; set; } = 13;
    public int EditorFontSize { get; set; } = 14;
}

public class TextSettings
{
    public string AppTitle { get; set; } = "DFTOOLS [CLASSIC DESKTOP]";
    public string StatusReady { get; set; } = "READY";
    public string StatusFormatted { get; set; } = "SQL FORMATTED SUCCESSFULLY";
    public string StatusDiffed { get; set; } = "TEXT DIFF COMPLETED";
    public string StatusCopied { get; set; } = "COPIED TO CLIPBOARD";
    public string StatusCleared { get; set; } = "EDITOR CLEARED";
    public string StatusSettingsSaved { get; set; } = "SETTINGS SAVED & APPLIED";
    public string StatusSettingsError { get; set; } = "INVALID JSON IN SETTINGS";
    public string FormatButtonText { get; set; } = "[F5] FORMAT";
    public string CompareButtonText { get; set; } = "[F5] COMPARE DIFF";
    public string CopyButtonText { get; set; } = "[Ctrl+C / F6] COPY";
    public string ClearButtonText { get; set; } = "[F7] CLEAR";
    public string CommandPaletteHeader { get; set; } = "COMMAND PALETTE (CTRL+P / F1)";
}

public class KeyboardSettings
{
    public string CommandPaletteHotkey { get; set; } = "Ctrl+P / F1";
    public string SwitchToolHotkey { get; set; } = "Ctrl+1..9";
    public string FormatHotkey { get; set; } = "F5";
    public string CopyHotkey { get; set; } = "F6";
    public string ClearHotkey { get; set; } = "F7";
}

public class SqlFormatterSettings
{
    public string IndentString { get; set; } = "  ";
    public string DefaultQuery { get; set; } = "SELECT u.id, u.name, count(o.id) as order_count FROM users u LEFT JOIN orders o ON o.user_id = u.id WHERE u.status = 'active' GROUP BY u.id, u.name HAVING count(o.id) > 5 ORDER BY order_count DESC;";
}

public class DiffToolSettings
{
    public string DefaultOldText { get; set; } = "Line 1: Original text\nLine 2: Hello world\nLine 3: Unchanged line";
    public string DefaultNewText { get; set; } = "Line 1: Original text\nLine 2: Hello earth\nLine 3: Unchanged line\nLine 4: Added new line";
}
