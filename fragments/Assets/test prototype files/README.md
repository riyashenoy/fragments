# FOUND — Unity / Meta Quest tool framework

A drop-in C# framework that ports the **tool layer** of the FOUND web prototype to
mixed reality on Quest 3 / 3S. It gives you the "notice → pinch → collect → transform
→ place → remember" loop: a tool state machine, a shared pinch-selection capture
gesture, a passthrough-pixel → scrapbook-material pipeline, and the memory recipe.

It is intentionally **SDK-agnostic**: every Meta-specific call is quarantined in one
file (`Meta/MetaPassthroughSource.cs.txt`) so the framework compiles and *runs in the
editor* on a blank project before you install anything.

---

## What's here

```
Scripts/
  Core/
    ITool.cs                  Tool interface + EnvironmentSelection data type
    ToolManager.cs            Active-tool state machine (the web setTool())
    FoundEvents.cs            Tiny event bus (toasts, fragment + recipe events)
  Capture/
    IPinchProvider.cs         Abstraction over "the two corners being framed"
    ControllerPinchProvider   Editor/controller driver (swap for hands later)
    IPassthroughSource.cs     The ONE seam to passthrough pixels
    EditorPassthroughSource   Fake café source → full pipeline with no headset
    PassthroughCapture.cs     World frame → cropped Texture2D (GPU→CPU readback)
    PinchSelection.cs         Live frame quad + release → capture → dispatch
    ScenePlaceLabeller.cs     "the wooden table" style provenance (MRUK hook)
  Tools/
    ColorSamplerTool.cs       Average + name a colour, apply to page
    WashiTapeTool.cs          Framed pattern → repeating tape strip
    CameraScrapTool.cs        Polaroid / borderless / torn photo
    StickerTool.cs            Segmenter path OR torn-scrap fallback + peel anim
    DirectTools.cs            Move / Pen / Eraser + small UI collaborator stubs
  Scrap/
    ScrapFragment.cs          A placed element: grab / resize / rotate / layer
    FragmentProvenance.cs     Where it came from (the point of FOUND)
    FragmentFactory.cs        Spawns tape / photo / sticker / label fragments
    TextureBaker.cs           Runtime torn ends, borders, colour naming
  Journal/
    MemoryRecipe.cs           The 5-item gentle objective + completion event
  Meta/
    MetaPassthroughSource.cs.txt   Rename → .cs once the SDK is installed
```

---

## Project setup (blank project → running)

1. **Unity 6.x**, create a 3D (URP recommended) project.
2. Copy `Scripts/` into `Assets/`. It compiles immediately — no SDK needed yet.
3. **Editor bring-up (no headset):**
   - Make a `ToolManager` GameObject; add `ToolManager` + all seven tool components.
   - Add `PassthroughCapture`, `PinchSelection`, `ControllerPinchProvider`,
     `FragmentFactory`, `MemoryRecipe`.
   - Add an `EditorPassthroughSource`; point it at a second Camera rendering any mock
     café scene (or a static texture). Wire it into `PassthroughCapture.passthroughSourceBehaviour`.
   - Make 4 tiny prefabs (a Quad + `ScrapFragment` + a collider) for tape/photo/sticker/label
     and assign them on `FragmentFactory`.
   - Press Play, pick the tape tool, drag on the mock café → a tape fragment spawns.
     The entire loop works in-editor. Iterate on tools here.

