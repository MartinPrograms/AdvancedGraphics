using Silk.NET.Core;

namespace VulkanAbstraction.Common;

public class VaVulkanVersion
{
    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Patch { get; private set; }
    public static VaVulkanVersion Default = new VaVulkanVersion(1, 0, 0);

    public VaVulkanVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }
    
    public VaVulkanVersion(uint version)
    {
        Major = (int)(version >> 22);
        Minor = (int)((version >> 12) & 0x3ff);
        Patch = (int)(version & 0xfff);
    }
    
    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
    
    // Override the cast to uint
    public static implicit operator uint(VaVulkanVersion version)
    {
        return (uint)((version.Major << 22) | (version.Minor << 12) | version.Patch);
    }
}