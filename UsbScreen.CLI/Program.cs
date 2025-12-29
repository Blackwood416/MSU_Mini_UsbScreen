using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UsbScreen.Utils;

namespace UsbScreen
{
    class Program
    {
        static SerialPort? ser;

        /// <summary>
        /// Determines the serial port to use based on priority:
        /// 1. Command line argument (--port / -p)
        /// 2. Saved configuration
        /// 3. Interactive user selection (first run)
        /// </summary>
        static string? DetermineSerialPort(string? cliPort)
        {
            // Priority 1: Command line argument
            if (!string.IsNullOrEmpty(cliPort))
            {
                Console.WriteLine($"Using serial port from command line: {cliPort}");
                return cliPort;
            }

            // Priority 2: Saved configuration
            var savedPort = ConfigHelper.GetSavedSerialPort();
            if (!string.IsNullOrEmpty(savedPort))
            {
                // Verify the saved port still exists
                var availablePorts = SerialPort.GetPortNames();
                if (availablePorts.Contains(savedPort))
                {
                    Console.WriteLine($"Using saved serial port: {savedPort}");
                    return savedPort;
                }
                else
                {
                    Console.WriteLine($"Warning: Saved port '{savedPort}' is no longer available.");
                }
            }

            // Priority 3: Interactive selection
            return PromptForSerialPortSelection();
        }

        /// <summary>
        /// Prompts the user to select a serial port from available options.
        /// </summary>
        static string? PromptForSerialPortSelection()
        {
            var availablePorts = SerialPort.GetPortNames();

            if (availablePorts.Length == 0)
            {
                Console.WriteLine("Error: No serial ports found on this system.");
                Console.WriteLine("Please connect your USB screen device and try again.");
                return null;
            }

            // Add common Linux USB serial device paths if not already included
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var linuxPorts = new List<string>(availablePorts);
                var commonLinuxPorts = new[] { "/dev/ttyACM0", "/dev/ttyACM1", "/dev/ttyUSB0", "/dev/ttyUSB1" };
                foreach (var port in commonLinuxPorts)
                {
                    if (File.Exists(port) && !linuxPorts.Contains(port))
                    {
                        linuxPorts.Add(port);
                    }
                }
                availablePorts = linuxPorts.ToArray();
            }

            Console.WriteLine();
            Console.WriteLine("=== Serial Port Selection ===");
            Console.WriteLine("Available serial ports:");
            Console.WriteLine();

            for (int i = 0; i < availablePorts.Length; i++)
            {
                Console.WriteLine($"  [{i + 1}] {availablePorts[i]}");
            }
            Console.WriteLine();

            while (true)
            {
                Console.Write($"Select port (1-{availablePorts.Length}): ");
                var input = Console.ReadLine();

                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= availablePorts.Length)
                {
                    var selectedPort = availablePorts[choice - 1];
                    
                    // Ask if user wants to save this selection
                    Console.Write("Save this selection for future use? (Y/n): ");
                    var saveInput = Console.ReadLine()?.Trim().ToLowerInvariant();
                    
                    if (string.IsNullOrEmpty(saveInput) || saveInput == "y" || saveInput == "yes")
                    {
                        ConfigHelper.SaveSerialPort(selectedPort);
                        Console.WriteLine($"Serial port '{selectedPort}' saved to configuration.");
                    }

                    return selectedPort;
                }

