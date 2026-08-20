using Xunit;
using GDL90.Core;
using GDL90.Core.Messages;

namespace GDL90.Tests;

public class OwnshipGeometricAltitudeTests
{
    [Fact]
    public void OwnshipGeometricAltitude_Parses_Normal_Values()
    {
        // Spec section 3.8 examples:
        // +1000 feet -> 0x00C8 because 1000 / 5 = 200 = 0x00C8
        // Vertical Metrics 0x8032 -> warning set, VFOM = 50m
        byte[] ownshipGeoAltMessage = new byte[]
        {
            0x0B,
            0x00, 0xC8,
            0x80, 0x32
        };

        OwnshipGeometricAltitude ownshipGeoAlt = new OwnshipGeometricAltitude(Message.AppendFlagBytes(Message.AppendCRC(ownshipGeoAltMessage)));

        Assert.True(ownshipGeoAlt.ValidCRC);
        Assert.Equal(1000, ownshipGeoAlt.GeometricAltitude);
        Assert.True(ownshipGeoAlt.VerticalWarningIndicator);
        Assert.Equal(50, ownshipGeoAlt.VerticalFigureOfMerit);
        Assert.False(ownshipGeoAlt.VerticalFigureOfMeritNotAvailable);
        Assert.False(ownshipGeoAlt.VerticalFigureOfMeritGreaterThanOrEqual32766);
    }

    [Fact]
    public void OwnshipGeometricAltitude_Parses_Negative_Altitude_And_NotAvailable_Vfom()
    {
        // Spec section 3.8 examples:
        // -1000 feet -> 0xFF38 because -1000 / 5 = -200 = 0xFF38
        // Vertical Metrics 0xFFFF -> warning set and VFOM not available
        byte[] ownshipGeoAltMessage = new byte[]
        {
            0x0B,
            0xFF, 0x38,
            0xFF, 0xFF
        };

        OwnshipGeometricAltitude ownshipGeoAlt = new OwnshipGeometricAltitude(Message.AppendFlagBytes(Message.AppendCRC(ownshipGeoAltMessage)));

        Assert.True(ownshipGeoAlt.ValidCRC);
        Assert.Equal(-1000, ownshipGeoAlt.GeometricAltitude);
        Assert.True(ownshipGeoAlt.VerticalWarningIndicator);
        Assert.Equal(0x7FFF, ownshipGeoAlt.VerticalFigureOfMerit);
        Assert.True(ownshipGeoAlt.VerticalFigureOfMeritNotAvailable);
        Assert.False(ownshipGeoAlt.VerticalFigureOfMeritGreaterThanOrEqual32766);
    }

    [Fact]
    public void MessageFactory_Creates_OwnshipGeometricAltitude_For_MessageId_0x0B()
    {
        // Spec section 3.8: Message ID 11 (0x0B) identifies Ownship Geometric Altitude.
        byte[] ownshipGeoAltMessage = new byte[]
        {
            0x0B,
            0x00, 0x00,
            0x7F, 0xFE
        };

        Message parsedMessage = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(ownshipGeoAltMessage)));

        OwnshipGeometricAltitude ownshipGeoAlt = Assert.IsType<OwnshipGeometricAltitude>(parsedMessage);
        Assert.True(ownshipGeoAlt.ValidCRC);
        Assert.False(ownshipGeoAlt.VerticalWarningIndicator);
        Assert.True(ownshipGeoAlt.VerticalFigureOfMeritGreaterThanOrEqual32766);
        Assert.False(ownshipGeoAlt.VerticalFigureOfMeritNotAvailable);
    }
}