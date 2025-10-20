# Lessons Learned

Document recurring issues and their fixes so future late nights stay shorter. Capture what happened, how we diagnosed it, the resolution, and any preventive guardrails. Treat this as a living log you update after each “why is everything black?” moment.

## When Black Screens Happen

| Date | Symptom | Cause | Fix | Preventative |
| ---- | ------- | ----- | --- | ------------ |
| 2025-10-19 | Launch window draws only black, HUD still visible | Renderer expected `VertexPositionNormalColorTexture` while mesh builder emitted `VertexPositionColorTexture`, so the shader read garbage | Rebuilt `GridMeshBuilder` to emit `VertexPositionNormalTexture` and updated `GridRenderer` to create buffers with the same type; re-enabled `BasicEffect` lighting | Keep mesh vertex type and renderer vertex type in sync; add unit test or editor check |
| 2025-10-19 | MonoGame reported `Content\Maps\intro.ascii` missing even though the file was copied | Load path used `File.Exists` instead of MonoGame’s `TitleContainer`, so content roots didn’t match | Switched `LoadMap` to `TitleContainer.OpenStream("Content/Maps/..." )` and added logging | Always load runtime assets through the content pipeline APIs; log missing assets immediately |

## Debug Checklist

1. **Check logs** in `Game/bin/Debug/net8.0/logs/run_*.log`.
   - Are required textures loading?
   - Did the map parse (mesh parts > 0, torch count reasonable)?
2. **Verify vertex format**: mesh builder and renderer must agree on `VertexPositionNormalTexture`.
3. **Draw order**: opaque world first, transparent billboards second.
4. **Camera sanity**: log the spawn position; drop a debug cube or wireframe if needed.

## Preventive Guardrails

- Add an automated test or editor script that asserts the vertex struct used by `GridMeshBuilder` matches the type used by `GridRenderer`.
- Throw or log immediately when `TitleContainer.OpenStream` fails so content issues don’t silently render black.
- Keep this document updated after each firefight so the next time is faster.
