using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rpg3D.Engine.Core;

namespace Rpg3D.Game.Systems;

public sealed class UiSystem : IDrawSystem, IDisposable
{
    private GraphicsDevice? _graphicsDevice;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;

    public float PlayerHealth { get; set; } = 8f;

    public float PlayerHealthMax { get; set; } = 8f;

    public string EquippedItemName { get; set; } = "Staff";

    public Color WeaponHandleColor { get; set; } = new(160, 118, 68);

    public Color WeaponHeadColor { get; set; } = new(70, 190, 255);

    public float WeaponSwingOffsetX { get; set; }

    public float WeaponBobOffsetY { get; set; }

    public float WeaponAttackOffsetX { get; set; }

    public bool ShowCrosshair { get; set; } = true;

    public Color CrosshairColor { get; set; } = new(230, 240, 255, 180);

    public void Initialize(ServiceRegistry services)
    {
        _graphicsDevice = services.Require<GraphicsDevice>();
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _pixel = new Texture2D(_graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(GameClock clock)
    {
        if (_spriteBatch == null || _pixel == null || _graphicsDevice == null)
        {
            return;
        }

        var viewport = _graphicsDevice.Viewport;
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);

        DrawHealthBar(viewport);
        DrawHotbar(viewport);
        DrawWeapon(viewport, clock);
        DrawCrosshair(viewport);

        _spriteBatch.End();
    }

    private void DrawHealthBar(Viewport viewport)
    {
        if (_spriteBatch == null || _pixel == null)
        {
            return;
        }

        const int padding = 12;
        const int height = 16;
        const int width = 180;

        var x = padding;
        var y = viewport.Height - padding - height;

        var backgroundRect = new Rectangle(x, y, width, height);
        var fillRatio = MathHelper.Clamp(PlayerHealth / Math.Max(0.01f, PlayerHealthMax), 0f, 1f);
        var fillWidth = (int)(width * fillRatio);
        var fillRect = new Rectangle(x + 2, y + 2, fillWidth - 4, height - 4);

        _spriteBatch.Draw(_pixel, backgroundRect, new Color(20, 10, 16, 220));
        if (fillRect.Width > 0)
        {
            _spriteBatch.Draw(_pixel, fillRect, new Color(200, 40, 50));
        }
    }

    private void DrawWeapon(Viewport viewport, GameClock clock)
    {
        if (_spriteBatch == null || _pixel == null)
        {
            return;
        }

        var baseX = viewport.Width - 200;
        var baseY = viewport.Height - 220;
        baseX = (int)MathF.Round(baseX + WeaponSwingOffsetX + WeaponAttackOffsetX);
        baseY = (int)MathF.Round(baseY + WeaponBobOffsetY);

        var handleRect = new Rectangle(baseX, baseY, 22, 160);
        _spriteBatch.Draw(_pixel, handleRect, WeaponHandleColor);

        var headRect = new Rectangle(baseX - 12, baseY - 32, 46, 46);
        _spriteBatch.Draw(_pixel, headRect, WeaponHeadColor);
    }

    private void DrawCrosshair(Viewport viewport)
    {
        if (_spriteBatch == null || _pixel == null || !ShowCrosshair)
        {
            return;
        }

        var centerX = viewport.Width / 2;
        var centerY = viewport.Height / 2;
        const int size = 4;
        const int gap = 3;
        const int thickness = 2;

        var color = CrosshairColor;
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - gap - size, centerY - thickness / 2, size, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX + gap, centerY - thickness / 2, size, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - thickness / 2, centerY - gap - size, thickness, size), color);
        _spriteBatch.Draw(_pixel, new Rectangle(centerX - thickness / 2, centerY + gap, thickness, size), color);
    }

    private void DrawHotbar(Viewport viewport)
    {
        if (_spriteBatch == null || _pixel == null)
        {
            return;
        }

        const int slotWidth = 48;
        const int slotHeight = 48;
        const int slotSpacing = 6;
        const int totalSlots = 6;

        var totalWidth = (slotWidth * totalSlots) + slotSpacing * (totalSlots - 1);
        var startX = (viewport.Width - totalWidth) / 2;
        var y = viewport.Height - slotHeight - 24;

        for (var i = 0; i < totalSlots; i++)
        {
            var rect = new Rectangle(startX + i * (slotWidth + slotSpacing), y, slotWidth, slotHeight);
            var color = i == 0 ? new Color(70, 60, 80, 220) : new Color(40, 34, 48, 200);
            _spriteBatch.Draw(_pixel, rect, color);
        }
    }

    public void Dispose()
    {
        _pixel?.Dispose();
        _spriteBatch?.Dispose();
    }
}
