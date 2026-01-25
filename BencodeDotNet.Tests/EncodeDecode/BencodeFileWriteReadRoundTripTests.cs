using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.EncodeDecode;

public class BencodeFileWriteReadRoundTripTests
{
    [Fact]
    public async Task WriteAndReadSingleIntegerFile()
    {
        var value = new Binteger(42);
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteToFileAsync(value, filePath, overwrite: true);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadFromFileAsync(filePath))
            {
                var integer = Assert.IsType<Binteger>(readValue);
                Assert.Equal(42, integer.Value);
            }
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteAndReadSingleStringFile()
    {
        var value = new Bstring(Encoding.ASCII.GetBytes("spam"));
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteToFileAsync(value, filePath, overwrite: true);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadFromFileAsync(filePath))
            {
                var str = Assert.IsType<Bstring>(readValue);
                Assert.Equal("spam", Encoding.ASCII.GetString(str.Value));
            }
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteAndReadNestedStructuresFile()
    {
        var value = new Blist(
        [
            new Bdictionary(new Dictionary<Bstring, IBobject>() {
                [new Bstring("x", Encoding.ASCII)] = new Binteger(9)
            }),
            new Blist(
            [
                new Binteger(1),
                new Binteger(2)
            ])
        ]);

        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteToFileAsync(value, filePath, overwrite: true);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadFromFileAsync(filePath))
            {
                var list = Assert.IsType<Blist>(readValue);

                var dict = Assert.IsType<Bdictionary>(list[0]);
                Assert.Equal(9, ((Binteger)dict[new Bstring(Encoding.ASCII.GetBytes("x"))]).Value);

                var innerList = Assert.IsType<Blist>(list[1]);
                Assert.Equal(1, ((Binteger)innerList[0]).Value);
                Assert.Equal(2, ((Binteger)innerList[1]).Value);
            }
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteAndReadMultipleConsecutiveObjectsFile()
    {
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();

            await writer.WriteToFileAsync(new Binteger(1), filePath, overwrite: true);
            await writer.WriteToFileAsync(new Binteger(2), filePath, overwrite: true); // overwrite for single-file semantics

            var reader = new BencodeReader();
            int[] expected = { 2 }; // only last write survives overwrite
            int i = 0;

            await foreach (var readValue in reader.ReadFromFileAsync(filePath))
            {
                var integer = Assert.IsType<Binteger>(readValue);
                Assert.Equal(expected[i], integer.Value);
                i++;
            }
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteAndReadGenericTypedObjectsFile()
    {
        var value = new Binteger(123);
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteBencodeToFileAsync(value, filePath, overwrite: true);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadFromFileAsync(filePath))
            {
                Assert.Equal(123, ((Binteger)readValue).Value);
            }
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteAndReadGenericMultipleTypedObjectsFile()
    {
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteBencodeToFileAsync(new Binteger(7), filePath, overwrite: true);

            var reader = new BencodeReader();
            int count = 0;

            await foreach (var readValue in reader.ReadFromFileAsync(filePath))
            {
                Assert.Equal(7, ((Binteger)readValue).Value);
                count++;
            }

            Assert.Equal(1, count);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteAndReadFilePreservesByteEquality()
    {
        var value = new Bstring(Encoding.ASCII.GetBytes("roundtrip"));
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteToFileAsync(value, filePath, overwrite: true);

            var originalBytes = File.ReadAllBytes(filePath);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadFromFileAsync(filePath))
            {
                using var mem = new MemoryStream();
                var writer2 = new BencodeWriter();
                await writer2.WriteAsync(readValue, mem);

                Assert.Equal(originalBytes, mem.ToArray());
            }
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static async Task<string> CreateTempFilePath()
    {
        var path = Path.GetTempFileName();
        File.Delete(path); // Ensure we start clean
        return path;
    }
}
