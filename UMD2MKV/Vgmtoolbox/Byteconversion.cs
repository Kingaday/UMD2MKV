namespace UMD2MKV.VGMToolbox;
public static class Byteconversion
{
    public static long GetLongValueFromString(string value)
    {
        long ret;
        var isNegative = false;
        string parseValue;

        if (value.StartsWith('-'))
        {
            parseValue = value[1..];
            isNegative = true;
        }
        else
            parseValue = value;

        if (parseValue.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
        {
            parseValue = parseValue[2..];
            ret = long.Parse(parseValue, System.Globalization.NumberStyles.HexNumber, null);
        }
        else
            ret = long.Parse(parseValue, System.Globalization.NumberStyles.Integer, null);

        if (isNegative)
            ret *= -1;

        return ret;
    }
        
    public static UInt32 GetUInt32BigEndian(byte[] value)
    {
        var workingArray = new byte[value.Length];
        Array.Copy(value, 0, workingArray, 0, value.Length); 
            
        if (BitConverter.IsLittleEndian)
            Array.Reverse(workingArray);
            
        return BitConverter.ToUInt32(workingArray, 0);
    }
}