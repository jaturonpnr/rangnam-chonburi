namespace RainGutter.Api.Models;

public class PricingConfig
{
    public int Id { get; set; } = 1;
    public decimal MinimumMeters { get; set; } = 10;
    public decimal DownspoutPricePerPoint { get; set; } = 500;
    public decimal HeightSurchargePercent { get; set; } = 20;
    public decimal RemovalPricePerMeter { get; set; } = 60;
    public decimal SurveyFee { get; set; } = 1000;
}
