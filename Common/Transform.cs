using System.Numerics;

namespace Common;

public class Transform
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    
    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }
    
    public Matrix4x4 GetMatrix()
    {
        return Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation * Quaternion.CreateFromYawPitchRoll(0, 0, 0)) * Matrix4x4.CreateTranslation(Position);
    }
}