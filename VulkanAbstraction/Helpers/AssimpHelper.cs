using System.Numerics;
using Assimp;
using VulkanAbstraction.Common;

namespace VulkanAbstraction.Helpers;

public class AssimpHelper
{
    public static (Vertex[], uint[]) LoadModel(string path)
    {
        var importer = new AssimpContext();
        var scene = importer.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.CalculateTangentSpace);
        
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        
        foreach (var mesh in scene.Meshes)
        {
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                var vertex = new Vertex
                {
                    Position = new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z),
                    TexCoord = new Vector2(mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y),
                    Normal = new Vector3(mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z),
                    Tangent = new Vector3(mesh.Tangents[i].X, mesh.Tangents[i].Y, mesh.Tangents[i].Z),
                    Bitangent = new Vector3(mesh.BiTangents[i].X, mesh.BiTangents[i].Y, mesh.BiTangents[i].Z)
                };
                vertices.Add(vertex);
            }
            
            foreach (var face in mesh.Faces)
            {
                indices.AddRange(face.Indices.Select(x => (uint)x));
            }
        }
        
        return (vertices.ToArray(), indices.ToArray());
    }
}