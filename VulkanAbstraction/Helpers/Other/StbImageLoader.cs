namespace VulkanAbstraction.Helpers.Other;

public class StbImageLoader
{
    public static byte[] Load(string texturesTestPng)
    {
        return File.ReadAllBytes(texturesTestPng);
    }
}