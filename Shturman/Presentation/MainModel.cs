#if __ANDROID__
using Android;
using Android.Gms.Common;
using Android.Util;
using Android.Gms.Location;
using Application = Android.App.Application;
using Android.OS;
using Android.Locations;
using AndroidX.Core.Content;
using Android.Content.PM;
using Android.Widget;
#endif
using System;
using System.Globalization;
using Windows.Devices.Geolocation;
using Windows.Devices.Sensors;
using Windows.Foundation;
using SkiaSharp.Views.Windows;
using Uno.Extensions.Reactive.Bindings;


namespace Shturman.Presentation;

public partial record MainModel
{
    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig>? appInfo,
        INavigator navigator)
    {
        compass.ReadingChanged += CompassOnReadingChanged;
#if __ANDROID__
        if (GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(BaseActivity.Current) == 0)
        {
            if (ContextCompat.CheckSelfPermission(BaseActivity.Current, Manifest.Permission.AccessFineLocation) ==
                Permission.Granted)
            {
                Android.Gms.Location.LocationRequest.Builder locationRequestBuilder =
                    new Android.Gms.Location.LocationRequest.Builder(Priority.PriorityHighAccuracy, 10000);
                locationRequestBuilder.SetMaxUpdates(Int32.MaxValue);
                locationRequestBuilder.SetMaxUpdateAgeMillis(10000);
                locationRequestBuilder.SetMaxUpdateDelayMillis(10000);
                locationRequestBuilder.SetMinUpdateIntervalMillis(0);
                locationRequestBuilder.SetGranularity(2);
                Android.Gms.Location.LocationRequest locationRequest = locationRequestBuilder.Build();
                LocationListener locationListener = new LocationListener();
                //LocationCallback locationCallback = new LocationCallback();
                fusedLocation.RequestLocationUpdates(locationRequest, locationListener, Looper.MainLooper);
                //locationCallback.LocationResult += LocationCallbackOnLocationResult;
                locationListener.LocationChanged += LocationListenerOnLocationChanged;
                
            }
            else
            {
                NeedPermissions = true;
                ChangeCenterText("Даны недостаточные разрешения. Приложение не может функционировать без них.");
            }
        }
        else
        {
            try
            {
                GoogleApiAvailability.Instance.MakeGooglePlayServicesAvailable(BaseActivity.Current);
            }
            catch (Exception e)
            {
                Log.Error("Shturman", "User didn't fix GMS");
                BaseActivity.Current.FinishAffinity();
            }
            
        }
#endif
    }

    bool NeedPermissions;

#if __ANDROID__
    private void LocationListenerOnLocationChanged(Location location)
    {
        if (location.Accuracy < 4.5
            // & DateTime.Now - UnixTimeStampToDateTime(location.Time).ToLocalTime() <= DateTime.Now.Subtract(new DateTime(0,0,0,0,0,10
           )//)) // Check if accuracy ≤3 meters
        {
            currentPosition = new Geoposition(
                new Geocoordinate(
                    location.Latitude,
                    location.Longitude,
                    location.Accuracy,
                    DateTime.Now,
                    new Geopoint(new BasicGeoposition())));
            if (ActivatePath)
            {
                if (Calculator.CalculateDistance(path[0], currentPosition) < path[0].Coordinate.Accuracy)
                {
                    path.Remove(path[0]);
                }

                targetHeading = Calculator.CalculateBearing(currentPosition.Coordinate.Latitude,
                    currentPosition.Coordinate.Longitude,
                    path[0].Coordinate.Latitude, path[0].Coordinate.Longitude);
            }
            else
            {
                path.Add(new Geoposition(
                    new Geocoordinate(
                        location.Latitude, 
                        location.Longitude, 
                        location.Accuracy, 
                        DateTime.Now, 
                        new Geopoint(new BasicGeoposition()))));
#if DEBUG
                pathnumber.UpdateAsync(c => new Text(path.Count.ToString()));
#endif
            }
        }
    }
    /*
    private void LocationCallbackOnLocationResult(object? sender, LocationCallbackResultEventArgs e)
    {
        foreach (var location in e.Result.Locations)
        {
            if (location.Accuracy <= 4.5 
                // & DateTime.Now - UnixTimeStampToDateTime(location.Time).ToLocalTime() <= DateTime.Now.Subtract(new DateTime(0,0,0,0,0,10
               )//)) // Check if accuracy ≤3 meters
            {
                path.Add(new Geoposition(
                    new Geocoordinate(
                        location.Latitude, 
                        location.Longitude, 
                        location.Accuracy, 
                        DateTimeOffset.Now, 
                        new Geopoint(new BasicGeoposition()))));
                currentPosition = new Geoposition(
                    new Geocoordinate(
                        location.Latitude,
                        location.Longitude,
                        location.Accuracy,
                        DateTimeOffset.Now,
                        new Geopoint(new BasicGeoposition())));
            }
        }
    }
    */
#endif
    private void CompassOnReadingChanged(Compass sender, CompassReadingChangedEventArgs args)
    {
        if (ActivatePath)
        {
            ChangeTargetDegrees(compassFilter.Update((double)args.Reading.HeadingTrueNorth) + targetHeading);     
        }
    }
#if __ANDROID__
    private IFusedLocationProviderClient fusedLocation = LocationServices.GetFusedLocationProviderClient(Application.Context);
#endif
    private CompassFilter compassFilter = new(9);
    private Geoposition currentPosition;
    private double targetHeading;

    public Compass compass = Compass.GetDefault();

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
    public IState<Text> Text => State.Value(this, () => new Text("Дай-ка тебе покажу, что делает что. " +
                                                                 "Верху - компасс. Красная стрелка обозначает куда иди, а черная - твоё направление. " +
                                                                 "Чтобы включить или выключить его, надо нажать на красную кнопку. " +
                                                                 "Иконка паузы очищает путь."));
    
    public IState<Button> Button => State.Value(this, () => new Button("Я заблудился"));
#if DEBUG
    public IState<Text> pathnumber => State.Value(this, () => new Text(""));
#endif
    
    public IState<UICompass> UICompass => State.Value(this, () => new UICompass(0));
    
    public IState<UICompass> TargetUICompass => State.Value(this, () => new UICompass(0));

    public void ReversePath()
    {
        path.Reverse();
    }

    public void Deletepath()
    {
        path = new List<Geoposition>();
    }

    public ValueTask ChangeText()
    {
        ActivatePath = !ActivatePath;
        return Button.UpdateAsync(c => c.ChangeText());
    }
    
    public ValueTask ChangeCenterText(string text)
        => Text.UpdateAsync(c => c = new Text(text));

    public ValueTask ChangeDegrees(double degrees)
        => UICompass.UpdateAsync(c => c.ChangeDegrees(degrees));
    public ValueTask ChangeTargetDegrees(double degrees)
        => TargetUICompass.UpdateAsync(c => c.ChangeDegrees(degrees));

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
