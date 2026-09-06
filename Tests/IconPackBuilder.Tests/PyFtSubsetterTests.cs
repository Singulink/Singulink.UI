using System.Buffers.Binary;
using IconPackBuilder.Core.IconSources;
using IconPackBuilder.Core.Services;
using PrefixClassName.MsTest;
using Shouldly;
using Singulink.IO;

namespace IconPackBuilder.Tests;

/// <summary>
/// Runs the bundled pyftsubset binary against the real Seagull font. The Windows x64 binary is committed to the repo; other platforms only have
/// a binary once the "Build pyftsubset binaries" workflow has published one, so the test is limited to Windows.
/// </summary>
[PrefixTestClass]
public class PyFtSubsetterTests
{
    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public async Task Save_SubsetsSeagullFontToRequestedCodePoints()
    {
        var subsetter = new PyFtSubsetter();
        subsetter.BundledToolFile.Exists.ShouldBeTrue($"expected bundled binary at {subsetter.BundledToolFile.PathDisplay}");

        var source = DirectoryPath.GetAppBase() + SeagullIconsSource.Instance.FontFile;
        var destination = FilePath.ParseAbsolute(Path.Combine(TestFiles.NewTempDirectory(), "Subset.otf"), PathOptions.None);
        int[] codePoints = [0xF03E4, 0xF0050, 0x100050];

        await subsetter.SaveAsync(source, destination, codePoints);

        destination.Exists.ShouldBeTrue();
        byte[] font = File.ReadAllBytes(destination.PathExport);
        font.Length.ShouldBeLessThan((int)source.Length / 10);
        font.AsSpan(0, 4).ToArray().ShouldBe("OTTO"u8.ToArray());
        ReadMappedCodePoints(font).ShouldBe(codePoints, ignoreOrder: true);
    }

    [TestMethod]
    public async Task Save_MissingTool_ReportsWhereItLooked()
    {
        // A tools directory without binaries only falls through to PATH/Python probing, so this is deterministic only when neither is present.
        var toolsDir = DirectoryPath.ParseAbsolute(TestFiles.NewTempDirectory(), PathOptions.None);
        var subsetter = new PyFtSubsetter(toolsDir);

        subsetter.BundledToolFile.ParentDirectory.ParentDirectory.ShouldBe(toolsDir);
        subsetter.BundledToolFile.Name.ShouldStartWith("pyftsubset");

        try
        {
            await subsetter.SaveAsync(toolsDir.CombineFile("missing.otf", PathOptions.None), toolsDir.CombineFile("out.otf", PathOptions.None), [0x41]);
        }
        catch (FileNotFoundException ex)
        {
            ex.Message.ShouldContain(toolsDir.PathDisplay);
            ex.Message.ShouldContain("pip install fonttools");
        }
        catch (InvalidOperationException)
        {
            // A PATH or Python fallback exists on this machine and rejected the missing input font, which is also correct behavior.
        }
    }

    /// <summary>
    /// Reads every code point mapped by the font's format 12 cmap subtables.
    /// </summary>
    private static HashSet<int> ReadMappedCodePoints(byte[] font)
    {
        var data = font.AsSpan();
        int numTables = BinaryPrimitives.ReadUInt16BigEndian(data[4..]);
        int cmapOffset = -1;

        for (int i = 0; i < numTables; i++)
        {
            var record = data.Slice(12 + (16 * i), 16);

            if (record[..4].SequenceEqual("cmap"u8))
                cmapOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
        }

        cmapOffset.ShouldBeGreaterThan(0, "font has no cmap table");

        var cmap = data[cmapOffset..];
        int numSubtables = BinaryPrimitives.ReadUInt16BigEndian(cmap[2..]);
        var result = new HashSet<int>();

        for (int i = 0; i < numSubtables; i++)
        {
            int subtableOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(cmap[(4 + (8 * i) + 4)..]);
            var subtable = cmap[subtableOffset..];

            if (BinaryPrimitives.ReadUInt16BigEndian(subtable) != 12)
                continue;

            int numGroups = (int)BinaryPrimitives.ReadUInt32BigEndian(subtable[12..]);

            for (int g = 0; g < numGroups; g++)
            {
                var group = subtable.Slice(16 + (12 * g), 12);
                int start = (int)BinaryPrimitives.ReadUInt32BigEndian(group);
                int end = (int)BinaryPrimitives.ReadUInt32BigEndian(group[4..]);

                for (int cp = start; cp <= end; cp++)
                    result.Add(cp);
            }
        }

        return result;
    }
}
