namespace UMD2MKV.VGMToolbox
{
    public class OffsetDescription(string offsetValue, string offsetSize, string offsetByteOrder)
    {
        public string OffsetValue { get; } = offsetValue;
        public string OffsetSize { get; } = offsetSize;
        public string OffsetByteOrder { get; } = offsetByteOrder;
    }
}