using DiscUtils.Iso9660;

namespace UMD2MKV.FileUtils;

public static class FileUtils
{
    public static async Task<bool> CopyLargestMps(string isoPath, string outputPath, IProgress<int>? progress = null)
    {
        const string targetFolder = @"\UMD_VIDEO\STREAM";
        await using var isoStream = new FileStream(isoPath, FileMode.Open, FileAccess.Read);
        var cdReader = new CDReader(isoStream, true); 
        if (!cdReader.DirectoryExists(targetFolder))
        {
            Console.WriteLine("Stream folder not found in ISO.");
            return false;
        }
        string? largestFile = null;
        long largestSize = 0;
        foreach (var file in cdReader.GetFileSystemEntries(targetFolder))
        {
            if (!cdReader.FileExists(file)) continue; // Ensure it's a file, not a subdirectory
            var fileSize = cdReader.GetFileLength(file);
            if (fileSize <= largestSize) continue;
            largestSize = fileSize;
            largestFile = file;
        }
        if (largestFile == null)
            return false;

        await using Stream fileStream = cdReader.OpenFile(largestFile, FileMode.Open);
        await using var outputStream = new FileStream(outputPath + "/movie.mps", FileMode.Create, FileAccess.Write);
        var buffer = new byte[8192 * 1024];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await outputStream.WriteAsync(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;
            if (progress == null) continue;
            var percentage = (int)((totalBytesRead * 100) / largestSize);
            progress.Report(percentage);
        }
        return true;
    }
    public static List<string?> GetFilesWithExtension(string directoryPath, string extension)
    {
        if (Directory.Exists(directoryPath))
            return [..Directory.GetFiles(directoryPath, extension, SearchOption.TopDirectoryOnly)];
        return [];

    }
    public static bool DirectoryHasFiles(string directoryPath)=> Directory.Exists(directoryPath) && Directory.EnumerateFiles(directoryPath).Any();
    public static void DeleteFilesWithExtension(string directoryPath, string extension)
    {
        var files = GetFilesWithExtension(directoryPath, extension);
        foreach (var file in files)
        {
            try
            {
                if (file != null) File.Delete(file);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}