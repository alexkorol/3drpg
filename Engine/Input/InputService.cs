using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rpg3D.Engine.Core;

namespace Rpg3D.Engine.Input;

public sealed class InputService : IUpdateSystem
{
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;
    private MouseState _currentMouse;
    private MouseState _previousMouse;
    private Point _mouseDelta;
    private int _scrollDelta;
    private GameWindow? _window;
    private Game? _game;
    private bool _initialized;
    private bool _captureMouse;

    public InputSnapshot Snapshot => new(
        _currentKeyboard,
        _previousKeyboard,
        _currentMouse,
        _previousMouse,
        _mouseDelta,
        _scrollDelta);

    public bool CaptureMouse
    {
        get => _captureMouse;
        set
        {
            _captureMouse = value;
            if (_game != null)
            {
                _game.IsMouseVisible = !value;
            }

            if (value)
            {
                CenterCursor();
            }
        }
    }

    public void Initialize(ServiceRegistry services)
    {
        _window = services.Require<GameWindow>();
        _game = services.Require<Game>();
        _currentKeyboard = Keyboard.GetState();
        _previousKeyboard = _currentKeyboard;
        _currentMouse = Mouse.GetState();
        _previousMouse = _currentMouse;
        _mouseDelta = Point.Zero;
        _scrollDelta = 0;
        _initialized = true;
    }

    public void Update(GameClock clock)
    {
        if (!_initialized)
        {
            return;
        }

        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;

        _currentKeyboard = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        _mouseDelta = new Point(mouseState.X - _previousMouse.X, mouseState.Y - _previousMouse.Y);
        _scrollDelta = mouseState.ScrollWheelValue - _previousMouse.ScrollWheelValue;
        _currentMouse = mouseState;

        if (_captureMouse && _window != null)
        {
            var bounds = _window.ClientBounds;
            var centerX = bounds.Left + bounds.Width / 2;
            var centerY = bounds.Top + bounds.Height / 2;

            if (_currentMouse.X != centerX || _currentMouse.Y != centerY)
            {
                Mouse.SetPosition(centerX, centerY);
                _currentMouse = new MouseState(
                    centerX,
                    centerY,
                    mouseState.ScrollWheelValue,
                    mouseState.LeftButton,
                    mouseState.MiddleButton,
                    mouseState.RightButton,
                    mouseState.XButton1,
                    mouseState.XButton2);
            }
        }
    }

    private void CenterCursor()
    {
        if (_window == null)
        {
            return;
        }

        var bounds = _window.ClientBounds;
        var centerX = bounds.Left + bounds.Width / 2;
        var centerY = bounds.Top + bounds.Height / 2;
        Mouse.SetPosition(centerX, centerY);
        var mouseState = Mouse.GetState();
        _currentMouse = mouseState;
        _previousMouse = mouseState;
        _mouseDelta = Point.Zero;
        _scrollDelta = 0;
    }
}
