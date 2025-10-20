using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Rpg3D.Engine.Input;

public readonly struct InputSnapshot
{
    public InputSnapshot(
        KeyboardState currentKeyboard,
        KeyboardState previousKeyboard,
        MouseState currentMouse,
        MouseState previousMouse,
        Point mouseDelta,
        int scrollDelta)
    {
        CurrentKeyboard = currentKeyboard;
        PreviousKeyboard = previousKeyboard;
        CurrentMouse = currentMouse;
        PreviousMouse = previousMouse;
        MouseDelta = mouseDelta;
        ScrollDelta = scrollDelta;
    }

    public KeyboardState CurrentKeyboard { get; }

    public KeyboardState PreviousKeyboard { get; }

    public MouseState CurrentMouse { get; }

    public MouseState PreviousMouse { get; }

    public Point MouseDelta { get; }

    public int ScrollDelta { get; }

    public bool IsKeyDown(Keys key) => CurrentKeyboard.IsKeyDown(key);

    public bool WasKeyPressed(Keys key) =>
        CurrentKeyboard.IsKeyDown(key) && !PreviousKeyboard.IsKeyDown(key);

    public bool WasKeyReleased(Keys key) =>
        !CurrentKeyboard.IsKeyDown(key) && PreviousKeyboard.IsKeyDown(key);
}
