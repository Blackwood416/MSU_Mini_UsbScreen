
using System;
using System.IO.Ports;
using System.Threading;

namespace UsbScreen.Utils
{
    public static class SerialPortUtil
    {
        public static int WriteToSerial(byte[] data, SerialPort? ser)
        {
            return WriteToSerial(data.AsSpan(), ser);
        }

        public static int WriteToSerial(Span<byte> data, SerialPort? ser)
        {
            try
            {
                if (ser != null)
                {
                    ser.Write(data.ToArray(), 0, data.Length);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine($"{ser?.PortName} 串口数据发送异常");
                return 1;
            }
        }

        public static byte[]? ReadFromSerial(SerialPort? ser)
        {
            try
            {
                if (ser != null)
                {
                    int bytesToRead = ser.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        byte[] buffer = new byte[bytesToRead];
                        ser.Read(buffer, 0, bytesToRead);
                        // Console.WriteLine($"{ser.PortName} 串口数据接收成功"); // Reduce noise
                        return buffer;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine($"{ser?.PortName} 串口数据接收异常");
                return null;
            }
            return null;
        }

        public static void WaitForResponse(SerialPort ser, int timeoutMs = 2000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (ser.BytesToRead > 0)
                {
                    ReadFromSerial(ser); // Consume buffer
                    return;
                }
                Thread.Sleep(10);
            }
            // Timeout is acceptable if device is silent, just log warning if critical
            // Console.WriteLine("Wait response timeout (normal if device silent)");
        }
        public static void InitConnection(SerialPort? ser)
        {
            if (ser == null) return;
            
            // Only open if not already open
            if (!ser.IsOpen)
            {
                ser.Open();
            }
            
            try
            {
                // Clear any stale data in buffers
                ser.DiscardInBuffer();
                ser.DiscardOutBuffer();
                
                // Send MSNCN handshake: 0x00 'M' 'S' 'N' 'C' 'N'
                WriteToSerial([0x00, 0x4D, 0x53, 0x4E, 0x43, 0x4E], ser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine($"{ser?.PortName} 串口数据发送异常");
            }
            Thread.Sleep(250);
            
            // Consume any response
            ReadFromSerial(ser);
        }
        
        /// <summary>
        /// Send wake-up signal without reopening port. Used for retry logic.
        /// </summary>
        public static void SendWakeUp(SerialPort? ser)
        {
            if (ser == null || !ser.IsOpen) return;
            
            try
            {
                // Clear buffers first
                ser.DiscardInBuffer();
                ser.DiscardOutBuffer();
                
                // Send MSNCN handshake
                WriteToSerial([0x00, 0x4D, 0x53, 0x4E, 0x43, 0x4E], ser);
                Thread.Sleep(250);
                
                // Consume response
                ReadFromSerial(ser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendWakeUp error: {ex.Message}");
            }
        }
    }
}
