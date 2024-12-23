namespace Common;

public class Time
{
    public static double DeltaTime { get; private set; }
    public static double TotalTime { get; private set; }
    
    public static void Update(double deltaTime)
    {
        DeltaTime = deltaTime;
        TotalTime += deltaTime;
    }
}