# plan.md — Retro 3D RPG (UU/Daggerfall/Delver‑style)

## 0) Project Summary
**Codename:** Underlight (placeholder)  
**Genre:** First‑person retro 3D RPG with sprite enemies/items, outdoor + dungeon exploration  
**Style:** Chunky Low‑poly world, low‑res pixel textures, billboarded sprites (Ultima Underworld / Daggerfall / Delver)  
**Primary Tech:** C# + MonoGame (proprietary engine + map editor).  
**Targets:** PC (Win), VR (OpenXR/OpenVR), later Steam Deck (Linux/Proton) and Mobile,.  
**AI Dev Tools:** OpenAI Codex (primary), Claude Sonnet, Gemini Pro (large-context review).

Non‑Goals (v1): multiplayer, complex physics, high‑fidelity PBR, full story systems.

---

## 1) Pillars
1. **Retro authenticity**: crisp pixels (point sampling), limited palettes, simple lighting.
2. **Ease of dev**: simple data formats, small modular code units, AI-friendly tasking.
3. **Fast iteration**: in‑engine map editor + hot‑reload for content.
4. **Good feel**: smooth FPS movement, snappy combat, readable feedback.
5. **VR‑prepared**: engine layout anticipates stereo rendering & head tracking.

---

## 2) High-Level Roadmap (Milestones & Acceptance)
**M0 – Bootstrap **
- ✅ Builds run on Win; project skeleton + Content pipeline.
- ✅ Point sampling in place; windowed/fullscreen toggle; fixed timestep with interpolation.

**M1 – Core 3D & FPS Loop **
- ✅ Render room: floor + wall meshes with 64×64 tile textures.
- ✅ Free‑look camera (yaw/pitch), WASD, collision vs. walls.
- ✅ Sprite billboard entity (idle) with depth‑sorted draw.
- ✅ Health/damage loop: player → enemy, enemy → player.

**M2 – Data & Editor **
- ✅ Level JSON + ASCII tile format with tileset bindings.
- ✅ In‑game editor mode (paint walls/floors, place entities, save/load).
- ✅ Simple undo/redo.

**M3 – Content & Systems **
- ✅ 4 enemy types (idle/walk/attack/death), loot table, items (weapon, potion).
- ✅ Inventory UI (basic), pickup/use, drop.
- ✅ 2 dungeon themes + 1 small outdoor area (skybox), day/night toggle (cheap).

**M4 – Packaging & Deck **
- ✅ Gamepad support; Linux build via DesktopGL; Proton/Deck sanity pass.
- ✅ Performance floor: 120+ FPS @1080p PC; 60+ Deck.

**M5 – VR Experimental **
- ✅ Engine stereo path (two view/proj, shared scene graph).
- ✅ Head pose integration via IVRBackend (OpenXR/OpenVR implementation later).
- ✅ Seated VR mode: head‑look + gamepad locomotion; frame pacing stable 72–90Hz on PC VR.

---

## 3) Architecture

### 3.1 Project Structure
```

/Engine
/Core           (Game loop, timing, services, DI container)
/Platform       (Window, input, file, platform checks)
/Graphics       (Renderer, materials, shaders, billboards, skybox)
/Content        (Loaders: textures, spritesheets, meshes, audio; mgcb helpers)
/World          (Level data, tilemap3D, sectors/cells, portals)
/Entities       (ECS‑lite: Entity, Components, Systems)
/Physics        (AABB collisions, raycasts)
/AI             (State machines, LOS, simple pathing)
/UI             (Immediate UI; debug overlay; inventory/HUD)
/Audio          (SFX, music, positional audio)
/VR             (Interfaces; stereo camera rig; backend plug)
/Tools          (In‑game editor; exporters; validators)
/Game
/Data           (JSON: tilesets, entities, items, levels, loot)
/Assets         (Sprites/Textures/Audio)
/Editor           (Entry point for Editor build, if split)
/ThirdParty       (ImGui.NET, optional)

````

### 3.2 Key Systems
- **Renderer**  
  - Forward renderer, **point sampling** only.  
  - Materials: `DiffuseTex`, tint color, `NoFilter`.  
  - Billboard modes: `CameraFacingFull` (default), `AxisYOnly` (for VR comfort).  
  - Render order: skybox → world geometry → billboards (sorted back‑to‑front) → particles → UI.
- **World/Tilemap3D**  
  - Grid cell = `{ floorId, ceilId, wallN/E/S/W, props[] }`.  
  - Mesh bake per chunk (e.g., 16×16 cells) for low draw calls.  
  - Outdoor: large ground plane tiles + prop placement; skybox.
- **ECS‑lite**  
  - Components: `Transform`, `SpriteBillboard`, `Character`, `Health`, `Inventory`, `AiBrain`, `LootDrop`, `Light` (optional), `Interactable`.  
  - Systems: `MotionSystem`, `CombatSystem`, `BillboardSystem`, `AISystem`, `LootSystem`, `UISystem`.
- **Physics/Collision**  
  - Grid Based: query solid walls per move; slide along normal.  
  - Raycast for melee/hitscan.  
- **Content Pipeline**  
  - JSON descriptors: spritesheets, tilesets, items; raw PNG/WAV.  
  - Build step validates power‑of‑two & palette conformance.
- **Input**  
  - KB/Mouse + Gamepad abstraction; binds in JSON.
- **VR**  
  - `IVRBackend`: `Init()`, `Poll() -> HeadPose`, `GetEyeViews()`, `Submit(RenderTarget left,right)`.  
  - Stub backend (no VR) vs OpenXR/OpenVR backend later.

---

## 4) Data & File Formats

### 4.1 Level (JSON)
```json
{
  "name": "dungeon_01",
  "size": [64, 64, 1],
  "theme": "crypt",
  "tileset": "tiles_crypt_v1",
  "cells": "levels/dungeon_01.ascii",
  "entities": [
    {"id":"player_start","pos":[4,1,4]},
    {"id":"skel_melee","pos":[10,1,12],"rot":180},
    {"id":"potion_small","pos":[6,1,5]}
  ]
}
````

