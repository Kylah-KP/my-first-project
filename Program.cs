using System.Diagnostics;
using System.Drawing;
using System.Text;
using NAudio.Wave;

namespace Program
{
    class ASCII_Gendering
    {
        const string BrightnessLevels = " .-+*wvGHM#&%";
        const string FramesDirectory = "cvf\\frames\\";
        const string AudioFilePath = "cvf\\audio.wav";
        const string FFmpegPath = "ffmpeg.exe";

        static void Main(string[] args)
        {
            int targetWith = Console.WindowWidth -1;
            int targetHeight = Console.WindowHeight - 2;

            Console.WriteLine("Choose an option");
            Console.WriteLine("[1] Gendering video to ASCII");
            Console.WriteLine("[2] Gendering image to ASCII");
            Console.Write("Enter your choice: ");

            string choice = Console.ReadLine();

            if (choice == "1")
            {
                string InputFileName = GetInputFileName(args);

                CleanUpDirectories();
                
                ExtractFrames(InputFileName, targetWith, targetHeight);
                ExtractAudio(InputFileName);
                var frames = ConvertFramesToAscii(targetWith, targetHeight);
                PlayAsciiVideo(frames);
            }
            else if (choice == "2")
            {
                string InputFileName = GetInputFileName(args);

                var image = ConvertImageToAscii(InputFileName, targetHeight);
                Console.WriteLine(image);

                Console.Write("Do you want to save the result to a file? (y/n): ");
                string saveChoice = Console.ReadLine();
                if (saveChoice?.ToLower() == "y")
                {
                   string outputDirectory = "images";
                   if (!Directory.Exists(outputDirectory))
                   {
                      Directory.CreateDirectory(outputDirectory);
                   }
                   Console.Write("Enter name of output file: ");
                   string fileName = Console.ReadLine();

                   string outputFileName = Path.Combine(outputDirectory, fileName + ".txt");
                   File.WriteAllText(outputFileName, image);
                   Console.WriteLine($"ASCII art saved to {outputFileName}");
                }
            }
        }

        static string GetInputFileName(string[] args)
        {
            if (args.Length > 0)
                return args[0];

            Console.Write("Input File: ");
            return Console.ReadLine().Replace("\"", "");
        }

        static void CleanUpDirectories()
        {
            if (Directory.Exists("cvf"))
            {
                if (Directory.Exists(FramesDirectory))
                {
                    Directory.Delete(FramesDirectory, true);
                }
                if (File.Exists(AudioFilePath))
                {
                    File.Delete(AudioFilePath);
                }
            }
            Directory.CreateDirectory(FramesDirectory);
        }

        static string ConvertImageToAscii(string imagePath, int targetWidth)
        {
            using (Bitmap originalBitmap = new Bitmap(imagePath))
            {
                int targetHeight = (int)(originalBitmap.Height / (double)originalBitmap.Width * targetWidth * 0.55); // Adjust for console font ratio
                using (Bitmap resizedBitmap = new Bitmap(originalBitmap, new Size(targetWidth, targetHeight)))
                {
                    StringBuilder asciiBuilder = new StringBuilder();

                    for (int y = 0; y < resizedBitmap.Height; y++)
                    {
                        for (int x = 0; x < resizedBitmap.Width; x++)
                        {
                            Color pixelColor = resizedBitmap.GetPixel(x, y);
                            int brightnessIndex = (int)(pixelColor.GetBrightness() * BrightnessLevels.Length);
                            brightnessIndex = Math.Clamp(brightnessIndex, 0, BrightnessLevels.Length - 1);
                            asciiBuilder.Append(BrightnessLevels[brightnessIndex]);
                        }
                        asciiBuilder.AppendLine();
                    }

                    return asciiBuilder.ToString();
                }
            }
        }

        static void ExtractFrames(string inputFile, int width, int height)
        {
            RunFFmpegProcess($"-i \"{inputFile}\" -vf scale={width}:{height} {FramesDirectory}%0d.bmp");
        }

        static void ExtractAudio(string inputFile)
        {
            RunFFmpegProcess($"-i \"{inputFile}\" {AudioFilePath}");
        }

        static void RunFFmpegProcess(string arguments)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = FFmpegPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit();
            }
        }

        static List<string> ConvertFramesToAscii(int width, int height)
        {
            var frames = new List<string>();
            int frameCount = Directory.GetFiles(FramesDirectory, "*.bmp").Length;

            Console.Clear();
            for (int frameIndex = 1; frameIndex <= frameCount; frameIndex++)
            {
                string fileName = $"{FramesDirectory}{frameIndex}.bmp";
                if (!File.Exists(fileName))
                    break;

                frames.Add(ConvertFrameToAscii(fileName, width, height));
                DisplayProgress(frameIndex, frameCount);
            }
            return frames;
        }

        static string ConvertFrameToAscii(string fileName, int width, int height)
        {
            StringBuilder frameBuilder = new StringBuilder();
            using (Bitmap bitmap = new Bitmap(fileName))
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int brightnessIndex = (int)(bitmap.GetPixel(x, y).GetBrightness() * BrightnessLevels.Length);
                        brightnessIndex = Math.Clamp(brightnessIndex, 0, BrightnessLevels.Length - 1);
                        frameBuilder.Append(BrightnessLevels[brightnessIndex]);
                    }
                    frameBuilder.AppendLine();
                }
            }
            return frameBuilder.ToString();
        }

        static void DisplayProgress(int currentFrame, int totalFrames)
        {
            int barLength = 50;
            int percentage = (int)((currentFrame / (float)totalFrames) * 100);
            int filledLength = (percentage * barLength) / 100;

            string progressBar = new string('#', filledLength) + new string('-', barLength - filledLength);

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"-> [PROGRESS]  Converting to ASCII    [{progressBar}] {percentage}%  ");
        }

        static void PlayAsciiVideo(List<string> frames)
        {
            using (var reader = new AudioFileReader(AudioFilePath))
            using (var waveOut = new WaveOutEvent())
            {
                waveOut.Init(reader);

                Console.WriteLine("\n== Press 'ENTER' to play! ==");
                Console.ReadLine();
                waveOut.Play();

                bool isPlaying = true;

                while (true)
                {
                    // Calculate current frame based on audio playback position
                    int frameIndex = (int)((waveOut.GetPosition() / (float)reader.Length) * frames.Count);
                    if (frameIndex >= frames.Count)
                        break;

                    // Display ASCII frame on console
                    Console.SetCursorPosition(0, 0);
                    Console.Write(frames[frameIndex]);

                    if (Console.KeyAvailable)
                    { // Handle key presses
                        var key = Console.ReadKey(true).Key;

                        if (key == ConsoleKey.Spacebar)
                        {
                            if (isPlaying)
                            {
                                waveOut.Pause();
                                isPlaying = false;
                            }
                            else
                            {
                                waveOut.Play();
                                isPlaying = true;
                            }
                        }
                        else if (key == ConsoleKey.Escape)
                        {
                            waveOut.Stop();
                            break;
                        }
                    }
                    Task.Delay(30); // Pause for a moment to reduce CPU load
                }
            }
            Console.WriteLine("Done. Press any key to close.");
            Console.ReadKey();
        }

    }
}
