using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PiHoleTray;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Run with --generate-icon to create app.ico (used by the .csproj ApplicationIcon)
        if (args.Contains("--generate-icon"))
        {
            GenerateAppIcon();
            return;
        }

        using var mutex = new Mutex(true, "PiHoleTrayMutex", out bool createdNew);
        if (!createdNew)
            return;

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var app = new TrayApp();
        Application.Run(app);
    }

    private static void GenerateAppIcon()
    {
        // Generates app.ico in the current working directory.
        // Run once before building: dotnet run -- --generate-icon
        var outPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        var sizes   = new[] { 256, 128, 64, 48, 32, 16 };
        var bitmaps = sizes.Select(s => IconRenderer.Render("enabled", s)).ToArray();

        using var ms  = new System.IO.MemoryStream();
        using var bw  = new System.IO.BinaryWriter(ms);
        int dataOffset = 6 + sizes.Length * 16;

        var pngDatas = bitmaps.Select(b => {
            using var tmp = new System.IO.MemoryStream();
            b.Save(tmp, System.Drawing.Imaging.ImageFormat.Png);
            return tmp.ToArray();
        }).ToArray();

        // ICONDIR
        bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);
        int offset = dataOffset;
        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)(sz >= 256 ? 0 : sz));
            bw.Write((byte)0); bw.Write((byte)0);
            bw.Write((short)1); bw.Write((short)32);
            bw.Write(pngDatas[i].Length);
            bw.Write(offset);
            offset += pngDatas[i].Length;
        }
        foreach (var data in pngDatas) bw.Write(data);

        System.IO.File.WriteAllBytes(outPath, ms.ToArray());
        Console.WriteLine($"Generated: {outPath}");

        foreach (var b in bitmaps) b.Dispose();
    }
}
