using RainGutter.Api.Dtos;
using RainGutter.Api.Models;

namespace RainGutter.Api.Services;

public interface IPricingService
{
    EstimateResult Calculate(EstimateRequest req, PricingConfig config, GutterProduct product, ServiceZone? zone);
}
