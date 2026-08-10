using System;
using System.Collections.Generic;

namespace DfTools.Desktop.Models;

public class CommandItem
{
    public string Shortcut { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Action Action { get; set; } = () => { };
}
