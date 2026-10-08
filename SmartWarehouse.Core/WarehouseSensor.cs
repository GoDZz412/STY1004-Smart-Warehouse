namespace SmartWarehouse.Core;

/// <summary>
/// Models an industrial IoT environmental sensor in a warehouse zone.
/// All state changes go through methods so the rules can't be bypassed.
/// </summary>
public class WarehouseSensor
{
    private const double MinReadingCelsius = -50.0;
    private const double MaxReadingCelsius = 80.0;
    private const double MinThresholdCelsius = -30.0;
    private const double MaxThresholdCelsius = 50.0;
    private const double DefaultThresholdCelsius = 4.0;

    private string _locationTag = string.Empty;

    public string SensorId { get; private set; }

    public string LocationTag
    {
        get => _locationTag;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Location tag cannot be null, empty, or whitespace.", nameof(value));
            _locationTag = value;
        }
    }

    public double CurrentTemperature { get; private set; } = 0.0;
    public bool IsActive { get; private set; } = false;
    public bool IsAlertTriggered { get; private set; } = false;
    public double CriticalThresholdCelsius { get; private set; }

    public WarehouseSensor(string sensorId, string locationTag, double criticalThreshold = DefaultThresholdCelsius)
    {
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new ArgumentException("Sensor ID cannot be null, empty, or whitespace.", nameof(sensorId));

        if (string.IsNullOrWhiteSpace(locationTag))
            throw new ArgumentException("Location tag cannot be null, empty, or whitespace.", nameof(locationTag));

        EnsureThresholdInRange(criticalThreshold);

        SensorId = sensorId;
        LocationTag = locationTag;
        CriticalThresholdCelsius = criticalThreshold;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsAlertTriggered = false;
    }

    public void RecordReading(double newTemperature)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot record a reading while the sensor is inactive.");

        // NaN is also rejected
        if (!(newTemperature >= MinReadingCelsius && newTemperature <= MaxReadingCelsius))
            throw new ArgumentOutOfRangeException(nameof(newTemperature), newTemperature,
                $"Temperature must be between {MinReadingCelsius} and {MaxReadingCelsius} °C.");

        CurrentTemperature = newTemperature;
        EvaluateAlert();
    }

    public void UpdateThreshold(double newThreshold)
    {
        EnsureThresholdInRange(newThreshold);

        CriticalThresholdCelsius = newThreshold;

        if (IsActive)
            EvaluateAlert();
    }

    private void EvaluateAlert()
    {
        IsAlertTriggered = CurrentTemperature >= CriticalThresholdCelsius;
    }

    private static void EnsureThresholdInRange(double threshold)
    {
        if (!(threshold >= MinThresholdCelsius && threshold <= MaxThresholdCelsius))
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold,
                $"Threshold must be between {MinThresholdCelsius} and {MaxThresholdCelsius} °C.");
    }
}