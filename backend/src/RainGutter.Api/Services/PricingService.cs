using RainGutter.Api.Dtos;
using RainGutter.Api.Enums;
using RainGutter.Api.Models;

namespace RainGutter.Api.Services;

public class PricingService : IPricingService
{
    public EstimateResult Calculate(EstimateRequest req, PricingConfig config, GutterProduct product, ServiceZone? zone)
    {
        var items = new List<BreakdownItem>();
        bool usedMinimum = req.LengthMeters < config.MinimumMeters;
        var effectiveMeters = usedMinimum ? config.MinimumMeters : req.LengthMeters;

        var materialLabel = product.Material == Material.Galvanized ? "สังกะสี" : "สแตนเลส";
        var gutterLabel = usedMinimum
            ? $"ค่าราง{materialLabel} {product.SizeInches}\" (คิดราคาขั้นต่ำ {config.MinimumMeters} ม.)"
            : $"ค่าราง{materialLabel} {product.SizeInches}\" {req.LengthMeters} ม.";

        var baseGutter = effectiveMeters * product.PricePerMeter;
        items.Add(new BreakdownItem(gutterLabel, baseGutter));

        decimal heightFee = 0;
        if (req.Floors > 2)
        {
            heightFee = baseGutter * config.HeightSurchargePercent / 100;
            items.Add(new BreakdownItem($"ค่าความสูงอาคาร {req.Floors} ชั้น (+{config.HeightSurchargePercent}%)", heightFee));
        }

        decimal downspoutFee = 0;
        if (req.DownspoutCount > 0)
        {
            downspoutFee = req.DownspoutCount * config.DownspoutPricePerPoint;
            items.Add(new BreakdownItem($"ท่อน้ำลง {req.DownspoutCount} จุด", downspoutFee));
        }

        decimal removalFee = 0;
        if (req.RemoveOld)
        {
            removalFee = req.LengthMeters * config.RemovalPricePerMeter;
            items.Add(new BreakdownItem($"รื้อถอนของเดิม {req.LengthMeters} ม.", removalFee));
        }

        decimal travelFee = zone?.TravelSurcharge ?? 0;
        if (travelFee > 0)
        {
            items.Add(new BreakdownItem($"ค่าเดินทาง ({zone!.Name})", travelFee));
        }

        var total = baseGutter + heightFee + downspoutFee + removalFee + travelFee;

        return new EstimateResult(
            items,
            total,
            "ราคาประเมินเบื้องต้น ราคาจริงยืนยันหลังสำรวจหน้างาน"
        );
    }
}
