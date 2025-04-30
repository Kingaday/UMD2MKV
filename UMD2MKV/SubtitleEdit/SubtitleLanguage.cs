namespace UMD2MKV.SubtitleEdit;

public class SubtitleLanguage(string code, string localName, string nativeName)
{
    public string Code { get; } = code;
    private string LocalName { get; } = localName;
    public string NativeName { get; } = nativeName;

    public override string ToString()=>LocalName;
}