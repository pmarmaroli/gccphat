using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NAudio.Wave;
using gccphat_core;
using System.Collections.Concurrent;


namespace gccphat
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 5)
                {
                    Console.WriteLine("Usage: gccphat.exe <audioFilePath> <bufferSize> <fmin> <fmax> <outputToConsole>");
                    Console.WriteLine("  <audioFilePath> - Path to the stereo audio file.");
                    Console.WriteLine("  <bufferSize> - Buffer size in samples (must be a power of two).");
                    Console.WriteLine("  <fmin> - Minimum frequency in Hz.");
                    Console.WriteLine("  <fmax> - Maximum frequency in Hz.");
                    Console.WriteLine("  <outputToConsole> - Flag to output results to console (true/false).");
                    return;
                }

                string filePath = args[0];
                if (!int.TryParse(args[1], out int bufferSize))
                {
                    Console.WriteLine("Invalid buffer size.");
                    return;
                }

                if (!int.TryParse(args[2], out int fmin))
                {
                    Console.WriteLine("Invalid fmin.");
                    return;
                }

                if (!int.TryParse(args[3], out int fmax))
                {
                    Console.WriteLine("Invalid fmax.");
                    return;
                }

                if (!bool.TryParse(args[4], out bool outputToConsole))
                {
                    Console.WriteLine("Invalid outputToConsole flag.");
                    return;
                }

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("The provided file path does not exist.");
                    return;
                }

                (double[] leftChannel, double[] rightChannel, int fs) = ReadStereoAudio(filePath);

                Stopwatch stopwatch = Stopwatch.StartNew();

                List<double> timeDelays = ComputeTimeDelays(leftChannel, rightChannel, bufferSize, fs, fmin, fmax);

                stopwatch.Stop();

                if (outputToConsole)
                {
                    foreach (var delay in timeDelays)
                    {
                        Console.WriteLine(delay);
                    }
                }
                else
                {
                    string outputFilePath = Path.Combine(Path.GetDirectoryName(filePath),
                        Path.GetFileNameWithoutExtension(filePath) + "_timedelay_ms_vs_time_s.csv");

                    using (var writer = new StreamWriter(outputFilePath))
                    {
                        writer.WriteLine("Time (s);Time Delay (ms)");
                        for (int i = 0; i < timeDelays.Count; i++)
                        {
                            double timeSec = (double)i * bufferSize / fs;
                            writer.WriteLine($"{timeSec:F6};{timeDelays[i]:F3}");
                        }
                    }

                    Console.WriteLine("Time delays written to CSV file successfully.");
                }

                Console.WriteLine($"Execution time: {stopwatch.Elapsed.TotalSeconds:F3} seconds");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static (double[] leftChannel, double[] rightChannel, int sampleRate) ReadStereoAudio(string filePath)
        {
            using (var reader = new AudioFileReader(filePath))
            {
                if (reader.WaveFormat.Channels != 2)
                {
                    throw new ArgumentException("The input file must be a stereo audio file.");
                }

                int sampleRate = reader.WaveFormat.SampleRate;

                int sampleCount = (int)reader.Length / sizeof(float);
                float[] samples = new float[sampleCount];
                reader.Read(samples, 0, sampleCount);

                double[] leftChannel = new double[sampleCount / 2];
                double[] rightChannel = new double[sampleCount / 2];

                for (int i = 0; i < sampleCount / 2; i++)
                {
                    leftChannel[i] = samples[2 * i];
                    rightChannel[i] = samples[2 * i + 1];
                }
                Console.WriteLine("Channels separated successfully.");
                return (leftChannel, rightChannel, sampleRate);
            }
        }

        public static List<double> ComputeTimeDelays(double[] leftChannel, double[] rightChannel, int bufferSize, int fs, int fmin, int fmax)
        {
            var timeDelays = new ConcurrentBag<double>();

            int numBuffers = leftChannel.Length / bufferSize;

            // Create slices instead of using Skip and Take
            double[] leftBuffer = new double[bufferSize];
            double[] rightBuffer = new double[bufferSize];

            for (int i = 0; i < numBuffers; i++)
            {
                try
                {
                    int startIndex = i * bufferSize;

                    Array.Copy(leftChannel, startIndex, leftBuffer, 0, bufferSize);
                    Array.Copy(rightChannel, startIndex, rightBuffer, 0, bufferSize);

                    if (leftBuffer.Length == 0 || rightBuffer.Length == 0)
                    {
                        Console.WriteLine($"Skipping empty buffer at index {startIndex}");
                        continue;
                    }

                    double timeDelay_ms = GccPhatCore.GCCPHAT(leftBuffer, rightBuffer, fs, 1, fmin, fmax);
                    //if (gccPhat == null || axeMs == null)
                    //{
                    //    Console.WriteLine("GCCPHAT returned null results.");
                    //    continue;
                    //}

                    //int maxIndex = Array.IndexOf(gccPhat, gccPhat.Max());
                    //if (maxIndex < 0 || maxIndex >= axeMs.Length)
                    //{
                    //    Console.WriteLine("Invalid maxIndex detected.");
                    //    continue;
                    //}

                    //double timeDelay = axeMs[maxIndex];
                    timeDelays.Add(timeDelay_ms);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in processing buffer loop: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            };

            // Convert ConcurrentBag to List if needed
            List<double> timeDelaysList = timeDelays.ToList();

            Console.WriteLine("Time delays computed successfully.");
            return timeDelaysList;
        }
    }
}
