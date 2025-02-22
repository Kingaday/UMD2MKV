namespace UMD2MKV.VGMToolbox
{
    public class Sonypmfstream : Mpeg2Stream
    {
        private const string defaultAudioExtension = ".at3";
        private const string atrac3AudioExtension = ".at3";
        private const string lpcmAudioExtension = ".lpcm";
        private const string subTitleExtension = ".subs";
        private const string avcVideoExtension = ".264";
        
        private static readonly byte[] AvcBytes = [0x00, 0x00, 0x00, 0x01];

        protected Sonypmfstream(string path) : base(path)
        {
            UsesSameIdForMultipleAudioTracks = true;
            FileExtensionAudio = defaultAudioExtension;
            FileExtensionVideo = avcVideoExtension;

            BlockIdDictionary[BitConverter.ToUInt32(PacketStartBytes, 0)] = new BlockSizeStruct(PacketSizeType.Static, 0xE); // Pack Header
            BlockIdDictionary[BitConverter.ToUInt32([0x00, 0x00, 0x01, 0xBD], 0)] = new BlockSizeStruct(PacketSizeType.SizeBytes, 2); // Audio Stream, two bytes following equal length (Big Endian)
        }

        protected override long GetStartOffset(Stream readStream, long currentOffset)
        {
            long startOffset = 0;

            var seekOffsets = Byteconversion.GetUInt32BigEndian(ParseFile.ParseSimpleOffset(readStream, 0x86, 4));
            var seekCount = Byteconversion.GetUInt32BigEndian(ParseFile.ParseSimpleOffset(readStream, 0x8A, 4));

            if (seekOffsets > 0)
                startOffset = seekOffsets + (seekCount * 0x0A);

            return startOffset;
        }

        protected override int GetAudioPacketHeaderSize(Stream readStream, long currentOffset)
        {            
            var od = new OffsetDescription(offsetByteOrder: Constants.bigEndianByteOrder, offsetSize: "1", offsetValue: "8");
            var checkBytes = (byte)ParseFile.GetVaryingByteValueAtRelativeOffset(readStream, od, currentOffset);
            return checkBytes + 7;           
        }
        protected override int GetAudioPacketSubHeaderSize(Stream readStream, long currentOffset, byte streamId)
        {
            var streamFileExtension = StreamIdFileType[streamId];

            var subHeaderSize = streamFileExtension switch
            {
                subTitleExtension => // leave timing data attached for post-processing
                    -0xC,
                _ => 0
            };
            return subHeaderSize;
        }
        protected override bool IsThisAnAudioBlock(byte[] blockToCheck)=> (blockToCheck[3] == 0xBD);
        protected override bool IsThisAVideoBlock(byte[] blockToCheck)=>((blockToCheck[3] >= 0xE0) && (blockToCheck[3] <= 0xEF));
        protected override string GetAudioFileExtension(Stream readStream, long currentOffset)
        {
            var streamId = GetStreamId(readStream, currentOffset);

            var fileExtension = streamId switch
            {
                < 0x20 => atrac3AudioExtension,
                >= 0x40 and < 0x50 => lpcmAudioExtension,
                >= 0x80 and < 0x9F => subTitleExtension,
                _ => ".bin"
            };
            return fileExtension;
        }
        protected override string GetVideoFileExtension(Stream readStream, long currentOffset)
        {
            var videoHeaderSize = GetVideoPacketHeaderSize(readStream, currentOffset);
            var checkBytes = ParseFile.ParseSimpleOffset(readStream, (currentOffset + videoHeaderSize + 6), 4);
            var fileExtension = ParseFile.CompareSegment(checkBytes, 0, AvcBytes) ? avcVideoExtension : ".bin";
            return fileExtension;
        }

        protected override byte GetStreamId(Stream readStream, long currentOffset) 
        {
            var sizeValue = ParseFile.ParseSimpleOffset(readStream, currentOffset + 8, 1)[0];
            var offsetToCheck = sizeValue + 6 + 7 - 4;
            var streamId = ParseFile.ParseSimpleOffset(readStream, currentOffset + offsetToCheck, 1)[0];

            return streamId;
        }
        protected override void DoFinalTasks(Stream sourceFileStream, Dictionary<uint, FileStream> outputFiles, bool addHeader,IProgress<int>? progress = null)
        {
            var totalFiles = outputFiles.Count; // Total number of files to process
            var processedFiles = 0; // Track the number of files processed
            progress?.Report(1);
            foreach (var streamId in outputFiles.Keys)
            {
                if (IsThisAnAudioBlock(BitConverter.GetBytes(streamId)) && outputFiles[streamId].Name.EndsWith(atrac3AudioExtension))
                {
                    var headerBytes = ParseFile.ParseSimpleOffset(outputFiles[streamId], 0, 0x8);

                    // remove all header chunks
                    var cleanedFile = Fileutil.RemoveAllChunksFromFile(outputFiles[streamId], headerBytes);

                    // close stream and rename file
                    var sourceFile = outputFiles[streamId].Name;

                    outputFiles[streamId].Close();
                    outputFiles[streamId].Dispose();

                    File.Delete(sourceFile);
                    File.Move(cleanedFile, sourceFile);

                    // add header
                    if (addHeader)
                    {
                        Array.Reverse(headerBytes);
                        var headerBlock = BitConverter.ToUInt32(headerBytes, 4);
                        var headeredFile = Path.ChangeExtension(sourceFile, Atrac3Plus.fileExtensionPsp);
                        var aa3HeaderBytes = Atrac3Plus.GetAa3Header(headerBlock);
                        Fileutil.AddHeaderToFile(aa3HeaderBytes, sourceFile, headeredFile);
                        File.Delete(sourceFile);
                    }
                }
                // Update progress after processing each file
                processedFiles++;
                if (progress == null) continue;
                var percentComplete = (int)((double)processedFiles / totalFiles * 100);
                progress.Report(percentComplete);
            }
        }
    }
}