### 4.2 ASCII Cells (Editor can roundtrip)

```
# = solid wall, . = floor, ^ = stair up, v = stair down, ~ = water
############
#..E....^..#
#..........#
#..P.......#
############
```

### 4.3 Spritesheet (JSON)

```json
{
  "name": "skel_melee",
  "frameSize": [64, 64],
  "directions": 8,
  "animations": {
    "idle":   {"frames":[0,1,2,3], "fps":6,  "loop":true},
    "walk":   {"frames":[8,9,10,11,12,13], "fps":8, "loop":true},
    "attack": {"frames":[16,17,18,19], "fps":8, "loop":false},
    "death":  {"frames":[24,25,26,27], "fps":8, "loop":false}
  },
  "billboardMode": "AxisYOnly",
  "origin": [32, 56]
}
```

### 4.4 Tileset (JSON)

```json
{
  "name": "tiles_crypt_v1",
  "tileSize": 64,
  "palette": "DB32",
  "textures": {
    "wall_stone_a": "tex/wall_stone_a.png",
    "floor_flag_a": "tex/floor_flag_a.png",
    "ceil_rock_a":  "tex/ceil_rock_a.png"
  },
  "variants": { "wall_stone_a": ["wall_stone_a","wall_stone_b"] }
}
```

---

## 5) Editor Spec (in‑game)

* Toggle: **F1** editor mode.
* Views: top‑down grid + first‑person fly camera.
* Tools: **Paint Wall/Floor**, **Place Entity**, **Select/Move**, **Eyedropper**, **Erase**.
* Panel: Tileset picker, Entity palette, Properties.
* File: New/Load/Save As (JSON + ASCII).
* Undo/Redo: last 100 ops.
* Hot‑reload assets on save.
* Optional: ImGui.NET overlay for UI widgets.

Acceptance:

* Paint 32×32 area < 100ms apply.
* Save/load preserves exact placement (bit‑identical JSON).
* Editor state persists recent files, tileset, last tool.

---

## 6) Rendering & Shaders

* **Sampling:** Point clamp everywhere; mipmaps off (or nearest).
* **Billboards:** Vertex shader or CPU‑oriented quads; `AxisYOnly` for VR comfort.
* **Lighting (v1):** Vertex lit (1–2 dir lights) or baked tint; optional unlit for pure retro.
* **Skybox:** Cubemap or 6‑sided textures, drawn first with depth disabled.
* **Low‑res render option:** Render to 480p/720p `RenderTarget2D` → upscale with nearest neighbor.

---

## 7) Gameplay v1

* **Player:** health, stamina (optional), melee weapon, potion.
* **Enemies (min 4):** skeleton melee, bat (fast, low HP), slime (slow, splits?), archer (ranged).
* **Items:** sword tiers, bow, arrows, small/large health, keys.
* **AI:** LOS cone, chase within radius, simple cooldown attacks.
* **Progression:** 2–3 dungeon floors + surface path; exit artifact.

---

## 8) Input

* **KB/Mouse:** WASD, Space (use), E (interact), LeftClick (attack), Tab (inventory).
* **Gamepad:** Left stick move, Right stick look, A use, RT attack, Start inventory.
* **Bind Config:** `config/input.json` (rebinding later).

---

## 9) Audio

* WAV/OGG SFX; looped music (XM/OGG).
* Positional audio for enemies; UI clicks non‑positional.
* Mix volumes config.

---

## 10) Performance Budgets (PC/Deck)

* Draw calls/frame ≤ 1,000 (typical ≤ 400).
* Sprites on screen ≤ 200.
* Texture memory ≤ 256 MB.
* GC alloc/frame ~ 0 KB (steady‑state).
* VR (PC): 72–90Hz with low‑res RT scale if needed.

---

## 11) VR Plan