4. **On-device (real passthrough):**
   - Install **Meta XR Core SDK** and **Meta MR Utility Kit (MRUK) v81+** (UPM, or via the
     Meta Quest build profile in Unity 6.3+). Enable the Meta XR feature set.
   - Switch platform to Android; set up an `OVRCameraRig` (or the Camera Rig building block)
     with **Passthrough Support = Supported** and passthrough enabled (Underlay).
   - Rename `Meta/MetaPassthroughSource.cs.txt` → `.cs`, add scripting define
     `FOUND_META_SDK` (Project Settings → Player → Scripting Define Symbols).
   - Add the camera permission to `AndroidManifest.xml`:
     ```xml
     <uses-permission android:name="horizonos.permission.HEADSET_CAMERA" />
     ```
     and request it at runtime (OVRPermissionsRequester or Unity's Permission API) before
     first capture.
   - Replace `EditorPassthroughSource` with `MetaPassthroughSource` in the capture wiring.
   - Swap `ControllerPinchProvider` for a hand-pinch provider (two `OVRHand` index-thumb
     pinches → CornerA / CornerB) for the true "pinch a fragment out of the air" feel.

---

## The one real cost to respect

Turning real pixels into materials needs a **GPU→CPU readback** of the framed region
(`PassthroughCapture.ReadbackRegion`). It happens once per capture (not per frame) and
is already downscaled to `maxCropSize`. Keep captures event-driven — never poll pixels
every frame — and this stays comfortably within frame budget.

## Honest caveats vs. the web demo

- **Passthrough camera access is Quest 3 / 3S only**, Horizon OS v81+, and requires the
  user to grant `HEADSET_CAMERA`. Plan a graceful "camera permission needed" state.
- **Sticker segmentation** isn't free like the web demo's predefined masks. Use the PCA
  `MultiObjectDetection` (Unity Sentis) sample as your `ISegmenter`, or ship the
  torn-scrap fallback (already wired) and add segmentation later.
- **Camera-world alignment**: `WorldToCameraUV` in the Meta adapter should use the SDK's
  intrinsics helper for accuracy; the co-located-camera fallback is fine for bring-up but
  will drift at the edges. Confirm the exact helper name in your installed MRUK version.
- **Text** (polaroid captions, handwritten labels): render with TextMeshPro children on
  the prefabs rather than baking glyphs into textures — crisper in XR.

## Recommended reading

- Passthrough Camera API overview & samples (CameraViewer, CameraToWorld,
  MultiObjectDetection) — the CameraToWorld sample is the reference for `WorldToCameraUV`.
- MRUK "Getting Started" for scene anchors that power `ScenePlaceLabeller`.
- Meta Interaction SDK `HandGrab` for grabbing `ScrapFragment`s naturally.

---

## 3D Journal (Scripts/Journal3D)

A tactile, dimensional book with a shell material on the cover/spine and a separate
paper material on the pages, real page-turn animation with a mid-turn curl, and
per-face decoration surfaces that scraps parent to.

**Build it in ~30 seconds:**
1. Empty GameObject → add `JournalBuilder`.
2. Assign a **Shell** material (cover/spine) and a **Page** material (paper). URP/Lit
   is fine; leave blank and it generates sensible defaults.
3. Right-click the component ▸ **Build Journal**. It creates the spine, back cover,
   front cover, and N pages and wires a `Journal` component automatically.
4. Press Play. In the editor, click the cover to open (`JournalPokeOpen`) and drag a
   page's free edge to flip (`PageCornerHandle`) — both work with the mouse, no headset.

**Navigation from code / UI / XR:** `Journal.Open()`, `Close()`, `Next()`, `Prev()`.
Subscribe `onSpreadChanged(int)` and `onActiveSurfaceChanged(Transform)`.

**Materials at runtime:** `Journal.SetShellMaterial(m)` / `SetPageMaterial(m)`, or
per-face `JournalPage.SetMaterials(front, back)` if you want a different back.

**Hooking to the tools:** add `JournalFragmentBridge` (needs the tool framework) and it
keeps `FragmentFactory.placementParent` pointed at the face-up page, so captured tape /
stickers / photos land on the current page and turn with it.

**On Quest:** give the front cover collider a poke interactor (Interaction SDK) → `Poke()`,
and drive `PageCornerHandle.BeginDrag/UpdateDrag/EndDrag` from a hand-grab interactor at
the page's free edge. Nothing else changes. Curl recomputes one page's ~200 verts only
while it's turning, so it's cheap; if you push page subdivisions very high, cache normals.
