using ModelContextProtocol.Server;
using System.ComponentModel;
using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.Fonts;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"Hello from C#: {message}";

    [McpServerTool, Description("Echoes in reverse the message sent by the client.")]
    public static string ReverseEcho(string message) => new string(message.Reverse().ToArray());

    [McpServerTool, Description("Downloads a CSV from Azure Blob Storage, sorts the first column numerically, plots the sorted values, and saves the image locally. Returns the absolute path to the saved image.")]
    public static async Task<string> PlotSortedCsvFromBlob(string connectionString, string containerName, string blobName, string outputImagePath = "sorted_plot.png")
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("connectionString is required", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException("containerName is required", nameof(containerName));
        if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException("blobName is required", nameof(blobName));

        var containerClient = new BlobContainerClient(connectionString, containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
            throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");

        var download = await blobClient.DownloadAsync();
        using var reader = new StreamReader(download.Value.Content);
        var text = await reader.ReadToEndAsync();

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var values = new List<double>();
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length == 0) continue;
            if (double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                values.Add(d);
            // non-numeric values (e.g., header) are skipped
        }

        if (values.Count == 0)
            throw new InvalidOperationException("No numeric values found in the first CSV column.");

        values.Sort();

        double[] ys = values.ToArray();
        // Render a simple line plot using SixLabors.ImageSharp
        int width = 1200, height = 600;
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.Fill(Color.White));

        int marginLeft = 60, marginRight = 20, marginTop = 50, marginBottom = 60;
        int plotWidth = width - marginLeft - marginRight;
        int plotHeight = height - marginTop - marginBottom;

        double min = ys.Min();
        double max = ys.Max();
        double range = Math.Abs(max - min) < double.Epsilon ? 1 : max - min;

        // build points
        var pts = new SixLabors.ImageSharp.PointF[ys.Length];
        for (int i = 0; i < ys.Length; i++)
        {
            float x = marginLeft + (float)i / Math.Max(1, ys.Length - 1) * plotWidth;
            float y = marginTop + (float)((max - ys[i]) / range * plotHeight);
            pts[i] = new SixLabors.ImageSharp.PointF(x, y);
        }

        // draw axes/grid, bars and markers with simple ImageSharp primitives
        image.Mutate(ctx =>
        {
            // y axis (thin rectangle)
            ctx.Fill(Color.Black, new RectangleF(marginLeft - 2, marginTop, 2, plotHeight));
            // x axis
            ctx.Fill(Color.Black, new RectangleF(marginLeft, marginTop + plotHeight, plotWidth, 2));

            // horizontal grid and labels
            var font = SystemFonts.CreateFont("Arial", 10);
            for (int i = 0; i <= 4; i++)
            {
                double t = i / 4.0;
                double yValue = max - t * (max - min);
                int y = marginTop + (int)(t * plotHeight);
                ctx.Fill(Color.LightGray, new RectangleF(marginLeft, y, plotWidth, 1));
                var label = yValue.ToString("G4", CultureInfo.InvariantCulture);
                ctx.DrawText(label, font, Color.Black, new SixLabors.ImageSharp.PointF(2, y - 7));
            }

            // draw bars for each point (simple visualisation of sorted values)
            float slotWidth = plotWidth / (float)ys.Length;
            float barWidth = Math.Max(1, slotWidth * 0.8f);
            for (int i = 0; i < ys.Length; i++)
            {
                float x = marginLeft + i * slotWidth + (slotWidth - barWidth) / 2f;
                float barHeight = (float)((ys[i] - min) / range * plotHeight);
                float yTop = marginTop + (plotHeight - barHeight);
                ctx.Fill(Color.Blue, new RectangleF(x, yTop, barWidth, barHeight));
                // optional marker on top
                ctx.Fill(Color.Red, new EllipsePolygon(x + barWidth / 2f, yTop, 3));
            }

            // title
            var title = $"Sorted values from '{blobName}'";
            var titleFont = SystemFonts.CreateFont("Arial", 14, FontStyle.Bold);
            ctx.DrawText(title, titleFont, Color.Black, new SixLabors.ImageSharp.PointF(marginLeft, 10));
        });

        image.Save(outputImagePath);

        var fullPath = System.IO.Path.GetFullPath(outputImagePath);
        return fullPath;
    }
}
