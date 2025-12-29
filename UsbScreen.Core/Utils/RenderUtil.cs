using System;
using System.IO.Ports;

namespace UsbScreen.Utils
{
    public static class RenderUtil
    {
        public static void DrawCall(SerialPort ser, int positionX, int positionY, int width, int height, byte[] frameData)
        {
            // Try up to 2 times
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    if (attempt > 0) SerialPortUtil.SendWakeUp(ser);

                    // 1. Set Area
                    SendCmdNoWait(ser, 0x02, 0x00, positionX >> 8, positionX & 0xFF, positionY >> 8, positionY & 0xFF);
                    SendCmdNoWait(ser, 0x02, 0x01, width >> 8, width & 0xFF, height >> 8, height & 0xFF);

                    // 2. Init Write
                    if (!SendCmdAndWaitAck(ser, 0x02, 0x03, 0x07, 0x00, 0x00, 0x00))
                    {
                        if (attempt == 0) continue;
                        return;
                    }

                    // 3. Send Data
                    for (int i = 0; i < frameData.Length; i += 256)
                    {
                        int chunkSize = Math.Min(256, frameData.Length - i);
                        byte[] chunk = new byte[256];
                        Array.Copy(frameData, i, chunk, 0, chunkSize);
                        for (int k = chunkSize; k < 256; k++) chunk[k] = 0xFF;

                        SendChunkToLCD(ser, chunk, chunkSize);
                    }
                    return;
                }
                catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
            }
        }

        private static void SendChunkToLCD(SerialPort ser, byte[] data256, int validSize)
        {
            byte[] packetBuffer = new byte[390];
            int offset = 0;

            for (int i = 0; i < 64; i++)
            {
                packetBuffer[offset++] = 0x04;
                packetBuffer[offset++] = (byte)i;
                packetBuffer[offset++] = data256[i * 4 + 0];
                packetBuffer[offset++] = data256[i * 4 + 1];
                packetBuffer[offset++] = data256[i * 4 + 2];
                packetBuffer[offset++] = data256[i * 4 + 3];
            }

            packetBuffer[offset++] = 0x02;
            packetBuffer[offset++] = 0x03;
            packetBuffer[offset++] = 0x08;
            packetBuffer[offset++] = (byte)(validSize >> 8);
            packetBuffer[offset++] = (byte)(validSize & 0xFF);
            packetBuffer[offset++] = 0x00;

            SerialPortUtil.WriteToSerial(packetBuffer, ser);
        }

        private static bool SendCmdAndWaitAck(SerialPort ser, byte b0, byte b1, int b2, int b3, int b4, int b5)
        {
            byte[] cmd = new byte[6];
            cmd[0] = b0;
            cmd[1] = b1;
            cmd[2] = (byte)b2;
            cmd[3] = (byte)b3;
            cmd[4] = (byte)b4;
            cmd[5] = (byte)b5;
            SerialPortUtil.WriteToSerial(cmd, ser);
            
            // Wait and check for response - match Python's 1ms poll interval
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 2000)
            {
                if (ser.BytesToRead > 0)
                {
                    var resp = SerialPortUtil.ReadFromSerial(ser);
                    if (resp != null && resp.Length >= 2)
                    {
                        // Check echo of first 2 bytes (Matches Python logic)
                        if (resp[0] == b0 && resp[1] == b1) return true;
                    }
                    // Python fails immediately if mismatch
                    return false;
                }
                System.Threading.Thread.Sleep(1); // Match Python's 0.001 second sleep
            }
            return false; // Timeout
        }
        
        private static void SendCmdNoWait(SerialPort ser, byte b0, byte b1, int b2, int b3, int b4, int b5)
        {
            byte[] cmd = new byte[6];
            cmd[0] = b0;
            cmd[1] = b1;
            cmd[2] = (byte)b2;
            cmd[3] = (byte)b3;
            cmd[4] = (byte)b4;
            cmd[5] = (byte)b5;
            SerialPortUtil.WriteToSerial(cmd, ser);
        }
    }
}
