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
    private static bool _ap;

    private static bool ActivatePath
    {
        get => _ap;
        set { _ap = value; }
    }

    public static bool _button;

    public static bool Button
    {
        get => _button;
        set
        {
            _button = value;
            if (value)
            {
                if (ActivatePath)
                {
                    ActivatePath = false;
                    CurrentText = Text;
                }
                else
                {
                    ActivatePath = true;
                    CurrentText = Text2;
                }
            }
        }
    }

    public const string Text = "Я заблудился";
    public const string Text2 = "Отменить";
    public static string CurrentText = Text;

    public void ButtonActivated()
    {
        path.Reverse();
    }

    public void GetGeoLocation()
    {
        var access = Geolocator.RequestAccessAsync();
        Geolocation.DesiredAccuracy = PositionAccuracy.High;
        if (access.GetResults() == GeolocationAccessStatus.Allowed)
        {
            while (!ActivatePath)
            {
                path.Add(Geolocation.GetGeopositionAsync().GetResults() ?? throw new NullReferenceException());
                Thread.Sleep(1000);
            }
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
