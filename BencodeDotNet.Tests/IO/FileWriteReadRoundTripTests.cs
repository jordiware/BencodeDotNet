using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class FileWriteReadRoundTripTests
{
    [Fact]
    public async Task WriteAndReadSingleIntegerFile()
    {
        var value = new BInteger(42);
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteToFileAsync(value, filePath, overwrite: true);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadMultipleFromFileAsync(filePath))
            {
                var integer = Assert.IsType<BInteger>(readValue);
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
        var value = new BString(Encoding.ASCII.GetBytes("spam"));
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteToFileAsync(value, filePath, overwrite: true);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadMultipleFromFileAsync(filePath))
            {
                var str = Assert.IsType<BString>(readValue);
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
        var value = new BList(
        [
            new BDictionary(new Dictionary<BString, IBObject>() {
                [new BString("x", Encoding.ASCII)] = new BInteger(9)
            }),
            new BList(
            [
                new BInteger(1),
                new BInteger(2)
            ])
        ]);

        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteToFileAsync(value, filePath, overwrite: true);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadMultipleFromFileAsync(filePath))
            {
                var list = Assert.IsType<BList>(readValue);

                var dict = Assert.IsType<BDictionary>(list[0]);
                Assert.Equal(9, ((BInteger)dict[new BString(Encoding.ASCII.GetBytes("x"))]).Value);

                var innerList = Assert.IsType<BList>(list[1]);
                Assert.Equal(1, ((BInteger)innerList[0]).Value);
                Assert.Equal(2, ((BInteger)innerList[1]).Value);
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

            await writer.WriteToFileAsync(new BInteger(1), filePath, overwrite: true);
            await writer.WriteToFileAsync(new BInteger(2), filePath, overwrite: true); // overwrite for single-file semantics

            var reader = new BencodeReader();
            int[] expected = { 2 }; // only last write survives overwrite
            int i = 0;

            await foreach (var readValue in reader.ReadMultipleFromFileAsync(filePath))
            {
                var integer = Assert.IsType<BInteger>(readValue);
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
        var value = new BInteger(123);
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteBencodeToFileAsync(value, filePath, overwrite: true);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadMultipleFromFileAsync(filePath))
            {
                Assert.Equal(123, ((BInteger)readValue).Value);
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
            await writer.WriteBencodeToFileAsync(new BInteger(7), filePath, overwrite: true);

            var reader = new BencodeReader();
            int count = 0;

            await foreach (var readValue in reader.ReadMultipleFromFileAsync(filePath))
            {
                Assert.Equal(7, ((BInteger)readValue).Value);
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
        var value = new BString(Encoding.ASCII.GetBytes("roundtrip"));
        var filePath = await CreateTempFilePath();

        try
        {
            var writer = new BencodeWriter();
            await writer.WriteToFileAsync(value, filePath, overwrite: true);

            var originalBytes = File.ReadAllBytes(filePath);

            var reader = new BencodeReader();
            await foreach (var readValue in reader.ReadMultipleFromFileAsync(filePath))
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
