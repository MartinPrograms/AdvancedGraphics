namespace VulkanAbstraction.Globals;

public class MaterialManager
{
    public struct Material
    {
        public int AlbedoTexture;
    }

    class MaterialOffset
    {
        public int Offset;
        public int Size;
    }
    
    public static Dictionary<string, Material> Materials = new();
    private static Dictionary<string, MaterialOffset> MaterialOffsets = new();
    
    public static void AddMaterial(string name, int albedoTexture)
    {
        if (Materials.ContainsKey(name))
        {
            throw new Exception($"Material with name {name} already exists");
        }
        
        Materials.Add(name, new Material { AlbedoTexture = albedoTexture });
    }
    
    public static Material GetMaterial(string name)
    {
        if (!Materials.ContainsKey(name))
        {
            throw new Exception($"Material with name {name} does not exist");
        }
        
        return Materials[name];
    }
}