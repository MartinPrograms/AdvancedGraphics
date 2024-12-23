﻿using Silk.NET.SDL;

namespace Common;

using System.Numerics;

public static class Input
{
    private static List<KeyCode> _keysDown = new();
    private static List<KeyCode> _previousKeysDown = new();
    private static List<KeyCode> _nextKeysDown = new();
    
    public static bool IsKeyDown(KeyCode key) => _keysDown.Contains(key);
    public static bool IsKeyUp(KeyCode key) => !_keysDown.Contains(key);
    public static bool IsKeyPressed(KeyCode key) => _keysDown.Contains(key) && !_previousKeysDown.Contains(key);
    public static bool IsKeyReleased(KeyCode key) => !_keysDown.Contains(key) && _previousKeysDown.Contains(key);
    
    private static Vector2 _mousePosition;
    private static Vector2 _previousMousePosition;
    private static Vector2 _nextMousePosition;
    private static Vector2 _mouseDelta => _mousePosition - _previousMousePosition;
    
    public static Vector2 MousePosition => _mousePosition;
    public static Vector2 MouseDelta => _mouseDelta;
    
    private static List<MouseButtonEvent> _mouseButtonDown = new();
    private static List<MouseButtonEvent> _previousMouseButtonDown = new();
    private static List<MouseButtonEvent> _nextMouseButtonDown = new();

    public static bool IsMouseButtonDown(MouseButtonEvent button) =>
        _mouseButtonDown.Any(x => (x.Button.Equals(button.Button) && x.State.Equals(button.State)));
    
    public static void Update()
    {
        _previousKeysDown.Clear();
        _previousKeysDown.AddRange(_keysDown);
        
        _keysDown.Clear();
        _keysDown.AddRange(_nextKeysDown);
        
        _previousMousePosition = _mousePosition;
        _mousePosition = _nextMousePosition;
        
        _previousMouseButtonDown.Clear();
        _previousMouseButtonDown.AddRange(_mouseButtonDown);
        
        _mouseButtonDown.Clear();
        _mouseButtonDown.AddRange(_nextMouseButtonDown);
    }
    
    public static void UpdateMousePosition(Vector2 position)
    {
        _nextMousePosition = position;
    }
    
    public static void UpdateMousePosition(float x, float y)
    {
        _nextMousePosition = new Vector2(x, y);
    }
    
    public static void UpdateMouseButton(MouseButtonEvent @event)
    {
        if (@event.Type == 1)
        {
            _nextMouseButtonDown.Add(@event);
        }
        else
        {
            _nextMouseButtonDown.RemoveAll(x => x.Button == @event.Button);
        }
    }
    
    public static void UpdateKeyDown(KeyCode keycode)
    {
        _nextKeysDown.Add(keycode);
    }
    
    public static void UpdateKeyUp(KeyCode keycode)
    {
        _nextKeysDown.RemoveAll(x => x == keycode);
    }
}