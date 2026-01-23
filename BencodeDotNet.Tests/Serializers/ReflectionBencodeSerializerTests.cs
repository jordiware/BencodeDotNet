using Jordiware.BencodeDotNet.Attributes;
using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public sealed class TestModel
{
    public int IntField;
    public string? NullableField;

    public int IntProperty { get; set; }

    [BencodeName("renamed")]
    public string NamedProperty { get; set; } = string.Empty;

    [BencodeIgnore]
    public int IgnoredProperty { get; set; }

    private int PrivateField;
    private string PrivateProperty { get; set; } = string.Empty;
}

public sealed class MissingMemberModel
{
    public int Value { get; set; }
}

public sealed class NullableModel
{
    public int? NullableInt { get; set; }
    public string? NullableString { get; set; }
}

public sealed class DuplicateKeysModel
{
    public int Value;

    [BencodeName("Value")]
    public int AnotherValue;
}

public class ReflectionBencodeSerializerTests
{
    [Fact]
    public void TrySerializeSerializesPublicFieldsAndProperties()
    {
        var serializer = new ReflectionBencodeSerializer<TestModel>();
        var model = new TestModel
        {
            IntField = 10,
            IntProperty = 20,
            NamedProperty = "hello",
            IgnoredProperty = 999
        };

        var result = serializer.TrySerialize(model, out var dict);

        Assert.True(result);
        Assert.NotNull(dict);

        Assert.True(dict!.ContainsKey(new Bstring(nameof(TestModel.IntField), Encoding.UTF8)));
        Assert.True(dict.ContainsKey(new Bstring(nameof(TestModel.IntProperty), Encoding.UTF8)));
        Assert.True(dict.ContainsKey(new Bstring("renamed", Encoding.UTF8)));

        Assert.False(dict.ContainsKey(new Bstring(nameof(TestModel.IgnoredProperty), Encoding.UTF8)));
    }

    [Fact]
    public void TrySerializeSkipsNullValues()
    {
        var serializer = new ReflectionBencodeSerializer<TestModel>();
        var model = new TestModel
        {
            IntField = 1,
            IntProperty = 2,
            NullableField = null
        };

        serializer.TrySerialize(model, out var dict);

        Assert.NotNull(dict);
        Assert.False(dict!.ContainsKey(new Bstring(nameof(TestModel.NullableField), Encoding.UTF8)));
    }

    [Fact]
    public void TrySerializeEmitsKeysInSortedOrder()
    {
        var serializer = new ReflectionBencodeSerializer<TestModel>();
        var model = new TestModel
        {
            IntField = 1,
            IntProperty = 2,
            NamedProperty = "x"
        };

        serializer.TrySerialize(model, out var dict);

        var keys = dict!.Keys.ToArray();

        for (int i = 1; i < keys.Length; i++)
        {
            Assert.True(keys[i - 1].CompareTo(keys[i]) <= 0);
        }
    }

    [Fact]
    public void TryDeserializePopulatesMatchingMembers()
    {
        var serializer = new ReflectionBencodeSerializer<TestModel>();
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring(nameof(TestModel.IntField), Encoding.UTF8)] = new Binteger(5),
            [new Bstring(nameof(TestModel.IntProperty), Encoding.UTF8)] = new Binteger(7),
            [new Bstring("renamed", Encoding.UTF8)] = new Bstring("abc", Encoding.UTF8)
        });

        var result = serializer.TryDeserialize(dict, out var model);

        Assert.True(result);
        Assert.NotNull(model);

        Assert.Equal(5, model!.IntField);
        Assert.Equal(7, model.IntProperty);
        Assert.Equal("abc", model.NamedProperty);
    }

    [Fact]
    public void TryDeserializeIgnoresUnknownKeys()
    {
        var serializer = new ReflectionBencodeSerializer<MissingMemberModel>();
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring("Unknown", Encoding.UTF8)] = new Binteger(123),
            [new Bstring(nameof(MissingMemberModel.Value), Encoding.UTF8)] = new Binteger(10)
        });

        serializer.TryDeserialize(dict, out var model);

        Assert.NotNull(model);
        Assert.Equal(10, model!.Value);
    }

    [Fact]
    public void TryDeserializeLeavesMissingMembersUnchanged()
    {
        var serializer = new ReflectionBencodeSerializer<NullableModel>();
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>());

        serializer.TryDeserialize(dict, out var model);

        Assert.NotNull(model);
        Assert.Null(model!.NullableInt);
        Assert.Null(model.NullableString);
    }

    [Fact]
    public void TrySerializeThrowsWhenMemberSerializationFails()
    {
        var serializer = new ReflectionBencodeSerializer<InvalidModel>();

        Assert.Throws<NotSupportedException>(() =>
        {
            serializer.TrySerialize(new InvalidModel(), out _);
        });
    }

    [Fact]
    public void TrySerializeThrowsWhenDuplicateKeysFound()
    {
        var serializer = new ReflectionBencodeSerializer<DuplicateKeysModel>();
        var model = new DuplicateKeysModel
        {
            Value = 1,
            AnotherValue = 2
        };

        Assert.Throws<InvalidOperationException>(() =>
        {
            serializer.TrySerialize(model, out _);
        });
    }

    private sealed class InvalidModel
    {
        public object Value { get; set; } = new();
    }
}
