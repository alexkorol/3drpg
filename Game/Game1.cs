using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rpg3D.Engine.Core;
using Rpg3D.Engine.Graphics;
using Rpg3D.Engine.Input;
using Rpg3D.Engine.Rendering;
using Rpg3D.Engine.World;
using Rpg3D.Game.Combat;
using Rpg3D.Game.Systems;

namespace Rpg3D.Game;

public sealed class Game1 : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GameHost _host = new();
    private readonly Camera3D _camera = new();
    private readonly InputService _inputService = new();
    private readonly GridRenderer _gridRenderer = new();
    private readonly SceneLighting _lighting = new();
    private readonly BillboardRenderer _billboards = new();
    private readonly ParticleSystem _particleSystem = new();
    private readonly UiSystem _uiSystem = new();
    private readonly PlayerControllerSystem _playerController = new();
    private readonly CombatSystem _combatSystem = new();

    private GridMap? _map;
    private readonly List<Vector3> _torchPositions = new();
    private readonly EnemyRoster _enemyRoster = new();
    private readonly List<ParticleEmitter> _torchEmitters = new();
    private float _playerHealth = 10f;
    private float _playerHealthMax = 12f;
    private Vector3 _playerSpawn = new(2.5f, 0f, -2.5f);
    private RenderTarget2D? _sceneTarget;
    private SpriteBatch? _postSpriteBatch;
    private readonly Random _random = new(1337);
    private readonly Dictionary<string, Texture2D> _worldTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Texture2D> _enemySprites = new();
    private Texture2D? _smokeTexture;
    private readonly string[] _mapAssets = { "Maps/intro.ascii", "Maps/test_30X30.ascii" };
    private int _mapIndex;
    private readonly float[] _renderScaleOptions = { 0.5f, 0.6f, 0.75f, 1f };
    private int _renderScaleIndex = 1;
    private float _renderScale;
    private readonly string _logPath;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            SynchronizeWithVerticalRetrace = false,
            PreferMultiSampling = false
        };

        Content.RootDirectory = "Content";
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        _renderScale = _renderScaleOptions[_renderScaleIndex];

        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        _logPath = Path.Combine(logDirectory, $"run_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        Log("Game1 constructed.");
    }

    protected override void Initialize()
    {
        Window.AllowUserResizing = true;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        RegisterServices();
        RegisterSystems();
        UpdateCameraAspectRatio();
        RecreateSceneRenderTarget();
        _host.Initialize();

        Window.ClientSizeChanged += OnClientSizeChanged;

        Log("Initialize completed.");
        base.Initialize();
    }

    protected override void LoadContent()
    {
        LoadWorldTextures();
        LoadBillboardSprites();
        LoadParticleTextures();
        _mapIndex = Array.IndexOf(_mapAssets, "Maps/test_30X30.ascii");
        if (_mapIndex < 0)
        {
            _mapIndex = 0;
        }
        LoadMap(_mapAssets[_mapIndex]);
        Log("LoadContent completed.");
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateLighting(gameTime);
        UpdateEmitters(gameTime);
        UpdateEnemies(gameTime);

        _host.Update(gameTime);
        SubmitSceneBillboards(gameTime);
        UpdateUiState(gameTime);

        var snapshot = _inputService.Snapshot;
        if (snapshot.WasKeyPressed(Keys.F5))
        {
            Log("F5 pressed - cycling render scale.");
            CycleRenderScale();
        }

        if (snapshot.WasKeyPressed(Keys.F6))
        {
            _mapIndex = (_mapIndex + 1) % _mapAssets.Length;
            Log($"F6 pressed - loading map '{_mapAssets[_mapIndex]}'.");
            LoadMap(_mapAssets[_mapIndex]);
        }

        if (_inputService.CaptureMouse && snapshot.WasKeyPressed(Keys.Escape))
        {
            _inputService.CaptureMouse = false;
        }
        else if (!_inputService.CaptureMouse && snapshot.WasKeyPressed(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_sceneTarget != null)
        {
            GraphicsDevice.SetRenderTarget(_sceneTarget);
        }

        GraphicsDevice.Clear(new Color(10, 8, 15));
        _host.Draw(gameTime);

        if (_sceneTarget != null)
        {
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _postSpriteBatch ??= new SpriteBatch(GraphicsDevice);
            _postSpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);

            var viewport = GraphicsDevice.Viewport;
            _postSpriteBatch.Draw(_sceneTarget, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
            _postSpriteBatch.End();
        }

        base.Draw(gameTime);
    }

    private void RegisterServices()
    {
        _host.RegisterService(GraphicsDevice);
        _host.RegisterService(Window);
        _host.RegisterService<Microsoft.Xna.Framework.Game>(this);
        _host.RegisterService(_camera);
        _host.RegisterService(_inputService);
        _host.RegisterService(_lighting);
        _host.RegisterService(_billboards);
        _host.RegisterService(_enemyRoster);
        _host.RegisterService(Content);
    }

    private void RegisterSystems()
    {
        _host.AddSystem(_inputService);
        _host.AddSystem(_gridRenderer);
        _host.AddSystem(_billboards);
        _host.AddSystem(_particleSystem);
        _host.AddSystem(_playerController);
        _host.AddSystem(_combatSystem);
        _host.AddSystem(_uiSystem);
    }

    private void LoadWorldTextures()
    {
        _worldTextures.Clear();
        TryLoadTexture("Textures/floor_tan");
        TryLoadTexture("Textures/ceiling");
        TryLoadTexture("Textures/orange_brick_wall");
        TryLoadTexture("Textures/mossy_brick_wall");
        Log($"World textures loaded: {_worldTextures.Count}");
    }

    private void LoadBillboardSprites()
    {
        _enemySprites.Clear();
        TryLoadSprite("Sprites/skeleton_front");
        TryLoadSprite("Sprites/troll_front");
        Log($"Enemy sprites loaded: {_enemySprites.Count}");
    }

    private void LoadParticleTextures()
    {
        _smokeTexture = TryLoadOptionalTexture("Particles/soft_gray_smoke");
        Log($"Particle texture available: {_smokeTexture != null}");
    }

    private void TryLoadTexture(string assetName)
    {
        try
        {
            _worldTextures[assetName] = Content.Load<Texture2D>(assetName);
            Log($"Loaded texture asset '{assetName}'.");
        }
        catch (ContentLoadException)
        {
            Log($"Missing texture asset '{assetName}'.");
        }
    }

    private void TryLoadSprite(string assetName)
    {
        try
        {
            _enemySprites.Add(Content.Load<Texture2D>(assetName));
            Log($"Loaded sprite asset '{assetName}'.");
        }
        catch (ContentLoadException)
        {
            Log($"Missing sprite asset '{assetName}'.");
        }
    }

    private Texture2D? TryLoadOptionalTexture(string assetName)
    {
        try
        {
            var tex = Content.Load<Texture2D>(assetName);
            Log($"Loaded optional texture '{assetName}'.");
            return tex;
        }
        catch (ContentLoadException)
        {
            Log($"Optional texture '{assetName}' not found.");
            return null;
        }
    }

    private void LoadMap(string assetPath)
    {
        var contentPath = Path.Combine(Content.RootDirectory, assetPath).Replace('\\', '/');
        Log($"Attempting to load map stream '{contentPath}'.");

        try
        {
            using var stream = TitleContainer.OpenStream(contentPath);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                lines.Add(reader.ReadLine() ?? string.Empty);
            }

            ParseMarkers(lines);
            _map = AsciiMapLoader.FromLines(lines);

            var mesh = GridMeshBuilder.Build(_map, cellSize: 1f, wallHeight: 2.5f);
            _gridRenderer.SetMesh(mesh, _worldTextures);
            _playerController.SetMap(_map);
            _playerController.SetSpawnPoint(_playerSpawn);
            SpawnTorchEmitters();

            Log($"Loaded map '{assetPath}' with {mesh.Parts.Count} mesh parts.");
            Log($"Player spawn world position: {_playerSpawn}");
            Log($"Torch count: {_torchPositions.Count}");
        }
        catch (Exception ex)
        {
            Log($"Failed to load map '{assetPath}': {ex}");
        }
    }

    private void ParseMarkers(IReadOnlyList<string> lines)
    {
        _torchPositions.Clear();
        _enemyRoster.Clear();
        _torchEmitters.Clear();
        _playerSpawn = new Vector3(2.5f, 0f, -2.5f);

        for (var y = 0; y < lines.Count; y++)
        {
            var line = lines[y];
            for (var x = 0; x < line.Length; x++)
            {
                var glyph = line[x];
                var center = CellToWorldCenter(x, y);

                switch (glyph)
                {
                    case 'P':
                        _playerSpawn = center;
                        break;
                    case 'T':
                        _torchPositions.Add(center + new Vector3(0f, 1.4f, 0f));
                        break;
                    case 'E':
                        _enemyRoster.Add(CreateAmbientEnemy(center + new Vector3(0f, 0.4f, 0f)));
                        break;
                }
            }
        }
    }

    private static Vector3 CellToWorldCenter(int x, int y)
    {
        return new Vector3(x + 0.5f, 0f, -(y + 0.5f));
    }

    private void SpawnTorchEmitters()
    {
        _torchEmitters.Clear();
        _particleSystem.ClearEmitters();

        foreach (var torchPos in _torchPositions)
        {
            var emitter = new ParticleEmitter
            {
                Position = torchPos,
                Direction = Vector3.Up,
                SpawnRate = 18f,
                MinSpeed = 0.2f,
                MaxSpeed = 0.6f,
                StartSize = 0.35f,
                EndSize = 0.1f,
                StartColor = new Color(255, 180, 80, 220),
                EndColor = new Color(255, 80, 40, 0),
                MinLifetime = 0.35f,
                MaxLifetime = 0.7f,
                Additive = true,
                Texture = _smokeTexture
            };

            _particleSystem.AddEmitter(emitter);
            _torchEmitters.Add(emitter);
        }
    }

    private void UpdateLighting(GameTime gameTime)
    {
        var t = (float)gameTime.TotalGameTime.TotalSeconds;
        var flicker = 0.12f * MathF.Sin(t * 3.3f) + 0.05f * MathF.Sin(t * 4.7f + 1.2f);

        var mainBase = new Vector3(0.72f, 0.62f, 0.54f);
        var main = Vector3.Clamp(mainBase + new Vector3(flicker * 0.25f, flicker * 0.2f, flicker * 0.12f), Vector3.Zero, Vector3.One);
        _lighting.MainLightColor = new Color(main);

        var ambientBase = new Vector3(0.12f, 0.12f, 0.18f);
        var ambient = Vector3.Clamp(ambientBase + new Vector3(flicker * 0.08f), Vector3.Zero, Vector3.One);
        _lighting.AmbientColor = new Color(ambient);

        _lighting.MainLightDirection = Vector3.Normalize(new Vector3(-0.55f, -1.1f, 0.35f));
        _lighting.FogColor = new Color(new Vector3(0.05f, 0.04f, 0.08f));
        _lighting.FogStart = 6f;
        _lighting.FogEnd = 32f;

        _lighting.PointLights.Clear();
        foreach (var torch in _torchPositions)
        {
            var lightColor = new Color(255, 200, 150);
            var radius = 4.5f;
            var intensity = 1.35f;
            _lighting.PointLights.Add(new PointLight(torch, lightColor, radius, intensity));
        }
    }

    private void UpdateEmitters(GameTime gameTime)
    {
        var t = (float)gameTime.TotalGameTime.TotalSeconds;
        for (var i = 0; i < _torchEmitters.Count; i++)
        {
            var basePos = _torchPositions[i];
            var offset = new Vector3(
                0f,
                0.05f * MathF.Sin(t * 8f + i),
                0.05f * MathF.Cos(t * 6f + i * 1.3f));
            _torchEmitters[i].Position = basePos + offset;
        }
    }

    private void SubmitSceneBillboards(GameTime gameTime)
    {
        var t = (float)gameTime.TotalGameTime.TotalSeconds;

        foreach (var torch in _torchPositions)
        {
            var flicker = 0.1f + 0.05f * MathF.Sin(t * 10f + torch.X);
            var flameColor = new Color(255, 180, 90, (int)(200 + flicker * 40f));
            _billboards.Submit(new BillboardInstance(torch, new Vector2(0.55f, 1.2f + flicker * 0.6f), flameColor, additive: true));

            var glowColor = new Color(255, 200, 120, 60);
            _billboards.Submit(new BillboardInstance(torch + new Vector3(0f, -0.8f, 0f), new Vector2(1.2f, 0.6f), glowColor, additive: true));
        }

        foreach (var enemy in _enemyRoster.Enemies)
        {
            if (enemy.IsDead)
            {
                continue;
            }

            var tint = enemy.GetCurrentTint();
            if (enemy.Sprite != null)
            {
                _billboards.Submit(new BillboardInstance(enemy.Position, enemy.SpriteSize, tint, enemy.Sprite));
            }
            else
            {
                _billboards.Submit(new BillboardInstance(enemy.Position, enemy.SpriteSize, tint));
            }

            _billboards.Submit(new BillboardInstance(enemy.Position + new Vector3(0f, -0.5f, 0f), new Vector2(1.3f, 0.7f), enemy.GlowColor, additive: true));

            if (enemy.HitFlashTimer > 0f)
            {
                var flashStrength = MathHelper.Clamp(enemy.HitFlashTimer / 0.18f, 0f, 1f);
                var flashSize = enemy.SpriteSize * (1.1f + flashStrength * 0.6f);
                var flashColor = Color.FromNonPremultiplied(255, 210, 120, (int)(flashStrength * 180));
                _billboards.Submit(new BillboardInstance(enemy.Position + new Vector3(0f, 0.1f, 0f), flashSize, flashColor, additive: true));
            }
        }
    }

    private void UpdateUiState(GameTime gameTime)
    {
        var snapshot = _inputService.Snapshot;
        var total = (float)gameTime.TotalGameTime.TotalSeconds;
        var moving = snapshot.IsKeyDown(Keys.W) || snapshot.IsKeyDown(Keys.A) || snapshot.IsKeyDown(Keys.S) || snapshot.IsKeyDown(Keys.D);
        var moveFactor = moving ? 1f : 0.4f;

        _uiSystem.PlayerHealth = _playerHealth;
        _uiSystem.PlayerHealthMax = _playerHealthMax;
        _uiSystem.EquippedItemName = "Frostbrand";
        _uiSystem.WeaponHandleColor = new Color(150, 100, 50);

        var baseSwing = MathHelper.Clamp(-snapshot.MouseDelta.X * 0.7f, -18f, 18f);
        _uiSystem.WeaponSwingOffsetX = baseSwing;

        var attackStrength = _combatSystem.AttackSwingStrength;
        _uiSystem.WeaponAttackOffsetX = MathHelper.Lerp(0f, 34f, attackStrength);
        _uiSystem.WeaponBobOffsetY = MathF.Sin(total * 8f) * 8f * moveFactor - attackStrength * 12f;

        var hitStrength = _combatSystem.HitFlashStrength;
        var headBase = new Color(80, 210, 255);
        var headFlash = new Color(255, 200, 120);
        _uiSystem.WeaponHeadColor = Color.Lerp(headBase, headFlash, MathF.Max(hitStrength, attackStrength * 0.35f));

        var crosshairBase = Color.FromNonPremultiplied(230, 240, 255, moving ? 200 : 150);
        if (hitStrength > 0f)
        {
            var hitColor = Color.FromNonPremultiplied(255, 150, 110, 255);
            crosshairBase = Color.Lerp(crosshairBase, hitColor, hitStrength);
        }
        else if (attackStrength > 0f)
        {
            var swingColor = Color.FromNonPremultiplied(240, 220, 255, 220);
            crosshairBase = Color.Lerp(crosshairBase, swingColor, attackStrength * 0.4f);
        }

        _uiSystem.CrosshairColor = crosshairBase;
    }

    private void UpdateEnemies(GameTime gameTime)
    {
        var time = (float)gameTime.TotalGameTime.TotalSeconds;
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var enemy in _enemyRoster.Enemies)
        {
            var offset = new Vector3(
                MathF.Sin(time * enemy.OrbitSpeed + enemy.Phase) * enemy.OrbitRadius,
                MathF.Sin(time * enemy.BobSpeed + enemy.Phase) * enemy.BobHeight,
                MathF.Cos(time * enemy.OrbitSpeed + enemy.Phase) * enemy.OrbitRadius);
            enemy.Position = enemy.Origin + offset;

            if (enemy.HitFlashTimer > 0f)
            {
                enemy.HitFlashTimer = Math.Max(0f, enemy.HitFlashTimer - dt);
            }
        }

        _enemyRoster.RemoveDead(enemy => Log($"Enemy defeated at {enemy.Position}"));
    }

    private EnemyInstance CreateAmbientEnemy(Vector3 origin)
    {
        Texture2D? sprite = _enemySprites.Count > 0 ? _enemySprites[_random.Next(_enemySprites.Count)] : null;
        var tintFactor = (float)_random.NextDouble();
        var tint = sprite != null ? Color.White : Color.Lerp(new Color(60, 200, 170, 230), new Color(130, 210, 250, 230), tintFactor);
        var glow = Color.Lerp(new Color(40, 120, 110, 80), new Color(90, 170, 220, 90), tintFactor);

        var spriteSize = sprite != null
            ? new Vector2(sprite.Width / 64f, sprite.Height / 64f)
            : new Vector2(0.9f, 1.4f);

        var enemy = new EnemyInstance
        {
            Origin = origin,
            Position = origin,
            Sprite = sprite,
            SpriteSize = spriteSize,
            BaseTint = tint,
            GlowColor = glow,
            OrbitRadius = 0.45f + (float)_random.NextDouble() * 0.35f,
            OrbitSpeed = 0.6f + (float)_random.NextDouble() * 0.3f,
            BobHeight = 0.2f + (float)_random.NextDouble() * 0.15f,
            BobSpeed = 2.4f + (float)_random.NextDouble(),
            Phase = (float)_random.NextDouble() * MathHelper.TwoPi
        };

        enemy.MaxHealth = 8f + (float)_random.NextDouble() * 4f;
        enemy.Health = enemy.MaxHealth;
        return enemy;
    }

    private void UpdateCameraAspectRatio()
    {
        var viewport = GraphicsDevice.Viewport;
        _camera.SetAspectRatio(viewport.Width, viewport.Height);
    }

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        UpdateCameraAspectRatio();
        RecreateSceneRenderTarget();
    }

    private void RecreateSceneRenderTarget()
    {
        _sceneTarget?.Dispose();

        var backBuffer = GraphicsDevice.PresentationParameters;
        var width = Math.Max(160, (int)(backBuffer.BackBufferWidth * _renderScale));
        var height = Math.Max(120, (int)(backBuffer.BackBufferHeight * _renderScale));

        _sceneTarget = new RenderTarget2D(
            GraphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.Depth24);

        _postSpriteBatch ??= new SpriteBatch(GraphicsDevice);
    }

    private void CycleRenderScale()
    {
        _renderScaleIndex = (_renderScaleIndex + 1) % _renderScaleOptions.Length;
        _renderScale = _renderScaleOptions[_renderScaleIndex];
        if (!GraphicsDevice.IsDisposed)
        {
            RecreateSceneRenderTarget();
        }
    }

    private void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:O}] {message}";
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch
        {
            // ignore logging failures
        }
    }
}
