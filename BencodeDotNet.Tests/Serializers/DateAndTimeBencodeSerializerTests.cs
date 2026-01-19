using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class DateAndTimeBencodeSerializerTests
{
    private static readonly DateTimeBencodeSerializer DateTimeSerializer = new();
    private static readonly DateOnlyBencodeSerializer DateOnlySerializer = new();
    private static readonly TimeOnlyBencodeSerializer TimeOnlySerializer = new();
    private static readonly TimeSpanBencodeSerializer TimeSpanSerializer = new();

    #region Test DateTimeSerializer
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void RoundTripPreservesValueAndKind(DateTimeKind kind)
    {
        var input = new DateTime(2024, 6, 15, 13, 45, 30, kind);

        Assert.True(DateTimeSerializer.TrySerialize(input, out var binteger));
        Assert.NotNull(binteger);

        Assert.True(DateTimeSerializer.TryDeserialize(binteger!, out var output));
        Assert.Equal(input, output);
        Assert.Equal(input.Kind, output.Kind);
    }

    [Fact]
    public void SupportsMinAndMaxDateTimeValues()
    {
        var values = new[]
        {
            DateTime.MinValue,
            DateTime.MaxValue
        };

        foreach (var value in values)
        {
            Assert.True(DateTimeSerializer.TrySerialize(value, out var binteger));
            Assert.True(DateTimeSerializer.TryDeserialize(binteger!, out var output));
            Assert.Equal(value, output);
        }
    }
    #endregion

    #region Test DateOnlySerializer
    [Fact]
    public void RoundTripPreservesDateOnlyValue()
    {
        var input = new DateOnly(2024, 6, 15);

        Assert.True(DateOnlySerializer.TrySerialize(input, out var binteger));
        Assert.NotNull(binteger);

        Assert.True(DateOnlySerializer.TryDeserialize(binteger!, out var output));
        Assert.Equal(input, output);
    }

    [Fact]
    public void SupportsMinAndMaxDateOnlyValues()
    {
        var values = new[]
        {
            DateOnly.MinValue,
            DateOnly.MaxValue
        };

        foreach (var value in values)
        {
            Assert.True(DateOnlySerializer.TrySerialize(value, out var binteger));
            Assert.True(DateOnlySerializer.TryDeserialize(binteger!, out var output));
            Assert.Equal(value, output);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    public void DeserializeFailsForOutOfDateOnlyRangeValues(long dayNumber)
    {
        var binteger = new Binteger(dayNumber);

        Assert.False(DateOnlySerializer.TryDeserialize(binteger, out _));
    }
    #endregion

    #region Test TimeOnlySerializer
    [Fact]
    public void RoundTripPreservesTimeOnlyValue()
    {
        var input = new TimeOnly(13, 45, 30);

        Assert.True(TimeOnlySerializer.TrySerialize(input, out var binteger));
        Assert.NotNull(binteger);

        Assert.True(TimeOnlySerializer.TryDeserialize(binteger!, out var output));
        Assert.Equal(input, output);
    }

    [Fact]
    public void SupportsMinAndMaxTimeOnlyValues()
    {
        var values = new[]
        {
            TimeOnly.MinValue,
            TimeOnly.MaxValue
        };

        foreach (var value in values)
        {
            Assert.True(TimeOnlySerializer.TrySerialize(value, out var binteger));
            Assert.True(TimeOnlySerializer.TryDeserialize(binteger!, out var output));
            Assert.Equal(value, output);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    public void DeserializeFailsForOutOfTimeOnlyRangeValues(long ticks)
    {
        var binteger = new Binteger(ticks);

        Assert.False(TimeOnlySerializer.TryDeserialize(binteger, out _));
    }
    #endregion

    #region Test TimeSpanSerializer
    [Fact]
    public void RoundTripPreservesTimeSpanValue()
    {
        var input = TimeSpan.FromHours(36) + TimeSpan.FromMinutes(15);

        Assert.True(TimeSpanSerializer.TrySerialize(input, out var binteger));
        Assert.NotNull(binteger);

        Assert.True(TimeSpanSerializer.TryDeserialize(binteger!, out var output));
        Assert.Equal(input, output);
    }

    [Fact]
    public void SupportsMinAndMaxTimeSpanValues()
    {
        var values = new[]
        {
            TimeSpan.MinValue,
            TimeSpan.MaxValue
        };

        foreach (var value in values)
        {
            Assert.True(TimeSpanSerializer.TrySerialize(value, out var binteger));
            Assert.True(TimeSpanSerializer.TryDeserialize(binteger!, out var output));
            Assert.Equal(value, output);
        }
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void DeserializeFailsForOutOfTimeSpanRangeValues(long ticks)
    {
        var binteger = new Binteger(ticks);

        // Only fail if the ticks exceed TimeSpan bounds
        var success = TimeSpanSerializer.TryDeserialize(binteger, out _);

        Assert.Equal(
            ticks <= TimeSpan.MaxValue.Ticks && ticks >= TimeSpan.MinValue.Ticks,
            success);
    }
    #endregion
}
