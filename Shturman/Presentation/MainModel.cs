using Windows.Devices.Geolocation;
using System;
using Windows.Foundation;

namespace Shturman.Presentation;

public partial record MainModel
{
    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig>? appInfo,
        INavigator navigator)
    {
        Task.Run(GetGeoLocation);
    }

    private Windows.Devices.Geolocation.Geolocator Geolocation = new();

    public List<Geoposition> path = new List<Geoposition>();
    private static bool _activatePath;
    public static bool ActivatePath = false;

    public void GetGeoLocation()
    {
        var access = Geolocator.RequestAccessAsync();
        Geolocation.DesiredAccuracy = PositionAccuracy.High;
        if (access.GetResults() == GeolocationAccessStatus.Allowed)
        {
            while (_activatePath)
            {
                path.Add(Geolocation.GetGeopositionAsync().GetResults() ?? throw new NullReferenceException());
                Thread.Sleep(1000);
            }

            path.Reverse();
        }
    }

    public Geocoordinate GetSingleGeoLocation()
    {
        var access = Geolocator.RequestAccessAsync();
        Geolocation.DesiredAccuracy = PositionAccuracy.High;
        return Geolocation.GetGeopositionAsync().GetResults().Coordinate;
    }

    public static class BearingCalculator
    {
        public static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            // Convert degrees to radians
            double lat1Rad = DegreesToRadians(lat1);
            double lat2Rad = DegreesToRadians(lat2);
            double deltaLonRad = DegreesToRadians(lon2 - lon1);

            // Calculate bearing components
            double y = Math.Sin(deltaLonRad) * Math.Cos(lat2Rad);
            double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad)
                       - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(deltaLonRad);

            // Calculate initial bearing
            double bearingRadians = Math.Atan2(y, x);
            double bearingDegrees = RadiansToDegrees(bearingRadians);

            // Normalize to 0-360 degrees
            return (bearingDegrees + 360) % 360;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }
    }
}
