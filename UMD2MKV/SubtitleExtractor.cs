using UMD2MKV.VGMToolbox;

namespace UMD2MKV;

using System;
using System.IO;
using System.Collections.Generic;

public static class Subtitles
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] PngFooter = [0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];

    /// <summary>
    /// Extracts PNG images from all .subs files in the given directory.
    /// </summary>
    private static async Task<bool> ExtractPngFromSubtitles(string inputDirectory)
    {
        if (!Directory.Exists(inputDirectory))
        {
            Console.WriteLine("Error: Input directory does not exist.");
            return false;
        }

        foreach (var subFile in Directory.GetFiles(inputDirectory, "*.subs"))
        {
            var outputDirectory = Path.Combine(Path.GetDirectoryName(subFile)!, Path.GetFileNameWithoutExtension(subFile));
            Directory.CreateDirectory(outputDirectory);

            var dataStream = await File.ReadAllBytesAsync(subFile);
            var pngFiles = ExtractPngFiles(dataStream);

            for (var i = 0; i < pngFiles.Count; i++)
            {
                var outputPath = Path.Combine(outputDirectory, $"{i}.png");
                await File.WriteAllBytesAsync(outputPath, pngFiles[i]);
            }
            
        }
        return true;
    }
    /// <summary>
    /// Extracts PNG image data from a binary stream.
    /// </summary>
    private static List<byte[]> ExtractPngFiles(byte[] data)
    {
        var pngFiles = new List<byte[]>();
        var index = 0;

        while (index < data.Length)
        {
            var start = FindPattern(data, PngHeader, index);
            if (start == -1) break;

            var end = FindPattern(data, PngFooter, start);
            if (end == -1) break;

            end += PngFooter.Length;

            var pngData = new byte[end - start];
            Array.Copy(data, start, pngData, 0, pngData.Length);

            pngFiles.Add(pngData);
            index = end;
        }

        return pngFiles;
    }
    /// <summary>
    /// Finds a byte pattern within a byte array.
    /// </summary>
    private static int FindPattern(byte[] data, byte[] pattern, int start)
    {
        for (var i = start; i <= data.Length - pattern.Length; i++)
        {
            var match = !pattern.Where((t, j) => data[i + j] != t).Any();
            if (match) return i;
        }
        return -1;
    }
    public static async Task<bool> ConvertAndMuxSubtitles(string outputPath)
    {
            var success = await ExtractPngFromSubtitles(outputPath);
            await ExtractTimeStampsFromSubtitles(outputPath);
            // OCR png files and replace path to image to text in srt files ... tessarect/LLM/...
            await FFmpeg.Ffmpeg.MuxSubtitlesAsync(Path.Combine(outputPath, "movie.mkv"),FileUtils.FileUtils.GetFilesWithExtension(outputPath,"*.srt",SearchOption.AllDirectories),outputPath);
        return success;
    }
    private static async Task ExtractTimeStampsFromSubtitles(string outputPath)
    {
        var subtitleFiles = FileUtils.FileUtils.GetFilesWithExtension(outputPath, "*.subs");
        foreach (var sub in subtitleFiles)
        {
            await using var subtitleStream = new FileStream(sub!, FileMode.Open, FileAccess.Read);
            
            // adjusted vgmtoolbox code for extracting timestamps .... not working 
            // disabled png writing as this is done elsewhere more successfully already
            var subsLength = subtitleStream.Length;
            long currentOffset = 0;

            var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var pngEnd = "IEND"u8.ToArray();

            uint pngCount = 0;

            var baseDirectory = Path.GetDirectoryName(subtitleStream.Name);
            baseDirectory = Path.Combine(baseDirectory!, $"{Path.GetFileNameWithoutExtension(subtitleStream.Name)}");
            var timestamplist = new List<KeyValuePair<ulong, string>>();
            while (currentOffset < subsLength)
            {
                // decode time stamp
                var encodedPresentationTimeStamp = ParseFile.ParseSimpleOffset(subtitleStream, currentOffset, 5);
                var decodedTimeStamp = DecodePresentationTimeStamp(encodedPresentationTimeStamp);

                // get subtitle packet size
                var subtitlePacketSize = ParseFile.ReadUshortBe(subtitleStream, currentOffset + 0xC);

                // extract PNG
                var pngStartOffset = ParseFile.GetNextOffset(subtitleStream, currentOffset + 0x1E, pngHeader);
                var pngEndOffset = ParseFile.GetNextOffset(subtitleStream, pngStartOffset, pngEnd) + 4;
                var pngSize = pngEndOffset - pngStartOffset;
                if (pngSize > (subtitlePacketSize - 0x14))
                {
                    Console.WriteLine($"Warning: PNG size ({pngSize}) exceeds packet size ({subtitlePacketSize - 0x14}). Skipping...");
                    //currentOffset += 0xE + subtitlePacketSize;
                    break;
                    // something going wrong ... need more debug time or someone smarter than me :) 
                    // for now only timings until sub extraction goes wrong...
                    // issue 1 => pngsize is at certain moment to large skewing further read out of file, if we ignore the png's seem to still be picked up correctly but the timestamp readouts go wrong
                    // issue 2 => we can extract the timestamp of the start to show but how long to show??? is this info also present in the binary file?
                }

                var destinationFile = Path.Combine(baseDirectory, $"{pngCount:D8}.png");
                // storing png files replaced with other code based on 
                //ParseFile.ExtractChunkToFile(subtitleStream, pngStartOffset, pngSize, destinationFile);
                pngCount++;

                timestamplist.Add(new KeyValuePair<ulong, string>(decodedTimeStamp, destinationFile));
                // move to next block
                currentOffset += 0xE + subtitlePacketSize;
            }

            //write timestamps to .srt file
            GenerateSrtFile(timestamplist,
                Path.Combine(baseDirectory, $"{Path.GetFileNameWithoutExtension(subtitleStream.Name)}.srt"));
        }
    }

        private static void GenerateSrtFile(List<KeyValuePair<ulong, string>> timestampDictionary, string srtFilePath, int defaultDurationMs = 3000)
        {
            using var writer = new StreamWriter(srtFilePath);
            for (var i = 0; i < timestampDictionary.Count; i++)
            {
                var startTimeMs = timestampDictionary[i].Key/90; //90hz format???
                var endTimeMs = startTimeMs + (ulong)defaultDurationMs;  // currently default duration as no idea how to retrieve end time from subs file...

                var pngFile = timestampDictionary[i].Value;

                writer.WriteLine($"{i + 1}");
                writer.WriteLine($"{FormatTime(startTimeMs)} --> {FormatTime(endTimeMs)}");
                writer.WriteLine($"{pngFile}\n");
            }
        }
        private static string FormatTime(ulong milliseconds)
        {
            var time = TimeSpan.FromMilliseconds((long)milliseconds);
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
        }
        private static ulong DecodePresentationTimeStamp(byte[] encodedTimeStampBytes)
        {
            ulong decodedTimeStamp = 0;

            if (encodedTimeStampBytes.Length != 5)
                throw new FormatException("Encoded time stamp must be 5 bytes.");

            // convert to ulong from bytes
            ulong encodedTimeStamp = encodedTimeStampBytes[0];
            encodedTimeStamp &= 0x0F;
            encodedTimeStamp <<= 32;
            encodedTimeStamp += (ulong)(encodedTimeStampBytes[1] << 24);
            encodedTimeStamp += (ulong)(encodedTimeStampBytes[2] << 16);
            encodedTimeStamp += (ulong)(encodedTimeStampBytes[3] << 8);                        
            encodedTimeStamp += encodedTimeStampBytes[4];
            
            decodedTimeStamp |= (encodedTimeStamp >> 3) & (0x0007ul << 30); // top 3 bits, shifted left by 3, other bits zeroed out
            decodedTimeStamp |= (encodedTimeStamp >> 2) & (0x7ffful << 15); // middle 15 bits
            decodedTimeStamp |= (encodedTimeStamp >> 1) & (0x7ffful << 0); // bottom 15 bits

            return decodedTimeStamp;
        }
    
}