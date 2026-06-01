using System.Security.Cryptography;

namespace RainGutter.Api.Services;

public static class JitterHelper
{
    public static (double approxLat, double approxLng) Jitter(double lat, double lng)
    {
        int metres = RandomNumberGenerator.GetInt32(150, 301);
        double bearing = Random.Shared.NextDouble() * 2 * Math.PI;
        double dLat = (metres * Math.Cos(bearing)) / 111320.0;
        double dLng = (metres * Math.Sin(bearing)) / (111320.0 * Math.Cos(lat * Math.PI / 180));
        return (Math.Round(lat + dLat, 5), Math.Round(lng + dLng, 5));
    }
}
