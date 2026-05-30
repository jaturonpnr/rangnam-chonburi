namespace RainGutter.Api.Models;

public class ShopProfile
{
    public int Id { get; set; } = 1;
    public string ShopName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string LineOaLink { get; set; } = string.Empty;
    public int QuoteValidityDays { get; set; } = 30;
    public string QuoteFooterNote { get; set; } = string.Empty;
}
