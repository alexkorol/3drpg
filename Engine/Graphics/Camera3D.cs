using Microsoft.Xna.Framework;

namespace Rpg3D.Engine.Graphics;

public sealed class Camera3D
{
    private float _pitch;
    private float _yaw;
    private Vector3 _position = new(0f, 1.8f, 0f);
    private float _aspectRatio = 16f / 9f;

    public Matrix View { get; private set; } = Matrix.Identity;

    public Matrix Projection { get; private set; } = Matrix.Identity;

    public float FieldOfView { get; set; } = MathHelper.ToRadians(70f);

    public float NearClip { get; set; } = 0.05f;

    public float FarClip { get; set; } = 256f;

    public Vector3 Position
    {
        get => _position;
        set
        {
            _position = value;
            RecalculateView();
        }
    }

    public float Pitch
    {
        get => _pitch;
        set
        {
            _pitch = MathHelper.Clamp(value, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);
            RecalculateView();
        }
    }

    public float Yaw
    {
        get => _yaw;
        set
        {
            _yaw = value;
            RecalculateView();
        }
    }

    public Vector3 Forward => Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f));

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));

    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));

    public void SetAspectRatio(int width, int height)
    {
        if (height <= 0)
        {
            return;
        }

        _aspectRatio = width / (float)height;
        RecalculateProjection();
    }

    public void Move(Vector3 delta)
    {
        _position += delta;
        RecalculateView();
    }

    public void SetRotation(float yaw, float pitch)
    {
        _yaw = yaw;
        Pitch = pitch;
    }

    public void ApplyRotation(float deltaYaw, float deltaPitch)
    {
        _yaw += deltaYaw;
        Pitch = _pitch + deltaPitch;
    }

    private void RecalculateView()
    {
        var target = _position + Forward;
        View = Matrix.CreateLookAt(_position, target, Vector3.Up);
    }

    public void RecalculateProjection()
    {
        Projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView, _aspectRatio, NearClip, FarClip);
    }
}
