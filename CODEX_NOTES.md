# Codex Notes

These are the outstanding manual steps or reminders that need your input (things I cannot generate directly).

## Asset Drops
- Place placeholder texture PNGs under `Game/Content/Textures/` (tile sheets, materials, decals).
- Add sprite sheet PNGs to `Game/Content/Sprites/`; matching JSON animation descriptors can be generated here if you provide the frames.
- Drop particle sprite sheets into `Game/Content/Particles/` alongside any emitter JSON you want scripted.
- UI elements (fonts, frame trims, icons) belong in `Game/Content/UI/`.
- After adding new assets, update `Game/Content/Content.mgcb` with the appropriate `TextureImporter`/`TextureProcessor` entries so MonoGame builds them.

## Tools & Builds
- Run `dotnet tool restore --tool-manifest Game\.config\dotnet-tools.json` once after cloning to install the MonoGame content pipeline tools locally.
- Use `dotnet run --project Game/Rpg3D.Game.csproj` (now targeting `.NET 8`) to launch the prototype.

## Future Inputs
- When you have finalized palettes or texture dimensions, note them here so I can hard-code validators in the content pipeline.
- If you need additional manual setup (e.g., controller mapping JSON you prefer to hand-edit), list it here and I'll adjust the code around it.
\n## Runtime Tips\n- Press F5 in-game to cycle pixel render scales (0.5x, 0.6x, 0.75x, full res).
- Press F6 to swap between `Maps/intro.ascii` and `Maps/test_30X30.ascii`.\n- ASCII glyphs: use T for torches, E for enemy placeholders, ~ for shallow water.\n
