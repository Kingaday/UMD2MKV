namespace UMD2MKV.SubtitleEdit;

public static class Subtitles
{
    public static async Task<bool> ConvertAndMuxSubtitles(string outputPath, OutputSubtitleType outputSubtitleType, IProgress<int>? progress = null)
    {
        var workingDirectories = new List<string>();
        foreach (var subFile in Directory.GetFiles(outputPath, "*.subs"))
        {
            var writedirectory = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(subFile));
            workingDirectories.Add(writedirectory);
            Directory.CreateDirectory(writedirectory);
            var psSub = new PspSubtitle(writedirectory, Path.GetFileNameWithoutExtension(subFile));
            await psSub.Process(subFile,outputSubtitleType);
        }

        var extension = outputSubtitleType switch
        {
            OutputSubtitleType.Srt => "*.srt",
            OutputSubtitleType.VobSub => "*.sub",
            _ => throw new ArgumentOutOfRangeException(nameof(outputSubtitleType), outputSubtitleType, null)
        };
        await FFmpeg.Ffmpeg.MuxSubtitlesAsync(Path.Combine(outputPath, "movie.mkv"),
            FileUtils.FileUtils.GetFilesWithExtension(outputPath, extension, SearchOption.AllDirectories), outputPath);
        //cleanup
        /*FileUtils.FileUtils.DeleteFilesWithExtension(outputPath, "*.subs");
        foreach (var dir in workingDirectories)
            FileUtils.FileUtils.DeleteDirectoryWithContent(dir);*/
        return true;
    }
}