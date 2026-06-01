using RainGutter.Api.Dtos;
using RainGutter.Api.Enums;
using RainGutter.Api.Models;
using RainGutter.Api.Services;

namespace RainGutter.Tests;

public class PricingServiceTests
{
    private readonly PricingService _svc = new();

    private static PricingConfig DefaultConfig() => new()
    {
        MinimumMeters = 10,
        DownspoutPricePerPoint = 500,
        HeightSurchargePercent = 20,
        RemovalPricePerMeter = 60,
        SurveyFee = 1000
    };

    private static GutterProduct Stainless6() => new()
    {
        Material = Material.Stainless, SizeInches = 6, PricePerMeter = 850
    };

    private static GutterProduct Galvanized4() => new()
    {
        Material = Material.Galvanized, SizeInches = 4, PricePerMeter = 400
    };

    [Fact]
    public void BelowMinimum_BillsAsMinimumMeters()
    {
        var req = new EstimateRequest(8, 0, 1, false, null);
        var result = _svc.Calculate(req, DefaultConfig(), Stainless6(), null);

        // baseGutter = 10 * 850 = 8500
        Assert.Equal(8500, result.Total);
        Assert.Contains(result.Breakdown, b => b.Label.Contains("คิดราคาขั้นต่ำ"));
    }

    [Fact]
    public void ExactMinimum_NoMinimumNote()
    {
        var req = new EstimateRequest(10, 0, 1, false, null);
        var result = _svc.Calculate(req, DefaultConfig(), Stainless6(), null);

        Assert.Equal(8500, result.Total);
        Assert.DoesNotContain(result.Breakdown, b => b.Label.Contains("คิดราคาขั้นต่ำ"));
    }

    [Fact]
    public void AboveMinimum_UsesActualMeters()
    {
        var req = new EstimateRequest(15, 0, 1, false, null);
        var result = _svc.Calculate(req, DefaultConfig(), Galvanized4(), null);

        // baseGutter = 15 * 400 = 6000
        Assert.Equal(6000, result.Total);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    public void HeightSurcharge_NoSurchargeUpToTwoFloors(int floors, decimal expectedSurcharge)
    {
        var req = new EstimateRequest(10, 0, floors, false, null);
        var result = _svc.Calculate(req, DefaultConfig(), Galvanized4(), null);

        // base = 10 * 400 = 4000, no surcharge
        Assert.Equal(4000 + expectedSurcharge, result.Total);
        Assert.DoesNotContain(result.Breakdown, b => b.Label.Contains("ความสูง"));
    }

    [Fact]
    public void HeightSurcharge_ThreeFloors_Adds20Percent()
    {
        var req = new EstimateRequest(10, 0, 3, false, null);
        var result = _svc.Calculate(req, DefaultConfig(), Galvanized4(), null);

        // base = 4000, height = 4000 * 0.2 = 800
        Assert.Equal(4800, result.Total);
        Assert.Contains(result.Breakdown, b => b.Label.Contains("ความสูง"));
    }

    [Fact]
    public void DownspoutFee_CalculatesCorrectly()
    {
        var req = new EstimateRequest(10, 3, 1, false, null);
        var result = _svc.Calculate(req, DefaultConfig(), Galvanized4(), null);

        // base = 4000, downspout = 3 * 500 = 1500
        Assert.Equal(5500, result.Total);
        Assert.Contains(result.Breakdown, b => b.Label.Contains("ท่อน้ำลง 3 จุด"));
    }

    [Fact]
    public void RemovalFee_WhenEnabled_UsesActualLength()
    {
        // removal uses actual LengthMeters (8m), not effectiveMeters (10m)
        var req = new EstimateRequest(8, 0, 1, true, null);
        var result = _svc.Calculate(req, DefaultConfig(), Stainless6(), null);

        // base = 10 * 850 = 8500, removal = 8 * 60 = 480
        Assert.Equal(8980, result.Total);
        Assert.Contains(result.Breakdown, b => b.Label.Contains("รื้อถอน"));
    }

    [Fact]
    public void RemovalFee_WhenDisabled_NotCharged()
    {
        var req = new EstimateRequest(10, 0, 1, false, null);
        var result = _svc.Calculate(req, DefaultConfig(), Galvanized4(), null);

        Assert.DoesNotContain(result.Breakdown, b => b.Label.Contains("รื้อถอน"));
    }

    [Fact]
    public void TravelSurcharge_AddedFromZone()
    {
        var zone = new ServiceZone { Name = "ต่างจังหวัด", TravelSurcharge = 500 };
        var req = new EstimateRequest(10, 0, 1, false, zone.Id);
        var result = _svc.Calculate(req, DefaultConfig(), Galvanized4(), zone);

        // base = 4000, travel = 500
        Assert.Equal(4500, result.Total);
        Assert.Contains(result.Breakdown, b => b.Label.Contains("ค่าเดินทาง"));
    }

    [Fact]
    public void ZoneWithZeroSurcharge_NoTravelLineItem()
    {
        var zone = new ServiceZone { Name = "พื้นที่หลัก", TravelSurcharge = 0 };
        var req = new EstimateRequest(10, 0, 1, false, zone.Id);
        var result = _svc.Calculate(req, DefaultConfig(), Galvanized4(), zone);

        Assert.DoesNotContain(result.Breakdown, b => b.Label.Contains("ค่าเดินทาง"));
    }

    [Fact]
    public void SpecExample_Stainless6_8m_2Downspouts_RemoveOld_2Floors_Total9980()
    {
        // Stainless 6", 8m input, 2 downspouts, removeOld=true, floors=2 → 9980
        var req = new EstimateRequest(8, 2, 2, true, null);
        var result = _svc.Calculate(req, DefaultConfig(), Stainless6(), null);

        // base(min10) = 10*850 = 8500, height(floors≤2) = 0, downspout = 2*500 = 1000, removal = 8*60 = 480
        Assert.Equal(9980, result.Total);
    }

    [Fact]
    public void Disclaimer_AlwaysPresent()
    {
        var req = new EstimateRequest(10, 0, 1, false, null);
        var result = _svc.Calculate(req, DefaultConfig(), Galvanized4(), null);

        Assert.False(string.IsNullOrEmpty(result.Disclaimer));
    }

    [Fact]
    public void Jitter_OffsetIsBetween150And300Metres()
    {
        double lat = 13.3, lng = 100.9;
        for (int i = 0; i < 50; i++)
        {
            var (aLat, aLng) = JitterHelper.Jitter(lat, lng);
            double dLat = (aLat - lat) * 111320.0;
            double dLng = (aLng - lng) * 111320.0 * Math.Cos(lat * Math.PI / 180);
            double dist = Math.Sqrt(dLat * dLat + dLng * dLng);
            Assert.InRange(dist, 149.0, 301.5);
        }
    }
}
