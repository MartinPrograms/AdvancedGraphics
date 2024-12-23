using System.Numerics;
using StupidSimpleLogger;
using Silk.NET.SDL;

namespace Common;

public abstract class Window
{
    protected int _width;
    protected int _height;
    protected string _title;
    protected bool _isRunning;
    protected Sdl _sdl;
    protected WindowFlags _windowFlags;
    protected unsafe Silk.NET.SDL.Window* _window;
    
    public int Width => _width;
    public int Height => _height;
    public string Title => _title;
    public bool IsRunning => _isRunning;
    public unsafe Silk.NET.SDL.Window* WindowPtr => _window;
    public Vector2 Size => new Vector2(_width, _height);
    public Vector2 Center => new Vector2(_width / 2, _height / 2);
    public WindowFlags Flags => _windowFlags;

    public bool IsFullscreen = false;
    
    public double AspectRatio => (double)_width / _height;
    public double DeltaTime { get; protected set; }
    
    public Window(int width, int height, string title, WindowFlags windowFlags)
    {
        Logger.Info("Creating Window", $"Width: {width}, Height: {height}, Title: {title} Flags: {windowFlags}");
        
        _width = width;
        _height = height;
        _title = title;
        _sdl = Sdl.GetApi();
        _windowFlags = windowFlags;
        
        InitWindow();
    }

    private unsafe void InitWindow() 
    {
        _sdl.Init(Sdl.InitVideo);
        _window = _sdl.CreateWindow(_title, 100, 100, _width, _height, (uint)_windowFlags);
        
        if (_window == null)
        {
            throw new Exception("Failed to create window");
        }
        
        _sdl.ShowWindow(_window);
        
    }

    private DateTime _lastFrameTime;
    private double[] _fpsLog = new double[200];
    private int _fpsLogIndex = 0;
    private double fpsLogTimeOut = 2; // 5 seconds
    private double fpsLogTime = 0;
    
    public unsafe void Run()
    {        
        Load();

        _isRunning = true;
        
        _lastFrameTime = DateTime.Now;
        while (_isRunning)
        {
            // Poll events
            var ev = new Event();
            while (_sdl.PollEvent(&ev) != 0)
            {
                if (ev.Type == (uint)EventType.Quit)
                {
                    _isRunning = false;
                }

                if (ev.Type == (uint)EventType.Mousebuttondown)
                {
                    var mouseEvent = *(MouseButtonEvent*)&ev;
                    Logger.Info("Mouse Button Down", $"Button: {mouseEvent.Button}, Clicks: {mouseEvent.Clicks}, X: {mouseEvent.X}, Y: {mouseEvent.Y}");
                    Input.UpdateMouseButton(mouseEvent);
                }
                
                if (ev.Type == (uint)EventType.Mousebuttonup)
                {
                    var mouseEvent = *(MouseButtonEvent*)&ev;
                    Input.UpdateMouseButton(mouseEvent);
                }
                
                if (ev.Type == (uint)EventType.Mousemotion)
                {
                    var mouseEvent = *(MouseMotionEvent*)&ev;
                    Input.UpdateMousePosition(new Vector2(mouseEvent.X, mouseEvent.Y));
                    // Mouse motion not logged because it's too spammy
                }
                
                if (ev.Type == (uint)EventType.Keydown)
                {
                    var keyEvent = *(KeyboardEvent*)&ev;
                    Input.UpdateKeyDown((KeyCode)keyEvent.Keysym.Sym);
                }
                
                if (ev.Type == (uint)EventType.Keyup)
                {
                    var keyEvent = *(KeyboardEvent*)&ev;
                    Input.UpdateKeyUp((KeyCode)keyEvent.Keysym.Sym);
                }
            }
            
            Input.Update();
            
            int width = _width;
            int height = _height;
            _sdl.GetWindowSize(_window, ref _width, ref _height); // Update window size
            
            if (width != _width || height != _height)
            {
                OnResize?.Invoke(_width, _height);
            }
            
            var currentTime = DateTime.Now;
            DeltaTime = (currentTime - _lastFrameTime).TotalSeconds;
            _lastFrameTime = currentTime;
            
            Update();
            OnUpdate?.Invoke();

            Render();
            
            fpsLogTime += DeltaTime;
            if (fpsLogTime >= fpsLogTimeOut)
            {
                Logger.Info("FPS", $"RAW: {(1 / DeltaTime).ToString("0.00")}, AVG: {_fpsLog.Average()}");
                Console.WriteLine("FPS: " + (1 / DeltaTime).ToString("0.00") + " AVG: " + (_fpsLog.Average()));
                fpsLogTime = 0;
            }
            
            _fpsLog[_fpsLogIndex] = 1 / DeltaTime;
            _fpsLogIndex = (_fpsLogIndex + 1) % _fpsLog.Length;
            
            Time.Update(DeltaTime);
        }
        
        OnUnload?.Invoke();
        Unload();
        
        _sdl.DestroyWindow(_window);
        _sdl.Quit();
    }
    
    public unsafe void SetWindowMode(WindowFlags flags)
    {
        _sdl.SetWindowFullscreen(_window, (uint)flags);
        
        _windowFlags = flags;
        _sdl.GetWindowSize(_window, ref _width, ref _height); // Update window size

        OnResize?.Invoke(_width, _height);
    }
    
    public abstract void Load();
    public abstract void Update();
    public abstract void Render();
    public abstract void Unload();
    
    public Action OnLoad;
    public Action OnUpdate; // onrender changes per api so it's not here
    public Action OnUnload;
    
    public Action<int, int> OnResize;
    
    public void SetWindowed()
    {
        SetWindowMode(WindowFlags.Resizable);
        IsFullscreen = false;
    }
    
    /// <summary>
    /// Sets the window to fullscreen. Uses borderless fullscreen.
    /// </summary>
    public void SetFullscreen()
    {
        SetWindowMode(WindowFlags.FullscreenDesktop);
        IsFullscreen = true;
    }
}