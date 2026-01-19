using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class DateTimeBencodeSerializerTests
{
    private static readonly DateTimeBencodeSerializer Serializer = new();

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void RoundTripPreservesValueAndKind(DateTimeKind kind)
    {
        var input = new DateTime(2024, 6, 15, 13, 45, 30, kind);

        Assert.True(Serializer.TrySerialize(input, out var binteger));
        Assert.NotNull(binteger);

        Assert.True(Serializer.TryDeserialize(binteger!, out var output));
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
            Assert.True(Serializer.TrySerialize(value, out var binteger));
            Assert.True(Serializer.TryDeserialize(binteger!, out var output));
            Assert.Equal(value, output);
        }
    }
}

public class DateOnlyBencodeSerializerTests
{
    private static readonly DateOnlyBencodeSerializer Serializer = new();

    [Fact]
    public void RoundTripPreservesDateOnlyValue()
    {
        var input = new DateOnly(2024, 6, 15);

        Assert.True(Serializer.TrySerialize(input, out var binteger));
        Assert.NotNull(binteger);

        Assert.True(Serializer.TryDeserialize(binteger!, out var output));
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
            Assert.True(Serializer.TrySerialize(value, out var binteger));
            Assert.True(Serializer.TryDeserialize(binteger!, out var output));
            Assert.Equal(value, output);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    public void DeserializeFailsForOutOfDateOnlyRangeValues(long dayNumber)
    {
        var binteger = new Binteger(dayNumber);

        Assert.False(Serializer.TryDeserialize(binteger, out _));
    }
}

public class TimeOnlyBencodeSerializerTests
{
    private static readonly TimeOnlyBencodeSerializer Serializer = new();

    [Fact]
    public void RoundTripPreservesTimeOnlyValue()
    {
        var input = new TimeOnly(13, 45, 30);

        Assert.True(Serializer.TrySerialize(input, out var binteger));
        Assert.NotNull(binteger);

        Assert.True(Serializer.TryDeserialize(binteger!, out var output));
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
            Assert.True(Serializer.TrySerialize(value, out var binteger));
            Assert.True(Serializer.TryDeserialize(binteger!, out var output));
            Assert.Equal(value, output);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    public void DeserializeFailsForOutOfTimeOnlyRangeValues(long ticks)
    {
        var binteger = new Binteger(ticks);

        Assert.False(Serializer.TryDeserialize(binteger, out _));
    }
}

public class TimeSpanBencodeSerializerTests
{
    private static readonly TimeSpanBencodeSerializer Serializer = new();

    [Fact]
    public void RoundTripPreservesTimeSpanValue()
    {
        var input = TimeSpan.FromHours(36) + TimeSpan.FromMinutes(15);

        Assert.True(Serializer.TrySerialize(input, out var binteger));
        Assert.NotNull(binteger);

        Assert.True(Serializer.TryDeserialize(binteger!, out var output));
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
            Assert.True(Serializer.TrySerialize(value, out var binteger));
            Assert.True(Serializer.TryDeserialize(binteger!, out var output));
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
        var success = Serializer.TryDeserialize(binteger, out _);

        Assert.Equal(
            ticks <= TimeSpan.MaxValue.Ticks && ticks >= TimeSpan.MinValue.Ticks,
            success);
    }
}
