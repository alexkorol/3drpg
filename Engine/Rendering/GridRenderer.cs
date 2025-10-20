using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rpg3D.Engine.Core;
using Rpg3D.Engine.Graphics;

namespace Rpg3D.Engine.Rendering;

public sealed class GridRenderer : IDrawSystem, IDisposable
{
    private const string LogPrefix = "[GridRenderer]";
    private GraphicsDevice? _graphicsDevice;
    private Camera3D? _camera;
    private BasicEffect? _effect;
    private SceneLighting? _lighting;
    private readonly List<MeshPart> _parts = new();
    private Texture2D? _fallbackTexture;

    public void Initialize(ServiceRegistry services)
    {
        _graphicsDevice = services.Require<GraphicsDevice>();
        _camera = services.Require<Camera3D>();
        _lighting = services.Require<SceneLighting>();
        _effect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = true,
            LightingEnabled = true,
            PreferPerPixelLighting = false,
            World = Matrix.Identity
        };
        _effect.EnableDefaultLighting();
        _effect.FogEnabled = true;
        _effect.FogColor = _lighting.FogColor.ToVector3();
        _effect.FogStart = _lighting.FogStart;
        _effect.FogEnd = _lighting.FogEnd;

        _fallbackTexture = new Texture2D(_graphicsDevice, 1, 1);
        _fallbackTexture.SetData(new[] { Color.White });
    }

    public void SetMesh(GridMesh meshData, IReadOnlyDictionary<string, Texture2D> textures)
    {
        DisposeParts();
        if (_graphicsDevice == null)
        {
            return;
        }

        foreach (var partData in meshData.Parts)
        {
            if (!textures.TryGetValue(partData.TextureKey, out var texture))
            {
                texture = _fallbackTexture ?? throw new InvalidOperationException("Fallback texture not initialized.");
            }

            var vertexBuffer = new VertexBuffer(
                _graphicsDevice,
                typeof(VertexPositionNormalTexture),
                partData.Vertices.Length,
                BufferUsage.WriteOnly);
            vertexBuffer.SetData(partData.Vertices);

            var indexBuffer = new IndexBuffer(
                _graphicsDevice,
                IndexElementSize.SixteenBits,
                partData.Indices.Length,
                BufferUsage.WriteOnly);
            indexBuffer.SetData(partData.Indices);

            _parts.Add(new MeshPart(vertexBuffer, indexBuffer, texture));
        }

        Console.WriteLine($"{LogPrefix} Loaded mesh with {_parts.Count} parts.");
    }

    public void Draw(GameClock clock)
    {
        if (_graphicsDevice == null || _effect == null || _camera == null || _parts.Count == 0)
        {
            return;
        }

        _effect.View = _camera.View;
        _effect.Projection = _camera.Projection;
        if (_lighting != null)
        {
            _effect.AmbientLightColor = _lighting.AmbientColor.ToVector3();
            _effect.DirectionalLight0.Enabled = true;
            _effect.DirectionalLight0.Direction = _lighting.MainLightDirection;
            _effect.DirectionalLight0.DiffuseColor = _lighting.MainLightColor.ToVector3();
            _effect.DirectionalLight0.SpecularColor = Vector3.Zero;
            _effect.FogColor = _lighting.FogColor.ToVector3();
            _effect.FogStart = _lighting.FogStart;
            _effect.FogEnd = _lighting.FogEnd;
        }

        var previousRasterizer = _graphicsDevice.RasterizerState;
        var previousBlend = _graphicsDevice.BlendState;
        var previousDepth = _graphicsDevice.DepthStencilState;
        var previousSampler = _graphicsDevice.SamplerStates[0];

        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        foreach (var part in _parts)
        {
            _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
            _graphicsDevice.Indices = part.IndexBuffer;

            _effect.Texture = part.Texture;
            _effect.DiffuseColor = Vector3.One;
            _effect.Alpha = 1f;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    baseVertex: 0,
                    startIndex: 0,
                    primitiveCount: part.IndexBuffer.IndexCount / 3);
            }
        }

        _graphicsDevice.RasterizerState = previousRasterizer;
        _graphicsDevice.BlendState = previousBlend;
        _graphicsDevice.DepthStencilState = previousDepth;
        _graphicsDevice.SamplerStates[0] = previousSampler;
        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.Indices = null;
    }

    public void Dispose()
    {
        DisposeParts();
        _effect?.Dispose();
        _fallbackTexture?.Dispose();
    }

    private void DisposeParts()
    {
        foreach (var part in _parts)
        {
            part.VertexBuffer.Dispose();
            part.IndexBuffer.Dispose();
        }
        _parts.Clear();
    }

    private sealed record MeshPart(VertexBuffer VertexBuffer, IndexBuffer IndexBuffer, Texture2D Texture);
}