                Console.WriteLine("Invalid selection. Please try again.");
            }
        }

        /// <summary>
        /// Lists all available serial ports.
        /// </summary>
        static void ListSerialPorts()
        {
            var ports = SerialPort.GetPortNames();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var linuxPorts = new List<string>(ports);
                var commonLinuxPorts = new[] { "/dev/ttyACM0", "/dev/ttyACM1", "/dev/ttyUSB0", "/dev/ttyUSB1" };
                foreach (var port in commonLinuxPorts)
                {
                    if (File.Exists(port) && !linuxPorts.Contains(port))
                    {
                        linuxPorts.Add(port);
                    }
                }
                ports = linuxPorts.ToArray();
            }

            Console.WriteLine("Available serial ports:");
            if (ports.Length == 0)
            {
                Console.WriteLine("  (none found)");
            }
            else
            {
                foreach (var port in ports)
                {
                    Console.WriteLine($"  {port}");
                }
            }

            var savedPort = ConfigHelper.GetSavedSerialPort();
            if (!string.IsNullOrEmpty(savedPort))
            {
                Console.WriteLine();
                Console.WriteLine($"Saved port: {savedPort}");
            }
        }

        static async Task<int> Main(string[] args)
        {
            // Define global port option
            var portOption = new Option<string?>(
                name: "--port",
                description: "Serial port to use (e.g., COM3 on Windows, /dev/ttyACM0 on Linux)"
            );
            portOption.AddAlias("-p");

            var listPortsOption = new Option<bool>(
                name: "--list-ports",
                description: "List all available serial ports and exit"
            );
            listPortsOption.AddAlias("-L");

            var resetPortOption = new Option<bool>(
                name: "--reset-port",
                description: "Reset saved serial port configuration"
            );

            // Check for --list-ports early (before full parsing)
            if (args.Contains("--list-ports") || args.Contains("-L"))
            {
                ListSerialPorts();
                return 0;
            }

            // Check for --reset-port
            if (args.Contains("--reset-port"))
            {
                ConfigHelper.SaveSerialPort("");
                Console.WriteLine("Serial port configuration has been reset.");
                return 0;
            }

            // Check for help/version arguments - skip serial port initialization
            bool isHelpOrVersion = args.Length == 0 ||
                args.Contains("--help") || args.Contains("-h") || args.Contains("-?") ||
                args.Contains("--version");
            
            // Also check for subcommand help (e.g., "image --help")
            if (!isHelpOrVersion && args.Length >= 2)
            {
                isHelpOrVersion = args.Contains("--help") || args.Contains("-h") || args.Contains("-?");
            }

            // Skip serial port initialization for help/version commands
            if (!isHelpOrVersion)
            {
                // Parse port from args (simple extraction before full command parsing)
                string? cliPort = null;
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "--port" || args[i] == "-p")
                    {
                        cliPort = args[i + 1];
                        break;
                    }
                }

                // Determine serial port
                var portName = DetermineSerialPort(cliPort);
                if (string.IsNullOrEmpty(portName))
                {
                    Console.WriteLine("Error: No serial port selected. Exiting.");
                    return 1;
                }

                // Initialize serial port
                ser = new SerialPort
                {
                    PortName = portName,
                    BaudRate = 19200,
                    ReadTimeout = 500,
                };

                try
                {
                    SerialPortUtil.InitConnection(ser);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: Failed to open serial port '{portName}': {ex.Message}");
                    Console.WriteLine("Use --list-ports to see available ports, or --port to specify a different port.");
                    return 1;
                }
            }

            var cancellationToken = new CancellationTokenSource().Token;
            cancellationToken.Register(() =>
            {
                Console.WriteLine("Exiting...");
                Console.WriteLine("Closing serial port connection...");
                try
                {
                    ser?.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Closing serial port connection failed...");
                }
                Environment.Exit(0);
            });

            // Define Options
            var delayOption = new Option<int?>(
                name: "--delay",
                description: "Delay time to show the image or text."
            );
            delayOption.AddAlias("-d");
            var imageScaleOption = new Option<string?>
            (
                name: "--scale",
                description: "The scale of the image."
            ).FromAmong("center", "top", "bottom", "left", "right", "top-left", "top-right", "bottom-left", "bottom-right");
            imageScaleOption.AddAlias("-l");
            var imageModeOption = new Option<string?>
            (
                name: "--mode",
                description: "The mode of the image."
            ).FromAmong("pad", "box", "stretch", "crop");
            imageModeOption.AddAlias("-m");
            var backgroundColorOption = new Option<string?>
            (
                name: "--background-color",
                description: "Specific the background color to show the text."
            ).FromAmong("red", "green", "blue", "yellow", "white", "black", "orange", "purple", "cyan", "gray", "pink");
            backgroundColorOption.AddAlias("-b");

            // Define Arguments
            var imageArg = new Argument<FileInfo>(
                name: "image",
                description: "The image file to display"
            );
            var textArgs = new Argument<string[]>(
                name: "texts",
                description: "The texts to display (multiple lines)"
            );
            var flashArgs = new Argument<FileInfo>(
                name: "binary",
                description: "The image file to flash to the screen's firmware"
            );

            // Define Commands
            var rootCommand = new RootCommand("A simple command line tool to show image or text on Mori Ateliers USB LCD screen.");
            var imageCommand = new Command("image", "Show image on the screen.");
            var textCommand = new Command("text", "Show text on the screen.");
            var flashCommand = new Command("flash", "Flash images to the screen's firmware.");

            // Add global options to RootCommand
            rootCommand.AddGlobalOption(portOption);
            rootCommand.AddOption(listPortsOption);
            rootCommand.AddOption(resetPortOption);

            // Add Commands to RootCommand
            rootCommand.AddCommand(imageCommand);
            rootCommand.AddCommand(textCommand);
            rootCommand.AddCommand(flashCommand);


            imageCommand.AddArgument(imageArg);
            imageCommand.AddOption(imageScaleOption);
            imageCommand.AddOption(imageModeOption);
            imageCommand.AddOption(delayOption);

            textCommand.AddArgument(textArgs);

            // Add per-text options
            var textFontsOption = new Option<string[]?>(
                name: "--font",
                description: "Fonts for each text line");
            textFontsOption.AddAlias("-f");
            var textColorsOption = new Option<string[]?>(
                name: "--text-color",
                description: "Text colors for each line");
            textColorsOption.AddAlias("-c");
            var textSizesOption = new Option<int[]?>(
                name: "--text-size",
                description: "Text sizes for each line");
            textSizesOption.AddAlias("-s");

            textCommand.AddOption(textFontsOption);
            textCommand.AddOption(textColorsOption);
            textCommand.AddOption(textSizesOption);
            textCommand.AddOption(backgroundColorOption);
            textCommand.AddOption(delayOption);

            var flashTypeOption = new Option<string>(
                name: "--type",
                description: "The type of content to flash (firmware, background, album, animation)."
            ).FromAmong("firmware", "background", "album", "animation");
            flashTypeOption.SetDefaultValue("firmware");
            flashTypeOption.AddAlias("-t");

            flashCommand.AddArgument(flashArgs);
            flashCommand.AddOption(flashTypeOption);

            // Add Handlers
            imageCommand.SetHandler((image, scale, mode, delay) =>
            {
                if (delay.HasValue)
                {
                    Thread.Sleep(delay.Value);
                }

                OptimizeImage(image, scale ?? "center", mode ?? "Pad");
            }, imageArg, imageScaleOption, imageModeOption, delayOption);

            textCommand.SetHandler((texts, fonts, textColors, textSizes, bgColor, delay) =>
            {
                try
                {
                    if (delay.HasValue)
                    {
                        Thread.Sleep(delay.Value);
                    }

                    var fontCollection = new FontCollection();
                    var bg = Color.Parse(bgColor ?? "black");

                    // Create list of text styles
                    var textStyles = new List<TextStyle>();
                    // if (fontStream == null)
                    // {
                    //     Console.WriteLine("Error: Arial font not found.");
                    // }
                    for (int j = 0; j < texts.Length; j++)
                    {
                        var font = fonts != null && j < fonts.Length ? fonts[j] : "Default";
                        var textColor = textColors != null && j < textColors.Length ? textColors[j] : null;
                        var textSize = textSizes != null && j < textSizes.Length ? textSizes[j] : 16;
                        FontFamily loadedFont;
                        Font textFont;
                        if (font == "Default")
                        {
                            using var fontStream = FontHelper.GetDefaultFontStream();
                            if (fontStream == null)
                            {
                                Console.WriteLine("Error: Arial font not found.");
                            }
                            loadedFont = fontCollection.Add(fontStream!);
                            textFont = loadedFont.CreateFont(textSize);
                        }
                        else
                        {
                            loadedFont = fontCollection.Add(font!);
                            textFont = new Font(loadedFont, textSize);
                        }
                        var fg = Color.Parse(textColor ?? "white");

                        textStyles.Add(new TextStyle(texts[j], textFont, fg));
                    }

                    ImageUtil.ShowMultiText(ser!, textStyles, bg);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error displaying text: {ex.Message}");
                }
            }, textArgs, textFontsOption, textColorsOption, textSizesOption, backgroundColorOption, delayOption);


            flashCommand.SetHandler((image, typeStr) =>
            {
                try
                {
                    if (image != null && image.Exists)
                    {
                        FlashImageType type = FlashImageType.Firmware;
                        if (Enum.TryParse<FlashImageType>(typeStr, true, out var parsedType))
                        {
                            type = parsedType;
                        }

                        Console.WriteLine($"Flashing {image.Name} as {type}...");
                        
                        // Create progress reporter for console
                        var progress = new Progress<(int current, int total, string message)>(p =>
                        {
                            if (p.total > 0)
                            {
                                int percent = (int)((double)p.current / p.total * 100);
                                Console.Write($"\r{p.message} ({percent}%)    ");
                            }
                            else
                            {
                                Console.WriteLine(p.message);
                            }
                        });
                        
                        FlashUtil.WriteImageToFlash(ser!, image, type, progress);
                        Console.WriteLine(); // New line after progress
                        Console.WriteLine($"Successfully flashed {image.Name} to display");
                    }
                    else
                    {
                        Console.WriteLine("Invalid image file specified");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError flashing image: {ex.Message}");
                }
            }, flashArgs, flashTypeOption);

            return await rootCommand.InvokeAsync(args);
        }
        static void OptimizeImage(FileInfo image, string scale, string mode)
        {
            try
            {
                using var img = Image.Load<Rgb24>(image.FullName);

                // Calculate target rectangle based on scale
                // Console.WriteLine($"Resizing image from {img.Width}x{img.Height} to 160x80 with scale {scale}");

                // Configure resize options to handle both cropping and downsampling
                var resizeOptions = new ResizeOptions
                {
                    Mode = mode switch
                    {
                        "pad" => ResizeMode.Pad,
                        "box" => ResizeMode.BoxPad,
                        "stretch" => ResizeMode.Stretch,
                        "crop" => ResizeMode.Crop,
                        _ => ResizeMode.Pad
                    },
                    Position = GetAnchorPosition(scale),
                    Sampler = KnownResamplers.Lanczos3,
                    Compand = true,
                    PremultiplyAlpha = true,
                    Size = new Size(160, 80)
                };

                // Resize image with cropping and downsampling
                var clone = img.Clone(x => x.Resize(resizeOptions));

                if (image.Extension == ".png")
                {
                    ImageUtil.ShowPng(ser!, clone);
                }
                else if (image.Extension == ".jpg" || image.Extension == ".jpeg")
                {
                    ImageUtil.ShowJpeg(ser!, clone);
                }
                else if (image.Extension == ".gif")
                {
                    ImageUtil.ShowGif(ser!, clone);
                }
                else
                {
                    Console.WriteLine($"Unsupported image format: {image.Extension}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying image: {ex.Message}");
            }
        }

        static AnchorPositionMode GetAnchorPosition(string scale)
        {
            return scale switch
            {
                "top" => AnchorPositionMode.Top,
                "bottom" => AnchorPositionMode.Bottom,
                "left" => AnchorPositionMode.Left,
                "right" => AnchorPositionMode.Right,
                "top-left" => AnchorPositionMode.TopLeft,
                "top-right" => AnchorPositionMode.TopRight,
                "bottom-left" => AnchorPositionMode.BottomLeft,
                "bottom-right" => AnchorPositionMode.BottomRight,
                _ => AnchorPositionMode.Center
            };
        }
    }
}
