using SmartWarehouse.Core;

namespace SmartWarehouse.Tests;

[TestClass]
public sealed class WarehouseSensorTests
{
    private const string ValidId = "SENS-001";
    private const string ValidLocation = "Cold-Vault-01";

    private static WarehouseSensor CreateActiveSensor(double threshold = 4.0)
    {
        var sensor = new WarehouseSensor(ValidId, ValidLocation, threshold);
        sensor.Activate();
        return sensor;
    }

    //Constructor
    [TestMethod]
    public void Constructor_ValidArguments_SetsInitialState()
    {
        //Arrange
        //inputs are the class constants

        //Act
        var sensor = new WarehouseSensor(ValidId, ValidLocation);

        //Assert
        Assert.AreEqual(ValidId, sensor.SensorId);
        Assert.AreEqual(ValidLocation, sensor.LocationTag);
        Assert.AreEqual(0.0, sensor.CurrentTemperature);
        Assert.IsFalse(sensor.IsActive);
        Assert.IsFalse(sensor.IsAlertTriggered);
        Assert.AreEqual(4.0, sensor.CriticalThresholdCelsius);
    }

    [TestMethod]
    public void Constructor_CustomThreshold_IsApplied()
    {
        //Arrange
        const double customThreshold = 8.5;
        //Act
        var sensor = new WarehouseSensor(ValidId, ValidLocation, customThreshold);
        //Assert
        Assert.AreEqual(customThreshold, sensor.CriticalThresholdCelsius);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_InvalidSensorId_ThrowsArgumentException(string? invalidId)
    {
        //Arrange
        Action create = () => new WarehouseSensor(invalidId!, ValidLocation);
        //Act
        //Assert
        Assert.ThrowsExactly<ArgumentException>(create);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_InvalidLocationTag_ThrowsArgumentException(string? invalidLocation)
    {
        //Arrange
        Action create = () => new WarehouseSensor(ValidId, invalidLocation!);
        //Act
        //Assert
        Assert.ThrowsExactly<ArgumentException>(create);
    }

    //Lifecycle
    [TestMethod]
    public void Activate_CalledTwice_RemainsActive()
    {
        //Arrange
        var sensor = new WarehouseSensor(ValidId, ValidLocation);
        //Act
        sensor.Activate();
        sensor.Activate();
        //Assert
        Assert.IsTrue(sensor.IsActive);
    }

    [TestMethod]
    public void Deactivate_AfterAlert_SetsInactiveAndClearsAlert()
    {
        //Arrange
        var sensor = CreateActiveSensor();
        sensor.RecordReading(10.0);
        //Act
        sensor.Deactivate();
        //Assert
        Assert.IsFalse(sensor.IsActive);
        Assert.IsFalse(sensor.IsAlertTriggered);
    }

    [TestMethod]
    public void RecordReading_WhenNeverActivated_ThrowsInvalidOperationException()
    {
        //Arrange
        var sensor = new WarehouseSensor(ValidId, ValidLocation);
        //Act
        Action record = () => sensor.RecordReading(2.0);
        //Assert
        Assert.ThrowsExactly<InvalidOperationException>(record);
    }

    [TestMethod]
    public void RecordReading_AfterDeactivate_ThrowsInvalidOperationException()
    {
        //Arrange
        var sensor = CreateActiveSensor();
        sensor.Deactivate();
        //Act
        Action record = () => sensor.RecordReading(2.0);
        //Assert
        Assert.ThrowsExactly<InvalidOperationException>(record);
    }

    //Threshold alerts (below / at / above)
    [DataTestMethod]
    [DataRow(3.9, false)]  // below threshold
    [DataRow(4.0, true)]   // exactly at threshold
    [DataRow(4.1, true)]   // above threshold
    public void RecordReading_RelativeToThreshold_SetsAlertCorrectly(double temperature, bool expectedAlert)
    {
        //Arrange
        var sensor = CreateActiveSensor();
        //Act
        sensor.RecordReading(temperature);
        //Assert
        Assert.AreEqual(temperature, sensor.CurrentTemperature);
        Assert.AreEqual(expectedAlert, sensor.IsAlertTriggered);
    }

    [TestMethod]
    public void RecordReading_DropsBelowThreshold_ClearsAlert()
    {
        //Arrange
        var sensor = CreateActiveSensor();
        sensor.RecordReading(6.0);
        //Act
        sensor.RecordReading(2.0);
        //Assert
        Assert.IsFalse(sensor.IsAlertTriggered);
    }

    //Reading boundaries
    [DataTestMethod]
    [DataRow(-50.0)]
    [DataRow(80.0)]
    public void RecordReading_AtBoundary_IsAccepted(double boundary)
    {
        //Arrange
        var sensor = CreateActiveSensor();
        //Act
        sensor.RecordReading(boundary);
        //Assert
        Assert.AreEqual(boundary, sensor.CurrentTemperature);
    }

    [DataTestMethod]
    [DataRow(-50.1)]
    [DataRow(80.1)]
    [DataRow(double.NaN)]
    public void RecordReading_OutsideBoundary_ThrowsAndKeepsPreviousValue(double invalid)
    {
        //Arrange
        var sensor = CreateActiveSensor();
        sensor.RecordReading(2.0);
        //Act
        Action record = () => sensor.RecordReading(invalid);
        //Assert
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(record);
        Assert.AreEqual(2.0, sensor.CurrentTemperature);
    }

    //UpdateThreshold
    [DataTestMethod]
    [DataRow(-30.0)]
    [DataRow(50.0)]
    public void UpdateThreshold_AtBoundary_IsAccepted(double boundary)
    {
        //Arrange
        var sensor = new WarehouseSensor(ValidId, ValidLocation);
        //Act
        sensor.UpdateThreshold(boundary);
        //Assert
        Assert.AreEqual(boundary, sensor.CriticalThresholdCelsius);
    }

    [DataTestMethod]
    [DataRow(-30.1)]
    [DataRow(50.1)]
    public void UpdateThreshold_OutsideBoundary_ThrowsArgumentOutOfRangeException(double invalid)
    {
        //Arrange
        var sensor = new WarehouseSensor(ValidId, ValidLocation);
        //Act
        Action update = () => sensor.UpdateThreshold(invalid);
        //Assert
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(update);
    }

    [DataTestMethod]
    [DataRow(15.0, false)] // threshold raised above current temp -> alert clears
    [DataRow(10.0, true)]  // threshold equals current temp -> alert stays
    [DataRow(5.0, true)]   // threshold below current temp -> alert stays
    public void UpdateThreshold_OnActiveSensor_ReevaluatesAlert(double newThreshold, bool expectedAlert)
    {
        //Arrange
        var sensor = CreateActiveSensor();
        sensor.RecordReading(10.0);
        //Act
        sensor.UpdateThreshold(newThreshold);
        //Assert
        Assert.AreEqual(expectedAlert, sensor.IsAlertTriggered);
    }

    [TestMethod]
    public void UpdateThreshold_LoweredBelowCurrentTemp_TriggersAlert()
    {
        //Arrange
        var sensor = CreateActiveSensor(threshold: 20.0);
        sensor.RecordReading(10.0);
        //Act
        sensor.UpdateThreshold(5.0);
        //Assert
        Assert.IsTrue(sensor.IsAlertTriggered);
    }
}