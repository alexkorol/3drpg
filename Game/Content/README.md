# Content Structure

Below are the minimum asset drops needed so I can wire up rendering and gameplay. Feel free to iterate on them later; placeholders are perfectly fine.

## Textures
- `dungeon_floor.png`: 64x64 tile, palette-limited stone floor.
- `dungeon_wall.png`: 64x64 tile for walls (matches floor palette).
- `ceiling_plain.png`: 64x64 neutral ceiling tile.

Place these in `Textures/`. I’ll handle material metadata and binding.

## Sprites
- `skeleton_spritesheet.png`: 64x64 frames, idle/walk/attack/death (8–12 frames total).
- `pickup_potion.png`: 32x32 item icon (front-facing).

Drop both into `Sprites/`; I can generate the accompanying animation JSON once the sheets exist.

## Particles
- `spark_orange.png`: 16x16 solid-color square for sparks.
- `smoke_square.png`: 32x32 soft-edged grey square (a blurred block is fine).

Place them in `Particles/`. No need for fancy artwork yet—just colored quads.

## UI
- `hud_panel.png`: 256x64 panel strip (used for health/stamina bars).
- `font_retrovga.png`: 128x128 pixel font atlas (16x16 glyph grid).
- `icon_sword.png`: 32x32 inventory icon.

These belong in `UI/`. Once they’re in place I’ll hook them into the HUD/inventory systems.

## Map Glyphs (ASCII)
- `#` wall, `.` floor, `~` shallow water tint.
- `P` player spawn, `T` torch (light + flame emitter).
- `E` ambient enemy sprite placeholder.
- `^` stair up, `v` stair down.
