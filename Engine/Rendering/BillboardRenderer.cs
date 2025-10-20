using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rpg3D.Engine.Core;
using Rpg3D.Engine.Graphics;

namespace Rpg3D.Engine.Rendering;

public readonly struct BillboardInstance
{
    public BillboardInstance(
        Vector3 position,
        Vector2 size,
        Color color,
        Texture2D? texture = null,
        bool additive = false)
    {
        Position = position;
        Size = size;
        Color = color;
        Texture = texture;
        Additive = additive;
    }

    public Vector3 Position { get; }

    public Vector2 Size { get; }

    public Color Color { get; }

    public Texture2D? Texture { get; }

    public bool Additive { get; }
}

public sealed class BillboardRenderer : IUpdateSystem, IDrawSystem, IDisposable
{
    private readonly List<BillboardInstance> _opaque = new();
    private readonly List<BillboardInstance> _additive = new();
    private readonly Dictionary<Texture2D, List<VertexPositionColorTexture>> _batches = new();
    private readonly Stack<List<VertexPositionColorTexture>> _batchPool = new();
    private GraphicsDevice? _graphicsDevice;
    private Camera3D? _camera;
    private BasicEffect? _effect;
    private Texture2D? _whiteTexture;

    public void Initialize(ServiceRegistry services)
    {
        _graphicsDevice = services.Require<GraphicsDevice>();
        _camera = services.Require<Camera3D>();

        _effect = new BasicEffect(_graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = true,
            World = Matrix.Identity
        };
        _whiteTexture = new Texture2D(_graphicsDevice, 1, 1);
        _whiteTexture.SetData(new[] { Color.White });
    }

    public void Update(GameClock clock)
    {
        _opaque.Clear();
        _additive.Clear();
    }

    public void Submit(BillboardInstance instance)
    {
        if (instance.Additive)
        {
            _additive.Add(instance);
        }
        else
        {
            _opaque.Add(instance);
        }
    }

    public void Draw(GameClock clock)
    {
        if (_graphicsDevice == null || _camera == null || _effect == null)
        {
            return;
        }

        _effect.View = _camera.View;
        _effect.Projection = _camera.Projection;

        var cameraRight = _camera.Right;
        var cameraUp = _camera.Up;

        var previousBlend = _graphicsDevice.BlendState;
        var previousDepth = _graphicsDevice.DepthStencilState;
        var previousRasterizer = _graphicsDevice.RasterizerState;
        var previousSampler = _graphicsDevice.SamplerStates[0];

        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        DrawBatch(_opaque, BlendState.NonPremultiplied, cameraRight, cameraUp);
        DrawBatch(_additive, BlendState.Additive, cameraRight, cameraUp);

        _graphicsDevice.BlendState = previousBlend;
        _graphicsDevice.DepthStencilState = previousDepth;
        _graphicsDevice.RasterizerState = previousRasterizer;
        _graphicsDevice.SamplerStates[0] = previousSampler;
    }

    private void DrawBatch(
        List<BillboardInstance> instances,
        BlendState blendState,
        Vector3 cameraRight,
        Vector3 cameraUp)
    {
        if (_graphicsDevice == null || _effect == null || _camera == null || instances.Count == 0)
        {
            return;
        }

        _graphicsDevice.BlendState = blendState;

        instances.Sort((a, b) =>
        {
            var da = Vector3.DistanceSquared(_camera.Position, a.Position);
            var db = Vector3.DistanceSquared(_camera.Position, b.Position);
            return db.CompareTo(da); // back-to-front
        });

        foreach (var instance in instances)
        {
            var texture = instance.Texture ?? _whiteTexture!;
            if (!_batches.TryGetValue(texture, out var batch))
            {
                batch = _batchPool.Count > 0 ? _batchPool.Pop() : new List<VertexPositionColorTexture>();
                _batches.Add(texture, batch);
            }

            var halfRight = cameraRight * (instance.Size.X * 0.5f);
            var halfUp = cameraUp * (instance.Size.Y * 0.5f);
            var center = instance.Position;

            var v0 = center - halfRight + halfUp;
            var v1 = center + halfRight + halfUp;
            var v2 = center - halfRight - halfUp;
            var v3 = center + halfRight - halfUp;

            var color = instance.Color;

            batch.Add(new VertexPositionColorTexture(v0, color, new Vector2(0f, 0f)));
            batch.Add(new VertexPositionColorTexture(v2, color, new Vector2(0f, 1f)));
            batch.Add(new VertexPositionColorTexture(v1, color, new Vector2(1f, 0f)));
            batch.Add(new VertexPositionColorTexture(v1, color, new Vector2(1f, 0f)));
            batch.Add(new VertexPositionColorTexture(v2, color, new Vector2(0f, 1f)));
            batch.Add(new VertexPositionColorTexture(v3, color, new Vector2(1f, 1f)));
        }

        foreach (var (texture, batch) in _batches)
        {
            var vertexArray = batch.ToArray();
            _effect.Texture = texture;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList,
                    vertexArray,
                    vertexOffset: 0,
                    primitiveCount: vertexArray.Length / 3);
            }

            batch.Clear();
            _batchPool.Push(batch);
        }

        _batches.Clear();
    }

    public void Dispose()
    {
        _whiteTexture?.Dispose();
    }
}
