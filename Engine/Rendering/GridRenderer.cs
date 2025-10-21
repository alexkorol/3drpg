using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Rpg3D.Engine.Core;
using Rpg3D.Engine.Graphics;

namespace Rpg3D.Engine.Rendering;

public sealed class GridRenderer : IDrawSystem, IDisposable
{
    private const string LogPrefix = "[GridRenderer]";
    private GraphicsDevice? _graphicsDevice;
    private Camera3D? _camera;
    private SceneLighting? _lighting;
    private ContentManager? _content;
    private Effect? _effect;
    private readonly List<MeshPart> _parts = new();
    private Texture2D? _fallbackTexture;
    private EffectParameter? _worldParam;
    private EffectParameter? _viewParam;
    private EffectParameter? _projectionParam;
    private EffectParameter? _ambientParam;
    private EffectParameter? _directionalDirParam;
    private EffectParameter? _directionalColorParam;
    private EffectParameter? _fogColorParam;
    private EffectParameter? _fogStartParam;
    private EffectParameter? _fogEndParam;
    private EffectParameter? _cameraPositionParam;
    private EffectParameter? _pointLightCountParam;
    private EffectParameter? _pointLightPositionParam;
    private EffectParameter? _pointLightColorParam;
    private EffectParameter? _pointLightRadiusParam;
    private EffectParameter? _pointLightIntensityParam;
    private EffectParameter? _textureParam;

    private const int MaxPointLights = 8;
    private readonly Vector3[] _pointLightPositions = new Vector3[MaxPointLights];
    private readonly Vector3[] _pointLightColors = new Vector3[MaxPointLights];
    private readonly float[] _pointLightRadii = new float[MaxPointLights];
    private readonly float[] _pointLightIntensities = new float[MaxPointLights];

    public void Initialize(ServiceRegistry services)
    {
        _graphicsDevice = services.Require<GraphicsDevice>();
        _camera = services.Require<Camera3D>();
        _lighting = services.Require<SceneLighting>();
        _content = services.Require<ContentManager>();
        _effect = _content.Load<Effect>("Effects/WorldLighting");
        if (_effect.Techniques["WorldLighting"] != null)
        {
            _effect.CurrentTechnique = _effect.Techniques["WorldLighting"];
        }

        _worldParam = _effect.Parameters["World"];
        _viewParam = _effect.Parameters["View"];
        _projectionParam = _effect.Parameters["Projection"];
        _ambientParam = _effect.Parameters["AmbientColor"];
        _directionalDirParam = _effect.Parameters["DirectionalDirection"];
        _directionalColorParam = _effect.Parameters["DirectionalColor"];
        _fogColorParam = _effect.Parameters["FogColor"];
        _fogStartParam = _effect.Parameters["FogStart"];
        _fogEndParam = _effect.Parameters["FogEnd"];
        _cameraPositionParam = _effect.Parameters["CameraPosition"];
        _pointLightCountParam = _effect.Parameters["PointLightCount"];
        _pointLightPositionParam = _effect.Parameters["PointLightPosition"];
        _pointLightColorParam = _effect.Parameters["PointLightColor"];
        _pointLightRadiusParam = _effect.Parameters["PointLightRadius"];
        _pointLightIntensityParam = _effect.Parameters["PointLightIntensity"];
        _textureParam = _effect.Parameters["BaseTexture"];

        _worldParam?.SetValue(Matrix.Identity);

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

        _viewParam?.SetValue(_camera.View);
        _projectionParam?.SetValue(_camera.Projection);
        _cameraPositionParam?.SetValue(_camera.Position);

        if (_lighting != null)
        {
            _ambientParam?.SetValue(_lighting.AmbientColor.ToVector3());
            _directionalDirParam?.SetValue(Vector3.Normalize(_lighting.MainLightDirection));
            _directionalColorParam?.SetValue(_lighting.MainLightColor.ToVector3());
            _fogColorParam?.SetValue(_lighting.FogColor.ToVector3());
            _fogStartParam?.SetValue(_lighting.FogStart);
            _fogEndParam?.SetValue(_lighting.FogEnd);

            var pointLights = _lighting.PointLights;
            var count = pointLights.Count;
            if (count > MaxPointLights)
            {
                count = MaxPointLights;
            }

            for (var i = 0; i < count; i++)
            {
                var light = pointLights[i];
                _pointLightPositions[i] = light.Position;
                _pointLightColors[i] = light.Color.ToVector3();
                _pointLightRadii[i] = light.Radius;
                _pointLightIntensities[i] = light.Intensity;
            }

            for (var i = count; i < MaxPointLights; i++)
            {
                _pointLightPositions[i] = Vector3.Zero;
                _pointLightColors[i] = Vector3.Zero;
                _pointLightRadii[i] = 0f;
                _pointLightIntensities[i] = 0f;
            }

            _pointLightCountParam?.SetValue(count);
            _pointLightPositionParam?.SetValue(_pointLightPositions);
            _pointLightColorParam?.SetValue(_pointLightColors);
            _pointLightRadiusParam?.SetValue(_pointLightRadii);
            _pointLightIntensityParam?.SetValue(_pointLightIntensities);
        }
        else
        {
            _pointLightCountParam?.SetValue(0);
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

            _textureParam?.SetValue(part.Texture);

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
