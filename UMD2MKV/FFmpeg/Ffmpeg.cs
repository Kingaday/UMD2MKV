using System.Runtime.InteropServices;
using Xabe.FFmpeg;

namespace UMD2MKV.FFmpeg;

public static class Ffmpeg
{
    public static async Task<bool> ConvertOma(List<string?>? inputFiles, string outputDirectory,bool lossy, CancellationToken cancellationToken,IProgress<int>? progress = null)
    {
#if WINDOWS //having issues on macOS to get Xabe to find the ffmpegpath ... for now just copying during build to the MonoBundle folder in the app package
           Xabe.FFmpeg.FFmpeg.SetExecutablesPath(GetFfmpegPath());
#endif
        if (inputFiles == null || inputFiles.Count == 0) return false;
        foreach (var inputFile in inputFiles)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
            var codec = lossy ? "aac" : "flac";
            var outputFile = Path.Combine(outputDirectory, $"{fileNameWithoutExtension}.{codec}");
    
            var conversion = Xabe.FFmpeg.FFmpeg.Conversions.New()
                .AddParameter($"-i \"{inputFile}\" -c:a {codec} \"{outputFile}\"");

            conversion.OnProgress += (_, args) => progress?.Report(args.Percent);
    
            try
            {
                await conversion.Start(cancellationToken);
            }
            catch (Exception)
            {
                return false;
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
    public static async Task<bool> MergeMpsWithEncodedAudioAsync(string? videoPath, List<string?>? audioPaths, string outputPath, bool segment, TimeSpan startTime, TimeSpan endTime,IProgress<int>? progress = null )
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
    public static async Task<bool> MuxSubtitlesAsync(string inputMkv, List<string?>? subtitleFiles, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(inputMkv) || subtitleFiles == null || subtitleFiles.Count == 0)
            throw new ArgumentException("Invalid input file or subtitle list.");

        var conversion = Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddParameter($"-i \"{inputMkv}\"");
        foreach (var srt in subtitleFiles)
            conversion.AddParameter($"-i \"{srt}\"");
        conversion.AddParameter("-map 0");  
        for (var i = 0; i < subtitleFiles.Count; i++)
            conversion.AddParameter($"-map {i + 1} -c:s:{i} srt");
        conversion.AddParameter("-c:v copy -c:a copy");
        conversion.SetOutput(Path.Combine(outputPath,"subbed_movie.mkv"));
            
        try
        {
            await conversion.Start();
        }
        catch (Exception)
        {
            return false;
        }

        //cleanup original mkv and srt files
        File.Delete(inputMkv);
        //leave srt files for now until timing extraction is resolved
        /*foreach (var file in subtitleFiles)
        {
            try
            {
                if (file != null) File.Delete(file);
            }
            catch (Exception)
            {
                return false;
            }
        }*/
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