using System.Runtime.InteropServices;
using Xabe.FFmpeg;

namespace UMD2MKV.FFmpeg;

public static class Ffmpeg
{
    public static async Task<bool> ConvertOma(List<string?>? inputFiles, string outputDirectory,bool lossy, CancellationToken cancellationToken,IProgress<int>? progress = null)
    {
#if WINDOWS
           Xabe.FFmpeg.FFmpeg.SetExecutablesPath(GetFfmpegPath());
#endif
        if (inputFiles == null || inputFiles.Count == 0) return false;
        foreach (var inputFile in inputFiles)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
            if (lossy)
            {
                var outputFile = Path.Combine(outputDirectory, $"{fileNameWithoutExtension}.aac");
                var conversion = Xabe.FFmpeg.FFmpeg.Conversions.New().AddParameter($"-i \"{inputFile}\" -c:a aac \"{outputFile}\"");
                conversion.OnProgress += (_, args) => { progress?.Report(args.Percent); };
                try
                {
                    await conversion.Start(cancellationToken);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                var outputFile = Path.Combine(outputDirectory, $"{fileNameWithoutExtension}.flac");
                var conversion = Xabe.FFmpeg.FFmpeg.Conversions.New().AddParameter($"-i \"{inputFile}\" -c:a flac \"{outputFile}\"");
                conversion.OnProgress += (_, args) => { progress?.Report(args.Percent); };
                try
                {
                    await conversion.Start(cancellationToken);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            
        }
        //cleanup oma files 
        foreach (var file in inputFiles)
        {
            try
            {
                if (file != null) File.Delete(file);
            }
            catch (Exception)
            {
                return false;
            }
        }
        return true;
    }
    public static async Task<bool> MergeMpsWithFlacAsync(string? videoPath, List<string?>? audioPaths, string outputPath, bool segment, TimeSpan startTime, TimeSpan endTime,IProgress<int>? progress = null )
    {
#if WINDOWS
           Xabe.FFmpeg.FFmpeg.SetExecutablesPath(GetFfmpegPath());
#endif
        if (string.IsNullOrWhiteSpace(videoPath) || audioPaths == null || audioPaths.Count == 0)
            return false;
        if (!File.Exists(videoPath) || audioPaths.Any(ap => !File.Exists(ap)))
            return false;
        var outputFilePath = Path.Combine(outputPath, "movie.mkv");
        var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(videoPath);
        var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
        var conversion = Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddStream(videoStream?.SetCodec(VideoCodec.copy))
            .SetOutput(outputFilePath)
            .SetOutputFormat(Format.matroska);
        var ordered = audioPaths.Order(); //make sure audio streams are in same order as on psp
        foreach (var audioPath in ordered)
        {
            var audioInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(audioPath);
            var audioStream = audioInfo.AudioStreams.FirstOrDefault();
            if (audioStream != null)
                conversion.AddStream(audioStream.SetCodec(AudioCodec.copy));
        }
        conversion.OnProgress += (_, args) =>
        {
            progress?.Report(args.Percent);
        };
        if (segment)
            conversion.SetSeek(startTime).SetOutputTime(endTime);
        try
        {
            await conversion.Start();
        }
        catch (Exception)
        {
            return false;
        }
        //cleanup files 
        audioPaths.Add(videoPath);
        //audioPaths.AddRange(FileUtils.FileUtils.GetFilesWithExtension(outputPath, "*.subs")!);
        foreach (var file in audioPaths)
        {
            try
            {
                if (file != null)
                    File.Delete(file);
            }
            catch (Exception)
            {
                return false;
            }
        }
        return true;
    }
    
    
    private static string GetFfmpegPath()
    {
        var baseDir = Directory.GetCurrentDirectory();
#if WINDOWS
          return Path.Combine(baseDir, "FFmpeg", "win-x64");
#elif MACCATALYST
        var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "macos-arm64" : "macos-x64";
        arch = Path.Combine(baseDir, "FFmpeg", arch);
        return arch;
#else
        throw new PlatformNotSupportedException("FFmpeg bundling is only set up for Windows & Mac Catalyst.");
#endif
    }
}