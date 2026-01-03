namespace ClipCore.Core.Entities;

public class ThemeSettings
{
    // Dark Mode Colors
    public string? DarkPrimary { get; set; }
    public string? DarkSecondary { get; set; }
    public string? DarkBackground { get; set; }
    public string? DarkSurface { get; set; }
    public string? DarkText { get; set; }
    public string? DarkBtnStart { get; set; }
    public string? DarkBtnEnd { get; set; }
    public string? DarkBrandText { get; set; }

    // Light Mode Colors
    public string? LightPrimary { get; set; }
    public string? LightSecondary { get; set; }
    public string? LightBackground { get; set; }
    public string? LightSurface { get; set; }
    public string? LightText { get; set; }
    public string? LightBtnStart { get; set; }
    public string? LightBtnEnd { get; set; }
    public string? LightBrandText { get; set; }

    // Brand Assets
    public string? LogoUrl { get; set; }
    public string? WatermarkUrl { get; set; }
    public string? IconUrl { get; set; }
}
