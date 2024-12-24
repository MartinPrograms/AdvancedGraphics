namespace Common;

public enum MouseButton
{
    Left,
    Right,
    Middle
}

public static class MouseButtonConverter
{
    public static MouseButton FromSDL(byte mouseEventButton)
    {
        switch (mouseEventButton)
        {
            case 1:
                return MouseButton.Left;
            case 2:
                return MouseButton.Middle;
            case 3:
                return MouseButton.Right;
            default:
                return MouseButton.Left;
        }
    }
}