* **Engine hooks now:** dual eye cameras, shared scene graph, `IVRBackend`.
* **Seated mode first:** head orientation drives camera; stick locomotion; snap turn optional.
* **Comfort:** Axis‑Y billboards, vignette on sprint, world‑locked UI optionally.
* **Backends:** `VrBackendStub` (default), `VrBackendOpenXR` (later).
* **Testing:** add `r_vr_preview` mono‑view emulation in desktop for dev.

---

## 12) Build, CI, Packaging

* **Content:** mgcb tracked; `Content.mgcb` build step.
* **CI:** GitHub Actions – build Win/Linux; artifact zip with `Underlight.exe` + `Content/`.
* **Steam/Deck:** Proton run, add controller layout.
* **Config:** `config/*.json` overrides next to exe.

---

## 13) Coding Standards (AI‑friendly)

* One class per file; ≤ 300 LOC per class where possible.
* Public APIs documented with XML comments (purpose/params/returns).
* Deterministic updates: `Update(dt)`, pure as possible; minimal static state.
* Guard rails: argument checks, `Debug.Assert` on invariants.
* Log: `Log.Info/Warn/Error` with subsystem tags.

---

## 14) AI‑Coder Workstyle

**Task Card Template**

```
Title: Implement BillboardSystem
Context: Renders SpriteBillboard comps with AxisYOnly facing.
Inputs: Transform, SpriteBillboard, Camera.
Outputs: Drawn quads with correct UV per frame and dir.
Steps:
  1) Compute camera forward projected onto XZ to get facing yaw.
  2) Build quad vertices around Transform with origin pivot.
  3) Select frame from animation time; set UVs.
  4) Submit to sprite batch / custom effect.
Acceptance:
  - Sprite faces camera yaw within ±1°.
  - Animation plays at fps set in JSON (±1 frame over 10s).
  - Origin pivot aligns feet to floor (no foot sliding).
```

**Prompting Tips for Codex**

* Provide **file path**, **method signature**, **pseudocode**, **acceptance tests**.
* Prefer “modify‑in‑place” diffs: *“Edit Engine/Graphics/BillboardSystem.cs: implement Update() per spec …”*
* Ask for **unit test** or **debug overlay** when relevant.

---

## 15) Initial Backlog (Executable)

* [ ] Bootstrap MonoGame project (DesktopGL), content pipeline, point sampling.
* [ ] CameraController (mouse‑look, clamp pitch, sensitivity config).
* [ ] Grid world + mesh bake for walls/floors (chunked).
* [ ] Collision vs grid (sweep test; slide).
* [ ] Sprite system (spritesheet JSON, animator, billboard draw).
* [ ] Combat: ray melee (range 1.5m), damage, hit feedback.
* [ ] Enemy: Skeleton melee (idle/walk/attack/death).
* [ ] Items: potion (use to heal), sword pickup.
* [ ] Editor mode: paint walls/floors; place enemy/item; save/load JSON+ASCII.
* [ ] Inventory panel (simple; gridless list).
* [ ] Outdoor map: ground tiles + trees (sprite props), skybox.
* [ ] Gamepad support (Windows).
* [ ] Linux build/Deck test path.
* [ ] Stereo camera rig abstraction (stubbed VR backend).

---

## 16) Risks & Mitigations

* **VR complexity:** start with seated mode + `AxisYOnly` billboards; lower RT scale; abstract backend early.
* **Asset consistency:** enforce palette/size validators in build step; use promptbase.
* **Scope creep:** lock v1 feature set; extras go to v1.x.

---

## 17) Visual Fidelity Roadmap (Textures, Lighting, FX, UI)

### 17.1 Textures & Materials
1. Define `Content/Textures/` + metadata JSON (albedo/tint/palette tags).
2. Implement texture atlas loader + point-sampled materials in renderer.
3. Add material bindings to grid mesh bake (floor/ceiling/wall IDs).
4. Introduce basic texture animation support (water/lava cycling).

### 17.2 Lighting & Shading
1. Extend renderer with per-vertex tint + directional light uniforms.
2. Add light component (color/intensity/range) and light culling pass.
3. Investigate simple baked lightmaps or LUT-based ambient ramps.
4. Provide debug overlays for light volumes & brightness heatmaps.

### 17.3 Particle & FX System
1. Create `Content/Particles/` for sprite sheets + JSON emitters.
2. Implement CPU particle pool with billboarding + depth sort hook.
3. Expose scripting hooks for burst/loop triggers (combat hits, environment).
4. Add editor tools for emitter preview/tuning.

### 17.4 Sprite & Object System
1. Populate `Content/Sprites/` for characters/items/projectiles.
2. Build sprite animation controller (state graph, FPS, directional frames).
3. Integrate with ECS-lite (`SpriteBillboard`, `AnimatedSprite` components).
4. Establish object templates (loot, props) with JSON descriptors + factory.

### 17.5 UI & HUD
1. Set up `Content/UI/` for fonts, panel textures, icon atlases.
2. Prototype immediate-mode HUD (health/stamina/weapons).
3. Add inventory & interaction panels (keyboard/gamepad navigation).
4. Style pass: pixel fonts, window frames, palette swaps for alerts.

---

```
