namespace ZLimits;

public class LimitGroupInfo
{
    public string Handler { get; set; } = string.Empty;
    public string Punisher { get; set; } = string.Empty;
    public string Name { get; set; } = "unnamed";
    public short Max { get; set; }
    public string[] Items { get; set; } = [];
}