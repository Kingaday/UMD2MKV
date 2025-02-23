namespace UMD2MKV.VGMToolbox;
public abstract class Mpegstream
{
    protected static readonly byte[] PacketStartBytes = [0x00, 0x00, 0x01, 0xBA];
    private static readonly byte[] PacketEndBytes = [0x00, 0x00, 0x01, 0xB9];
    protected Mpegstream(string path)
    {
        FilePath = path;            
        UsesSameIdForMultipleAudioTracks = false;
        BlockSizeIsLittleEndian = false;
        FileExtensionAudio = string.Empty;
        FileExtensionVideo = string.Empty;
        //********************
        // Add Slice Packets 
        //********************
        var blockSize = new BlockSizeStruct(PacketSizeType.Static, 0xE);

        for (byte i = 0; i <= 0xAF; i++)
        {
            byte[] sliceBytes = [0x00, 0x00, 0x01, i];
            var sliceBytesValue = BitConverter.ToUInt32(sliceBytes, 0);
            BlockIdDictionary.Add(sliceBytesValue, blockSize);
        }
    }
    protected enum PacketSizeType
    {
        Static,
        SizeBytes,
        Eof
    }
    protected struct BlockSizeStruct(PacketSizeType sizeTypeValue, int sizeValue)
    {
        public readonly PacketSizeType SizeType = sizeTypeValue;
        public readonly int Size = sizeValue;
    }
    public struct DemuxOptionsStruct
    {            
        public bool ExtractVideo { init; get; }
        public bool ExtractAudio { init; get; }
        public bool AddHeader { init; get; }
    }
    #region Dictionary Initialization
    protected readonly Dictionary<uint, BlockSizeStruct> BlockIdDictionary =
        new()
        {                                                
            //********************
            // System Packets
            //********************
            {BitConverter.ToUInt32(Mpegstream.PacketEndBytes, 0), new BlockSizeStruct(PacketSizeType.Eof, -1)},   // Program End
            {BitConverter.ToUInt32(Mpegstream.PacketStartBytes, 0), new BlockSizeStruct(PacketSizeType.Static, 0xE)}, // Pack Header
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xBB], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // System Header, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xBD], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Private Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xBE], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Padding Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xBF], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Private Stream, two bytes following equal length (Big Endian)
            //****************************
            // Audio Streams
            //****************************
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC0], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC1], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC2], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC3], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC4], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC5], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC6], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC7], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC8], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xC9], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xCA], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xCB], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xCC], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xCD], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xCE], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xCF], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD0], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD1], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD2], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD3], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD4], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD5], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD6], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD7], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD8], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xD9], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xDA], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xDB], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xDC], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xDD], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xDE], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xDF], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Audio Stream, two bytes following equal length (Big Endian)
            //****************************
            // Video Streams
            //****************************
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE0], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE1], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE2], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE3], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE4], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE5], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE6], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE7], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE8], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xE9], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xEA], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xEB], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xEC], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xED], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xEE], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
            {BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xEF], 0), new BlockSizeStruct(PacketSizeType.SizeBytes, 2)}, // Video Stream, two bytes following equal length (Big Endian)
        };
    #endregion
    private string FilePath { get; }
    protected string FileExtensionAudio { get; init; }
    protected string FileExtensionVideo { get; set; }
    protected readonly Dictionary<byte, string> StreamIdFileType = new();
    protected bool UsesSameIdForMultipleAudioTracks { init; get; } // for PMF/PAM/DVD, who use 000001BD for all audio tracks
    private bool BlockSizeIsLittleEndian { get; }
    private static byte[] GetPacketStartBytes() { return PacketStartBytes; }
    protected abstract int GetAudioPacketHeaderSize(Stream readStream, long currentOffset);
    protected virtual int GetAudioPacketSubHeaderSize(Stream readStream, long currentOffset, byte streamId) { return 0; }
    protected abstract int GetVideoPacketHeaderSize(Stream readStream, long currentOffset);
    protected virtual int GetAudioPacketFooterSize(Stream readStream, long currentOffset) { return 0; }
    protected virtual int GetVideoPacketFooterSize(Stream readStream, long currentOffset) { return 0; }
    protected virtual bool IsThisAnAudioBlock(byte[] blockToCheck)
    {
        return ((blockToCheck[3] >= 0xC0) && 
                (blockToCheck[3] <= 0xDF));
    }
    protected virtual bool IsThisAVideoBlock(byte[] blockToCheck) => ((blockToCheck[3] >= 0xE0) && (blockToCheck[3] <= 0xEF));
    protected virtual string GetAudioFileExtension(Stream readStream, long currentOffset)=>FileExtensionAudio;
    protected virtual string GetVideoFileExtension(Stream readStream, long currentOffset)=>FileExtensionVideo;
    protected virtual byte GetStreamId(Stream readStream, long currentOffset) => 0;
    protected virtual long GetStartOffset(Stream readStream, long currentOffset) =>0;
    protected virtual void DoFinalTasks(Stream sourceFileStream, Dictionary<uint, FileStream> outputFiles, bool addHeader,IProgress<int>? progress = null) { }
    public async Task<bool> DemultiplexStreams(DemuxOptionsStruct demuxOptions, string outputDirectory, IProgress<int>? progress = null)
    {
        await using var fs = File.OpenRead(FilePath);
        var fileSize = fs.Length;
        long currentOffset = 0;

        var eofFlagFound = false;

        var streamOutputWriters = new Dictionary<uint, FileStream>();

        // look for first packet
        currentOffset = GetStartOffset(fs, currentOffset);
        currentOffset = ParseFile.GetNextOffset(fs, currentOffset, GetPacketStartBytes());

        if (currentOffset != -1)
        {                    
            while (currentOffset < fileSize)
            {
                try
                {
                    // get the current block
                    var currentBlockId = ParseFile.ParseSimpleOffset(fs, currentOffset, 4);

                    // get value to use as key to hash table
                    var currentBlockIdVal = BitConverter.ToUInt32(currentBlockId, 0);

                    if (BlockIdDictionary.TryGetValue(currentBlockIdVal, out var blockStruct))
                    {
                        // get info about this block type

                        switch (blockStruct.SizeType)
                        {
                            /////////////////////
                            // Static Block Size
                            /////////////////////
                            case PacketSizeType.Static:
                                currentOffset += blockStruct.Size; // skip this block
                                break;
                            //////////////////
                            // End of Stream
                            //////////////////
                            case PacketSizeType.Eof: 
                                eofFlagFound = true; // set EOF block found so we can exit the loop
                                break;
                            //////////////////////
                            // Varying Block Size
                            //////////////////////
                            case PacketSizeType.SizeBytes:
                                // Get the block size
                                var blockSizeArray = ParseFile.ParseSimpleOffset(fs, currentOffset + currentBlockId.Length, blockStruct.Size);
                                if (!BlockSizeIsLittleEndian)
                                    Array.Reverse(blockSizeArray);
                                var blockSize = blockStruct.Size switch
                                {
                                    4 => BitConverter.ToUInt32(blockSizeArray, 0),
                                    2 => BitConverter.ToUInt16(blockSizeArray, 0),
                                    1 => blockSizeArray[0],
                                    _ => throw new ArgumentOutOfRangeException($"Unhandled size block size.{Environment.NewLine}")
                                };
                                // if block type is audio or video, extract it
                                var isAudioBlock = IsThisAnAudioBlock(currentBlockId);

                                if ((demuxOptions.ExtractAudio && isAudioBlock) ||
                                    (demuxOptions.ExtractVideo && IsThisAVideoBlock(currentBlockId)))
                                {
                                    // reset stream id
                                    byte streamId = 0;          // for types that have multiple streams in the same block ID
                                    // if audio block, get the stream number from the queue                                                                                
                                    uint currentStreamKey;  // hash key for each file
                                    if (isAudioBlock && UsesSameIdForMultipleAudioTracks)
                                    {
                                        streamId = GetStreamId(fs, currentOffset);
                                        currentStreamKey = (streamId | currentBlockIdVal);
                                    }
                                    else
                                        currentStreamKey = currentBlockIdVal;
                                    // check if we've already started parsing this stream
                                    if (!streamOutputWriters.ContainsKey(currentStreamKey))
                                    {
                                        // convert block id to little endian for naming
                                        var currentBlockIdNaming = BitConverter.GetBytes(currentStreamKey);
                                        Array.Reverse(currentBlockIdNaming);
                                        // build output file name
                                        var lastSlashIndex = FilePath.LastIndexOf('\\');
                                        var lastPart = FilePath.Substring(lastSlashIndex + 1);
                                        var outputFileName = Path.GetFileNameWithoutExtension(lastPart);
                                        outputFileName = outputFileName + "_" + BitConverter.ToUInt32(currentBlockIdNaming, 0).ToString("X8");
                                        // add proper extension
                                        if (IsThisAnAudioBlock(currentBlockId))
                                        {
                                            var audioFileExtension = GetAudioFileExtension(fs, currentOffset);
                                            outputFileName += audioFileExtension;
                                            StreamIdFileType.TryAdd(streamId, audioFileExtension);
                                        }
                                        else
                                        { 
                                            FileExtensionVideo = GetVideoFileExtension(fs, currentOffset);
                                            outputFileName += FileExtensionVideo;
                                        }
                                        // add output directory
                                        outputFileName = outputDirectory + "/" +outputFileName;
                                        // add an output stream for writing
                                        streamOutputWriters[currentStreamKey] = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite);
                                    }
                                    // write the block
                                    int cutSize;
                                    if (IsThisAnAudioBlock(currentBlockId))
                                    {
                                        // write audio
                                        var audioBlockSkipSize = GetAudioPacketHeaderSize(fs, currentOffset) + GetAudioPacketSubHeaderSize(fs, currentOffset, streamId);
                                        var audioBlockFooterSize = GetAudioPacketFooterSize(fs, currentOffset);
                                        cutSize = (int)(blockSize - audioBlockSkipSize - audioBlockFooterSize);
                                        if (cutSize > 0)
                                            streamOutputWriters[currentStreamKey].Write(ParseFile.ParseSimpleOffset(fs, currentOffset + currentBlockId.Length + blockSizeArray.Length + audioBlockSkipSize, (int)(blockSize - audioBlockSkipSize)), 0, cutSize);
                                    }                                           
                                    else
                                    {
                                        // write video
                                        var videoBlockSkipSize = GetVideoPacketHeaderSize(fs, currentOffset);
                                        var videoBlockFooterSize = GetVideoPacketFooterSize(fs, currentOffset);
                                        cutSize = (int)(blockSize - videoBlockSkipSize - videoBlockFooterSize);
                                        if (cutSize > 0) 
                                            streamOutputWriters[currentStreamKey].Write(ParseFile.ParseSimpleOffset(fs, currentOffset + currentBlockId.Length + blockSizeArray.Length + videoBlockSkipSize, (int)(blockSize - videoBlockSkipSize)), 0, cutSize);
                                    }
                                }
                                // move to next block
                                currentOffset += currentBlockId.Length + blockSizeArray.Length + blockSize;
                                // Report progress periodically
                                if (progress != null)
                                {
                                    var percentComplete = (int)((double)currentOffset / fileSize * 100);
                                    progress.Report(percentComplete);
                                }
                                break;
                        }
                    }
                    else // this is an unexpected block type
                    {
                        CloseAllWriters(streamOutputWriters);
                        Array.Reverse(currentBlockId);
                        throw new FormatException($"Block ID at 0x{currentOffset:X8} not found in table: 0x{BitConverter.ToUInt32(currentBlockId, 0):X8}");
                    }
                    // exit loop if EOF block found
                    if (eofFlagFound)
                        break;
                }
                catch (Exception ex)
                {
                    CloseAllWriters(streamOutputWriters);
                    throw new Exception($"Error parsing file at offset {currentOffset:X8}), '{ex.Message}'", ex); 
                }
            } // while (currentOffset < fileSize)
        }
        else
        {
            CloseAllWriters(streamOutputWriters);
            throw new FormatException($"Cannot find Pack Header for file: {Path.GetFileName(FilePath)}{Environment.NewLine}");
        }
        ///////////////////////////////////
        // Perform any final tasks needed
        ///////////////////////////////////
        DoFinalTasks(fs, streamOutputWriters, demuxOptions.AddHeader, progress);
        //////////////////////////
        // close all open writers
        //////////////////////////
        CloseAllWriters(streamOutputWriters);
        return true;
    }
    private static void CloseAllWriters(Dictionary<uint, FileStream> writers)
    {
        //////////////////////////
        // close all open writers
        //////////////////////////
        foreach (var b in writers.Keys.Where(b => writers[b].CanRead))
        {
            writers[b].Close();
            writers[b].Dispose();
        }
    }
}