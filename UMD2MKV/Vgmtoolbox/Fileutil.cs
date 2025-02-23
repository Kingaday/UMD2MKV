namespace UMD2MKV.VGMToolbox;
public static class Fileutil
{
    public static string RemoveAllChunksFromFile(FileStream fs, byte[] chunkToRemove)
    {
        int bytesRead;
        long currentReadOffset = 0;
        long totalBytesRead = 0;
        int maxReadSize;
        long currentChunkSize;

        var bytes = new byte[Constants.fileReadChunkSize];
        var offsets = ParseFile.GetAllOffsets(fs, 0, chunkToRemove, false, -1, -1, true);

        var destinationPath = Path.ChangeExtension(fs.Name, ".cut");

        using var destinationFs = File.OpenWrite(destinationPath);
        foreach (var t in offsets)
        {
            // move position
            fs.Position = currentReadOffset;

            // get length of current size to write
            currentChunkSize = t - currentReadOffset;

            // calculate max cut size for this loop iteration
            maxReadSize = (currentChunkSize - totalBytesRead) > bytes.Length
                ? bytes.Length
                : (int)(currentChunkSize - totalBytesRead);

            while ((bytesRead = fs.Read(bytes, 0, maxReadSize)) > 0)
            {
                destinationFs.Write(bytes, 0, bytesRead);
                totalBytesRead += bytesRead;

                maxReadSize = (currentChunkSize - totalBytesRead) > bytes.Length
                    ? bytes.Length
                    : (int)(currentChunkSize - totalBytesRead);
            }

            totalBytesRead = 0;
            currentReadOffset = t + chunkToRemove.Length;
        }

        ////////////////////////////
        // write remainder of file
        ////////////////////////////
        // move position
        fs.Position = currentReadOffset;

        // get length of current size to write
        currentChunkSize = fs.Length - currentReadOffset;

        // calculate max cut size
        maxReadSize = (currentChunkSize) > bytes.Length
            ? bytes.Length
            : (int)(currentChunkSize);

        while ((bytesRead = fs.Read(bytes, 0, maxReadSize)) > 0)
        {
            destinationFs.Write(bytes, 0, bytesRead);
            totalBytesRead += bytesRead;

            maxReadSize = (currentChunkSize - totalBytesRead) > bytes.Length
                ? bytes.Length
                : (int)(currentChunkSize - totalBytesRead);
        }

        return destinationPath;
    }

    public static void AddHeaderToFile(byte[] headerBytes, string sourceFile, string destinationFile)
    {
        int bytesRead;
        var readBuffer = new byte[Constants.fileReadChunkSize];

        using var destinationStream = File.Open(destinationFile, FileMode.CreateNew, FileAccess.Write);
        // write header
        destinationStream.Write(headerBytes, 0, headerBytes.Length);

        // write the source file
        using var sourceStream = File.Open(sourceFile, FileMode.Open, FileAccess.Read);
        while ((bytesRead = sourceStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            destinationStream.Write(readBuffer, 0, bytesRead);
        }
    }
}