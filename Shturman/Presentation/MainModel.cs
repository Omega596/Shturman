using Windows.Devices.Geolocation;
using System;
using Windows.Devices.Sensors;
using Windows.Foundation;
using SkiaSharp.Views.Windows;

namespace Shturman.Presentation;

public partial record MainModel
{
    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig>? appInfo,
        INavigator navigator)
    {
        Geolocation = new Geolocator();
        switch (Geolocator.RequestAccessAsync().GetResults())
        {
            case GeolocationAccessStatus.Allowed:
            {
                Geolocation.PositionChanged += GeolocationOnPositionChanged;
                break; 
            } 
            case GeolocationAccessStatus.Denied:
                throw new Exception("Access denied.");
            case GeolocationAccessStatus.Unspecified:
                throw new Exception("Unspecified.");
        }
        var __ = Geolocation.GetGeopositionAsync();
        Geolocation.ReportInterval = 1000;
        Geolocation.DesiredAccuracyInMeters = 1;
        compass.ReadingChanged += CompassOnReadingChanged;
        
    }

    private void GeolocationOnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
    {
        currentPosition = args.Position;
        if (ActivatePath)
        {
            if (Calculator.CalculateDistance(currentPosition, path[0]) < 10)
            {
                path.Remove(path[0]);
            }

            TargetUICompass.UpdateAsync(c => c.ChangeDegrees(Calculator.CalculateBearing(
                currentPosition.Coordinate.Latitude, currentPosition.Coordinate.Longitude,
                path[0].Coordinate.Latitude, path[0].Coordinate.Longitude)));
        }
        else
        {
            path.Add(currentPosition);
        }
    }

    private void CompassOnReadingChanged(Compass sender, CompassReadingChangedEventArgs args)
    {
        if (ActivatePath)
        {
            ChangeDegrees(compassFilter.Update((double)args.Reading.HeadingTrueNorth));     
        }
    }

    private CompassFilter compassFilter = new(9);
    private Geoposition currentPosition;

    public Compass compass = Compass.GetDefault();
    private Geolocator Geolocation = new();

    public List<Geoposition> path = new();
    private bool _activatePath;
    public bool ActivatePath
    {
        get => _activatePath;
        set
        {
            _activatePath = value;
            ReversePath();
        }
    }
    public IState<Button> Button => State.Value(this, () => new Button("Я заблудился"));
    
    public IState<UICompass> UICompass => State.Value(this, () => new UICompass(0));
    
    public IState<UICompass> TargetUICompass => State.Value(this, () => new UICompass(0));

    public void ReversePath()
    {
        path.Reverse();
    }

    public ValueTask ChangeText()
    {
        ActivatePath = !ActivatePath;
        return Button.UpdateAsync(c => c.ChangeText());
    }

    public ValueTask ChangeDegrees(double degrees)
        => UICompass.UpdateAsync(c => c.ChangeDegrees(degrees));

    public Geocoordinate GetSingleGeoLocation()
    {
        var access = Geolocator.RequestAccessAsync();
        Geolocation.DesiredAccuracy = PositionAccuracy.High;
        return Geolocation.GetGeopositionAsync().GetResults().Coordinate;
    }

    public static class Calculator
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
        public static double CalculateDistance(Geoposition loc1, Geoposition loc2)
        {
            const double EarthRadiusMeters = 6371e3; // Earth radius in meters

            double lat1Rad = DegreesToRadians(loc1.Coordinate.Latitude);
            double lon1Rad = DegreesToRadians(loc1.Coordinate.Longitude);
            double lat2Rad = DegreesToRadians(loc2.Coordinate.Latitude);
            double lon2Rad = DegreesToRadians(loc2.Coordinate.Longitude);

            double deltaLat = lat2Rad - lat1Rad;
            double deltaLon = lon2Rad - lon1Rad;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                           Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                           Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusMeters * c; // Distance in meters
        }
    }
}

public class CompassFilter
{
    private Queue<double> _angleBuffer;
    private int _windowSize;

    public CompassFilter(int windowSize)
    {
        _windowSize = windowSize;
        _angleBuffer = new Queue<double>(windowSize);
    }

    public double Update(double newAngle)
    {
        // Add the new angle to the buffer
        _angleBuffer.Enqueue(newAngle);

        // Remove the oldest angle if buffer exceeds window size
        if (_angleBuffer.Count > _windowSize)
            _angleBuffer.Dequeue();

        // Calculate sum of sines and cosines
        double sumSin = 0, sumCos = 0;
        foreach (var angle in _angleBuffer)
        {
            double radians = angle * Math.PI / 180.0;
            sumSin += Math.Sin(radians);
            sumCos += Math.Cos(radians);
        }

        // Compute circular mean
        int count = _angleBuffer.Count;
        double avgSin = sumSin / count;
        double avgCos = sumCos / count;
        double smoothedRadians = Math.Atan2(avgSin, avgCos);

        // Convert to degrees and normalize to 0-360
        double smoothedDegrees = smoothedRadians * (180.0 / Math.PI);
        return (smoothedDegrees + 360) % 360;
    }
}
