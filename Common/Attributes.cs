using System;
using System.Reflection;

public class DisableAttribute : Attribute {}
public class HideAttribute : Attribute {}
public class HeaderAttribute : Attribute
{
    public string Header { get; }
    public HeaderAttribute(string header) => Header = header;
}
public class DescriptionAttribute : Attribute
{
    public string Description { get; }
    public DescriptionAttribute(string description) => Description = description;
}
public class TooltipAttribute : Attribute
{
    public string Tooltip { get; }
    public TooltipAttribute(string tooltip) => Tooltip = tooltip;
}