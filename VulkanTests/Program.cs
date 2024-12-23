using Common;
using Silk.NET.SDL;
using StupidSimpleLogger;
using VulkanAbstraction;
using VulkanAbstraction.Common;

Logger.Init();
var a = new VaWindow(800, 600, "Vulkan Window", new VaContext(), new VaVulkanVersion(1, 0, 0));

a.OnUpdate += () =>
{
    if (Input.IsKeyPressed(KeyCode.KF11))
    {
        if (a.IsFullscreen)
        {
            a.SetWindowed();
        }
        else
        {
            a.SetFullscreen();
        }
    }
};

a.Run();

Logger.DumpLogs();