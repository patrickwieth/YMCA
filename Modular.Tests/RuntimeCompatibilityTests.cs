using System.Security.Cryptography;
using NUnit.Framework;
using OpenRA.Mods.CA;

namespace Modular.Tests;

public class RuntimeCompatibilityTests
{
    [Test]
    public void HashesMatchDotNetIncludingEmptyInput()
    {
        var random = new Random(913);
        foreach (var size in new[] { 0, 1, 255, 1024, 65536 })
        {
            var bytes = new byte[size]; random.NextBytes(bytes);
            Assert.That(RuntimeCompat.HashData(bytes), Is.EqualTo(SHA256.HashData(bytes)));
            Assert.That(RuntimeCompat.ToHexString(bytes), Is.EqualTo(Convert.ToHexString(bytes)));
        }
    }

    [Test]
    public void PriorityQueuePreservesDotNetTieOrdering()
    {
        var random = new Random(7201);
        var reference = new PriorityQueue<int, int>();
        var portable = new PortablePriorityQueue<int, int>();
        for (var i = 0; i < 20000; i++)
        {
            if (random.Next(3) != 0)
            {
                var priority = random.Next(8); // deliberately many equal priorities
                reference.Enqueue(i, priority); portable.Enqueue(i, priority);
            }
            else
            {
                var expected = reference.TryDequeue(out var element, out var priority);
                Assert.That(portable.TryDequeue(out var actual, out var actualPriority), Is.EqualTo(expected));
                Assert.That((actual, actualPriority), Is.EqualTo((element, priority)));
            }
        }
        while (reference.TryDequeue(out var element, out var priority))
        {
            Assert.That(portable.TryDequeue(out var actual, out var actualPriority), Is.True);
            Assert.That((actual, actualPriority), Is.EqualTo((element, priority)));
        }
        Assert.That(portable.TryDequeue(out _, out _), Is.False);
    }

    [Test]
    public void FileReplacementCreatesAndOverwritesWithoutLosingOldDataOnFailure()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var source = Path.Combine(directory, "temp"); var target = Path.Combine(directory, "data");
            File.WriteAllText(source, "first"); RuntimeCompat.ReplaceFile(source, target);
            Assert.That(File.ReadAllText(target), Is.EqualTo("first"));
            File.WriteAllText(source, "second"); RuntimeCompat.ReplaceFile(source, target);
            Assert.That(File.ReadAllText(target), Is.EqualTo("second"));
            Assert.That(File.Exists(source), Is.False);
            Assert.Throws<FileNotFoundException>(() => RuntimeCompat.ReplaceFile(source, target));
            Assert.That(File.ReadAllText(target), Is.EqualTo("second"));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Test]
    public void EnumAndPlatformDetectionMatchDotNet()
    {
        Assert.That(RuntimeCompat.EnumValues<DayOfWeek>(), Is.EqualTo(Enum.GetValues<DayOfWeek>()));
        Assert.That(RuntimeCompat.IsWindows, Is.EqualTo(OperatingSystem.IsWindows()));
    }
}
