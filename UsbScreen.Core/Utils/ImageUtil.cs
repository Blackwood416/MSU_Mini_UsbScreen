using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace UsbScreen.Utils
{
    public static class ImageUtil
    {
        static Image<Rgb24>? textImage;
        static readonly int[] imageBuffer = new int[12800];
        static readonly List<byte> hexData = new();
        public static byte[] Rgb24ToRenderFrame(Image<Rgb24> image)
        {
            byte[] result = new byte[image.Width * image.Height * 2];
            Rgb24ToRenderFrame(image, result);
            return result;
        }

        public static void Rgb24ToRenderFrame(Image<Rgb24> image, byte[] buffer)
        {
            // Big Endian RGB565:
            // Byte 1: RRRRRGGG (R4-R0, G5-G3)
            // Byte 2: GGGBBBBB (G2-G0, B4-B0)

            image.ProcessPixelRows(accessor =>
            {
                int index = 0;
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        ref Rgb24 pixel = ref row[x];
                        
                        // Fast bit manipulation for RGB565
                        buffer[index++] = (byte)((pixel.R & 0xF8) | (pixel.G >> 5));
                        buffer[index++] = (byte)(((pixel.G & 0x1C) << 3) | (pixel.B >> 3));
                    }
                }
            });
        }
        public static void ShowMultiText(SerialPort ser, List<TextStyle> textStyles, Color bgColor)
        {
            textImage = new Image<Rgb24>(160, 80, bgColor);

            int y = 0;
            foreach (var style in textStyles)
            {
                textImage.Mutate(ctx => ctx
                    .DrawText(style.Text, style.Font, style.Color, new PointF(0, y)));

                // Calculate next line position with 1px spacing
                var textHeight = TextMeasurer.MeasureBounds(style.Text, new TextOptions(style.Font)).Height;
                y += (int)textHeight + 1;

                // Stop if we reach the bottom of the screen
                if (y >= 80) break;
            }

            RenderUtil.DrawCall(ser, 0, 0, 160, 80, Rgb24ToRenderFrame(textImage));
        }

        // public static void ShowText(SerialPort ser, string text, int x = 0, int y = 0, Font? font = null, int fontSize = 16, Color? fgColor = null, Color? bgColor = null)
        // {
        //     ShowMultiText(ser, new List<TextStyle>
        //     {
        //         new TextStyle(
        //             text,
        //             font ?? new Font(new FontCollection().Add("./arial.ttf"), fontSize),
        //             fgColor ?? Color.White)
        //     }, bgColor ?? Color.Black);
        // }
        public static void ShowPng(SerialPort ser, Image<Rgb24> image)
        {
            RenderUtil.DrawCall(ser, 0, 0, image.Width, image.Height, Rgb24ToRenderFrame(image));
        }
        public static void ShowJpeg(SerialPort ser, Image<Rgb24> image)
        {
            RenderUtil.DrawCall(ser, 0, 0, image.Width, image.Height, Rgb24ToRenderFrame(image));
        }
        public static void ShowGif(SerialPort ser, Image<Rgb24> image, ResizeOptions? resizeOptions = null, Action? onReady = null, CancellationToken token = default)
        {
            int count = image.Frames.Count;
            var frameDataArray = new byte[count][];

            // Use Parallel.For with limited concurrency to avoid memory spikes
            var parallelOptions = new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(4, System.Environment.ProcessorCount)),
                CancellationToken = token 
            };

            try 
            {
                Parallel.For(0, count, parallelOptions, i =>
                {
                    token.ThrowIfCancellationRequested();
                    
                    using var frameImage = image.Frames.CloneFrame(i);
                    if (resizeOptions != null) frameImage.Mutate(ctx => ctx.Resize(resizeOptions));

                    byte[] localRgbBuffer = new byte[160 * 80 * 2];
                    Rgb24ToRenderFrame(frameImage, localRgbBuffer);
                    
                    frameDataArray[i] = localRgbBuffer;
                });
            }
            catch (OperationCanceledException) { return; }

            // Release original image
            int[] frameDelays = new int[count];
            for(int i = 0; i < count; i++) {
                int d = (int)image.Frames[i].Metadata.GetGifMetadata().FrameDelay * 10;
                frameDelays[i] = d == 0 ? 100 : d;
            }
            image.Dispose();
            System.GC.Collect();

            onReady?.Invoke();

            // PLAYBACK LOOP
            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < count; i++)
                {
                    if (token.IsCancellationRequested) break;
                    
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    RenderUtil.DrawCall(ser, 0, 0, 160, 80, frameDataArray[i]);
                    
                    int sleepTime = frameDelays[i] - (int)sw.ElapsedMilliseconds;
                    if (sleepTime > 0) Thread.Sleep(sleepTime);
                }
            }
        }
    }
}
