using IconPackBuilder.Core;
using IconPackBuilder.Core.Services;
using PrefixClassName.MsTest;
using Shouldly;

namespace IconPackBuilder.Tests;

[PrefixTestClass]
public class JavaScriptExporterTests
{
    [TestMethod]
    public async Task Save_GeneratesModuleAndDeclarations()
    {
        var context = ExporterTestContext.Create("MyApp.FontIcons");
        var save = new IconGroupInfo("Save", "Save", [new IconInfo("Regular", 0xF03E4, null), new IconInfo("Filled", 0xF03E5, null)]);
        var back = new IconGroupInfo("ArrowLeft", "Arrow Left", [new IconInfo("Regular", 0xF0048, 0x100048)]);

        await JavaScriptExporter.Instance.SaveAsync(context with {
            Icons = [
                new ExportIconInfo("Save", save.Icons[0]),
                new ExportIconInfo("Save", save.Icons[1]),
                new ExportIconInfo("Back", back.Icons[0]),
            ],
        });

        string js = File.ReadAllText(Path.Combine(context.ExportDir.PathExport, "FontIcons.js"));
        string dts = File.ReadAllText(Path.Combine(context.ExportDir.PathExport, "FontIcons.d.ts"));

        js.ShouldContain("export const FontIcons = Object.freeze({");
        js.ShouldContain("Save: Object.freeze({ className: \"font-icons-save\", glyph: \"\\u{F03E4}\", rtlGlyph: \"\\u{F03E4}\" }),");
        js.ShouldContain("SaveFilled: Object.freeze({ className: \"font-icons-save-filled\", glyph: \"\\u{F03E5}\", rtlGlyph: \"\\u{F03E5}\" }),");
        js.ShouldContain("Back: Object.freeze({ className: \"font-icons-back\", glyph: \"\\u{F0048}\", rtlGlyph: \"\\u{100048}\" }),");
        js.ShouldContain("export function getGlyph(icon, rtl = false)");

        // Members are emitted in name order so that regenerated files diff cleanly.
        js.IndexOf("Back:", StringComparison.Ordinal).ShouldBeLessThan(js.IndexOf("Save:", StringComparison.Ordinal));

        dts.ShouldContain("export interface IconGlyph {");
        dts.ShouldContain("export declare const FontIcons: {");
        dts.ShouldContain("readonly Back: IconGlyph;");
        dts.ShouldContain("readonly SaveFilled: IconGlyph;");
        dts.ShouldContain("export declare function getGlyph(icon: IconGlyph, rtl?: boolean): string;");
    }
}
