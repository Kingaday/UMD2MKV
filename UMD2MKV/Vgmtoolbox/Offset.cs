namespace VGMToolbox.util
{
    public class OffsetDescription(string offsetValue, string offsetSize, string offsetByteOrder)
    {
        public string OffsetValue { set; get; } = offsetValue;
        public string OffsetSize { set; get; } = offsetSize;
        public string OffsetByteOrder { set; get; } = offsetByteOrder;
    }
}