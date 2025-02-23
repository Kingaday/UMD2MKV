namespace UMD2MKV.VGMToolbox;
public sealed class SonyPspMpsStream(string path) : Sonypmfstream(path)
{
    protected override long GetStartOffset(Stream readStream, long currentOffset) => 0;
}