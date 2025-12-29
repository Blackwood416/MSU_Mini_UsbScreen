using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;

namespace UsbScreen.Utils
{
    public enum FlashImageType
    {
        Firmware,
        Background,    // Address 3826 (CLK_BG)
        Album,         // Address 3926 (PH1)
        Animation      // Address 0, 36 frames
    }

    public static class FlashUtil
    {
        private const int ADDR_FIRMWARE = 0x0000;
        private const int ADDR_ANIMATION = 0x0000;
        private const int ADDR_BACKGROUND = 3826; // Page 3826
        private const int ADDR_ALBUM = 3926; // Page 3926
        private const int MAX_FLASH_PAGES = 4096; // Total flash capacity in pages (256 bytes per page)

        // Note: Python script addresses are Page Numbers.

        public static void WriteToFlash(SerialPort ser, FileInfo file, int address, IProgress<(int current, int total, string message)>? progress = null)
        {
            byte[] data = File.ReadAllBytes(file.FullName);
            WritePagesToFlashFast(ser, data, address, progress);
        }

        public static void WriteImageToFlash(SerialPort ser, FileInfo file, FlashImageType type, IProgress<(int current, int total, string message)>? progress = null)
        {
            try
            {
                // Wake up device before flash operation
                progress?.Report((0, 0, "Waking up device..."));
                SerialPortUtil.SendWakeUp(ser);
                
                int address = 0;
                switch (type)
                {
                    case FlashImageType.Background:
                        address = ADDR_BACKGROUND;
                        break;
                    case FlashImageType.Album:
                        address = ADDR_ALBUM;
                        break;
                    case FlashImageType.Animation:
                    case FlashImageType.Firmware:
                        address = ADDR_ANIMATION;
                        break;
                }

                if (type == FlashImageType.Animation)
                {
                    // Handle Animation (GIF or set of images)
                    // If file is GIF, extract frames.
                    // Python collects 36 frames of 160x80.
                    ProcessAndWriteAnimation(ser, file, address, progress);
                    return;
                }

                if (type == FlashImageType.Firmware && (file.Extension.ToLower() == ".bin"))
                {
                    WriteToFlash(ser, file, address, progress);
                    return;
                }

                // Single Image (Startup/Background)
                using var image = Image.Load<Rgb24>(file.FullName);

                // Python logic for resizing to 160x80
                // If w >= h*2: resize to (h*scale, 80), crop 160 width
                // Else: resize to (160, w*scale), crop 80 height
                // Basically "Cover" mode to 160x80

                using var resized = ProcessImageForFlash(image);
                var renderData = ImageUtil.Rgb24ToRenderFrame(resized);

                // Python uses Write_Flash_hex_fast which erases then writes
                WritePagesToFlashFast(ser, renderData, address, progress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing image to flash: {ex.Message}");
            }
        }

        private static Image<Rgb24> ProcessImageForFlash(Image<Rgb24> source)
        {
            // Target 160x80
            int targetW = 160;
            int targetH = 80;

            // Logic mimicking Python's Writet_Photo_Path1
            // But lets just use generalized "Cover" resizing which achieves the same result simpler
            // Python:
            // if im1.width >= (im1.height * 2): 
            //    im2 = im1.resize((int(80 * im1.width / im1.height), 80))
            //    ... crop center 160
            // else:
            //    im2 = im1.resize((160, int(160 * im1.height / im1.width)))
            //    ... crop center 80

            var clone = source.Clone(x => x.Resize(new ResizeOptions
            {
                Size = new Size(targetW, targetH),
                Mode = ResizeMode.Crop, // Crop fills the target size, preserving aspect ratio
                Position = AnchorPositionMode.Center
            }));

            return clone;
        }

        private static void ProcessAndWriteAnimation(SerialPort ser, FileInfo file, int address, IProgress<(int current, int total, string message)>? progress = null)
        {
            // If file is GIF, we likely want to use its frames
            // Python script expects 36 frames.
            // We'll support GIF with N frames.

            using var image = Image.Load<Rgb24>(file.FullName);
            int frameCount = image.Frames.Count;
            // Python uses 36 frames fixed loop. We should probably accept what we have, 
            // but the device might expect exactly 36? 
            // Python script: Loop i 0..36. If files missing, abort. 
            // Writes all to one bytebuffer.

            // We will convert all frames available in GIF.

            List<byte> totalData = new List<byte>();

            for (int i = 0; i < frameCount; i++)
            {
                using var frame = image.Frames.CloneFrame(i);
                using var processed = ProcessImageForFlash(frame);
                var bytes = ImageUtil.Rgb24ToRenderFrame(processed);
                totalData.AddRange(bytes);
            }

            // Note: If frameCount is large, this is huge. 
            // Address 0 is typically full flash?
            // Python writes "Img_data_use" which accumulates all frames.

            WritePagesToFlashFast(ser, totalData.ToArray(), address, progress);
        }

        public static void WriteHexToFlash(SerialPort ser, FileInfo file, int address)
        {
            try
            {
                byte[] hexData = File.ReadAllBytes(file.FullName);
                WritePagesToFlash(ser, hexData, address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing hex to flash: {ex.Message}");
            }
        }

        public static void WritePagesToFlash(SerialPort ser, byte[] data, int address)
        {
            try
            {
                // Align to 256 bytes
                int totalPages = (data.Length + 255) / 256;

                // Enforce flash capacity limit
                if (address + totalPages > MAX_FLASH_PAGES)
                {
                    totalPages = Math.Max(0, MAX_FLASH_PAGES - address);
                    if (totalPages == 0) throw new Exception("Flash address out of range or no space left.");
                }

                // Erase First
                EraseFlash(ser, address, totalPages);

                for (int i = 0; i < totalPages; i++)
                {
                    byte[] pageData = new byte[256];
                    int sourceIdx = i * 256;
                    int length = Math.Min(256, data.Length - sourceIdx);
                    Array.Copy(data, sourceIdx, pageData, 0, length);

                    // Fill remaining with 0xFF
                    for (int k = length; k < 256; k++) pageData[k] = 0xFF;

                    WritePagePacketized(ser, pageData, address + i, 1, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing pages to flash: {ex.Message}");
            }
        }

        public static void WritePagesToFlashFast(SerialPort ser, byte[] data, int address, IProgress<(int current, int total, string message)>? progress = null)
        {
            try
            {
                int totalPages = (data.Length + 255) / 256;

                // Enforce flash capacity limit
                if (address + totalPages > MAX_FLASH_PAGES)
                {
                    totalPages = Math.Max(0, MAX_FLASH_PAGES - address);
                    if (totalPages == 0) throw new Exception("Flash address out of range or no space left.");
                    progress?.Report((0, totalPages, $"Warning: Data truncated to fit {MAX_FLASH_PAGES} pages limit."));
                }

                // Erase First
                progress?.Report((0, totalPages, $"Erasing {totalPages} pages..."));
                EraseFlash(ser, address, totalPages);

                for (int i = 0; i < totalPages; i++)
                {
                    progress?.Report((i + 1, totalPages, $"Writing page {i + 1}/{totalPages}"));
                    
                    byte[] pageData = new byte[256];
                    int sourceIdx = i * 256;
                    int length = Math.Min(256, data.Length - sourceIdx);
                    Array.Copy(data, sourceIdx, pageData, 0, length);
                    for (int k = length; k < 256; k++) pageData[k] = 0xFF;

                    WritePagePacketized(ser, pageData, address + i, 1, true);
                }
                
                progress?.Report((totalPages, totalPages, "Flash complete!"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fast writing pages to flash: {ex.Message}");
                throw; // Re-throw so caller knows it failed
            }
        }

        private static void WritePagePacketized(SerialPort ser, byte[] page256, int pageAddress, int pageCount, bool fastMode)
        {
            // Python approach: Combine all commands into one bytearray and send ONCE.
            // 64 packets * 6 bytes + 1 commit packet * 6 bytes = 390 bytes.

            int packetSize = 6;
            int totalPackets = 64 + 1;
            byte[] buffer = new byte[totalPackets * packetSize];
            int offset = 0;

            // 1. Fill Buffer with 64 packets of Cmd 0x04
            for (int i = 0; i < 64; i++)
            {
                // Cmd 0x04
                buffer[offset++] = 0x04;
                buffer[offset++] = (byte)i;
                buffer[offset++] = page256[i * 4 + 0];
                buffer[offset++] = page256[i * 4 + 1];
                buffer[offset++] = page256[i * 4 + 2];
                buffer[offset++] = page256[i * 4 + 3];
            }

            // 2. Commit Buffer to Flash (Cmd 0x03)
            buffer[offset++] = 0x03;
            buffer[offset++] = fastMode ? (byte)0x03 : (byte)0x01;
            buffer[offset++] = (byte)(pageAddress >> 16);
            buffer[offset++] = (byte)((pageAddress >> 8) & 0xFF);
            buffer[offset++] = (byte)(pageAddress & 0xFF);
            buffer[offset++] = (byte)pageCount;

            // Send all at once
            SerialPortUtil.WriteToSerial(buffer, ser);

            // Wait for completion (Python reads back loop)
            SerialPortUtil.WaitForResponse(ser);
        }

        public static void EraseFlash(SerialPort ser, int address, int length)
        {
            // Python Erase_Flash_page:
            // Cmd 3, Op 2
            // Data1 = (add % 65536) // 256
            // Data2 = (add % 65536) % 256
            // Data1 = (size % 65536) // 256
            // Data2 = (size % 65536) % 256

            // It sends 6 bytes: 03 02 AM AL SH SL

            byte[] cmd = new byte[6];
            cmd[0] = 0x03;
            cmd[1] = 0x02;
            cmd[2] = (byte)((address >> 8) & 0xFF);
            cmd[3] = (byte)(address & 0xFF);
            cmd[4] = (byte)((length >> 8) & 0xFF);
            cmd[5] = (byte)(length & 0xFF);

            SerialPortUtil.WriteToSerial(cmd, ser);

            // Wait for completion
            SerialPortUtil.WaitForResponse(ser);
        }
    }
}
