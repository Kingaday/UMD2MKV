namespace UMD2MKV.VGMToolbox
{
    public class Mpeg2Stream : Mpegstream
    {
        private const string defaultAudioExtension = ".m2a";
        private const string defaultVideoExtension = ".m2v";

        protected Mpeg2Stream(string path) :
            base(path)
        {
            FileExtensionAudio = defaultAudioExtension;
            FileExtensionVideo = defaultVideoExtension;

            BlockIdDictionary[BitConverter.ToUInt32(PacketStartBytes, 0)] = new BlockSizeStruct(PacketSizeType.Static, 0xE); // Pack Header
        }
        private static int GetStandardPesHeaderSize(Stream readStream, long currentOffset)
        {
            var od = new OffsetDescription(offsetByteOrder: Constants.bigEndianByteOrder, offsetSize: "1", offsetValue: "8");

            var checkBytes = (byte)ParseFile.GetVaryingByteValueAtRelativeOffset(readStream, od, currentOffset);

            return checkBytes + 3;
        }
        protected override int GetAudioPacketHeaderSize(Stream readStream, long currentOffset)
        {
            var packetSize = GetStandardPesHeaderSize(readStream, currentOffset);
            return packetSize;
        }
        protected override int GetVideoPacketHeaderSize(Stream readStream, long currentOffset)=>GetStandardPesHeaderSize(readStream, currentOffset);
        protected override string GetAudioFileExtension(Stream readStream, long currentOffset)=>defaultAudioExtension;
    }
}