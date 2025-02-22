namespace UMD2MKV.VGMToolbox
{
    public class SonyPspMpsStream : Sonypmfstream
    {
        public SonyPspMpsStream(string path) : base(path) 
        {
            SubTitleExtractionSupported = true;
        }

        protected override long GetStartOffset(Stream readStream, long currentOffset) => 0;

        protected override bool IsThisASubPictureBlock(byte[] blockToCheck) => base.IsThisAnAudioBlock(blockToCheck); // uses same stream as Audio
    }
}