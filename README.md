# The Floor Is Lava — Internal SDK / DX Notes

This repo packages the gameplay runtime as a cleanly separated module and exposes a small, stable interface surface for wrapper code. Wrapper developers can build surrounding product features (menus, level select, networking, settings) by calling the runtime interfaces, while the simulation and rendering can evolve independently.

The repo also includes a **custom level authoring workflow**: a lightweight **JSON level contract** (`CustomLevelDataModel`) plus a Unity Editor tool to **draw levels in a grid UI**, export JSON, and playtest by injecting the data into a runtime level (`PatternLevel_CustomFromData`). Because the format is plain JSON, wrapper-side code can also support **end-user level creation** (in-game editor, local save/load, sharing) by producing the same JSON and using the same injection path.

---

## Quick start

1. Open the sample scene:

    - `Assets/Scenes/Test Scene 16x9`

2. Press **Play**.

3. Use the sample wrapper (`GameBootstrapper`) to exercise the runtime interfaces:

    - In Hierarchy, select the GameObject with **`GameBootstrapper`**.
    - In Inspector:
        - Adjust **Demo Grid Size** (full-world grid) and **Demo Viewport Config** (viewport window).
        - Assign PatternLevel GameObjects into Levels to choose which level to play on.
        - Click the buttons:
            - `StartCountDown` / `StartLevel` / `StopLevel` (drives `IFullGridGameControl`)
            - `ResetHpToStarting` (example of wrapper UI update via `IViewportInterface.SetRoleUiText`)
            - `Restart` (reloads scene)

4. (Optional) Author and playtest a custom level:
    - Open `Tools -> Floor Is Lava -> Custom Level Creator`
    - Stay in Play Mode in the sample scene
    - Click **Playtest In Running Scene** (requires `CustomLevelPlaytestHarness` in the scene)

Notes:

-   Unity version can be checked in `ProjectSettings/ProjectVersion.txt`.
-   The sample uses a **16x9 viewport**; the world grid can be larger and viewed through one or more configured viewports.

---

## Project structure (separation of concerns)

**Wrapper (external / product layer)**

-   Owns product flow: menus, level selection, host-client/networking, settings, session lifecycle.
-   Chooses which level/data to run and when to start/stop.
-   Chooses how many viewports exist and how they are configured.
-   Talks to the runtime only through interfaces (no kernel internals).

**Kernel (simulation layer)**

-   Owns the full-world grid state and simulation rules.
-   Produces resolved cell states (role/color) over time.
-   Does not contain any UI/rendering logic.

**Viewport (presentation + input bridge)**

-   Renders a configured window into the world grid.
-   Does not encode gameplay rules; it displays whatever the kernel publishes for the visible region.
-   Bridges input back to the kernel via world-cell coordinates (e.g., touch → world index).

**Custom Level Data + Tooling (content authoring layer)**

-   A separate data model + editor workflow for defining levels without changing code.
-   Provides a JSON contract and tools to author/export/playtest custom levels against the runtime.

---

## Where to look (entry points)

-   **Wrapper-facing runtime control**
    -   `IFullGridGameControl` (interface)
    -   `FullGridGameController` (implementation)
-   **Wrapper-facing viewport control**
    -   `IViewportInterface` (interface)
    -   `LinkToViewportInterface` (scene adapter forwarding to a concrete viewport implementation)
-   **Custom level injection path**
    -   `CustomLevelDataModel` (data + JSON contract)
    -   `PatternLevel_CustomFromData` (runtime `PatternLevelSetupBase` driven by injected data)
    -   `CustomLevelPlaytestHarness` (sample “inject + run” helper for playtesting)
-   **Editor tooling**
    -   `CustomLevelCreatorWindow` (Tools menu window)

---

## Interfaces and workflow

### Sample scene: `Test Scene 16x9`

Under `Assets/Scenes/` there is a sample scene **`Test Scene 16x9`**. It is a reference “level scene” setup and includes the required runtime objects for running a level end-to-end (kernel + viewport wiring + a sample wrapper).

In that scene, there is a GameObject with `GameBootstrapper`.

`GameBootstrapper` is **sample wrapper code**: it demonstrates how a product-level manager can run game flow (countdown, HP, win/lose, level selection, etc.) while treating the gameplay runtime as a controlled subsystem.

The expectation is that other developers can build their own manager scripts; the intended API surface for that is the interfaces below.

---

### Full game control: `IFullGridGameControl`

`IFullGridGameControl` is the primary control surface exposed to wrapper code. It provides lifecycle control (configure → standby → start/stop) plus a small set of runtime queries and events.

**Implementation in this repo:** `FullGridGameController : MonoBehaviour, IFullGridGameControl`

#### Typical flow (wrapper side)

1. Configure the world grid size:
    - `ConfigureFullGrid(width, height)`
2. Choose which level to run:
    - `SetActiveLevel(level)`
3. Configure and enable viewport(s) (see next section)
4. Optionally display standby patterns (e.g., initial safe zone / preview):
    - `ShowStandby()`
5. Start simulation:
    - `StartLevel()`
6. Stop simulation when needed:
    - `StopLevel()`

#### API summary (what wrapper code should rely on)

-   **Lifecycle**

    -   `ConfigureFullGrid(int width, int height)`
    -   `SetActiveLevel(PatternLevelSetupBase level)`
    -   `ShowStandby()`
    -   `StartLevel()`
    -   `StopLevel()`
    -   `int GridWidth`, `int GridHeight`

-   **Runtime signals / queries**
    -   `event Action<Vector2Int, CellRole, CellColor> WorldCellTouched`
    -   `IReadOnlyCollection<Vector2Int> GetCurrentOverlappedCells()`
    -   `int GetWeaknessCellCount()`
    -   `void GetWeaknessCellIndices(List<Vector2Int> buffer)`

Notes:

-   `GameBootstrapper` is one example of how to implement product-level rules using this surface.
-   Wrapper code is expected to own its own state machine / UI / networking; the kernel is treated as a controlled runtime.

---

### Viewport control: `IViewportInterface` (via `LinkToViewportInterface`)

Viewport configuration and UI updates are done through `IViewportInterface`.

**In-scene adapter provided by this repo:** `LinkToViewportInterface : MonoBehaviour, IViewportInterface`  
It forwards calls to a concrete component assigned in `viewportInterfaceImpl` (which must implement `IViewportInterface`).

#### What wrapper code typically does

-   Configure viewport placement and orientation:
    -   `ConfigureViewport(worldOrigin, localOrigin, width, height, direction)`
-   Enable/disable viewport:
    -   `SetViewportEnabled(bool enabled)`
-   Set role-based UI text (example: show HP on safe cells):
    -   `SetRoleUiText(CellRole.Safe, "50")`

This keeps the wrapper interacting with a small, explicit viewport API rather than depending on a specific renderer/UI implementation.

---

## Custom levels (JSON contract + authoring tool)

Custom levels are defined by a simple data model (`CustomLevelDataModel`) that can be serialized to / from JSON. This enables level creation and iteration without writing new `PatternLevelSetupBase` subclasses.

### Data model: `CustomLevelDataModel`

At a high level, a custom level contains:

-   **Identity / metadata**

    -   `id` (string)
    -   `name` (string)

-   **Grid definition**

    -   `gridWidth` / `gridHeight`

-   **Timing**

    -   `defaultFrameCooldownSeconds` (default speed for the forbidden animation loop)

-   **Content**
    -   `SafePattern` (single frame; list of cells)
    -   `ForbiddenFrames` (multiple frames; each frame is a list of cells)
    -   `WeaknessCells` (list of cells)

Cells are expressed as `{ x, y }` in world grid coordinates.

### JSON format

Example JSON:

```json
{
	"id": "example_id_001",
	"name": "My Custom Level",
	"gridWidth": 16,
	"gridHeight": 9,
	"defaultFrameCooldownSeconds": 0.25,
	"SafePattern": {
		"cells": [
			{ "x": 0, "y": 0 },
			{ "x": 1, "y": 0 }
		]
	},
	"ForbiddenFrames": [
		{ "cells": [{ "x": 5, "y": 5 }] },
		{ "cells": [{ "x": 6, "y": 5 }] }
	],
	"WeaknessCells": [{ "x": 2, "y": 2 }]
}
```

### Running a custom level from JSON (`PatternLevel_CustomFromData`)

`PatternLevel_CustomFromData` is a `PatternLevelSetupBase` implementation driven by a `CustomLevelDataModel`.

Typical usage:

1. Parse JSON into a model:

    - `CustomLevelDataModel data = JsonUtility.FromJson<CustomLevelDataModel>(jsonText);`

2. Inject it into the level:

    - `patternLevel_CustomFromData.SetData(data);`

3. Run it through the normal wrapper workflow:
    - `IFullGridGameControl.ConfigureFullGrid(data.gridWidth, data.gridHeight)`
    - `IFullGridGameControl.SetActiveLevel(patternLevel_CustomFromData)`
    - `ShowStandby()` (optional)
    - `StartLevel()`

In the sample scene, this injection workflow is wrapped by `CustomLevelPlaytestHarness` for playtesting.

---

### Editor tool: Custom Level Creator

Menu:

-   `Tools -> Floor Is Lava -> Custom Level Creator`

This Unity EditorWindow supports:

-   Entering level metadata (name, grid size, default forbidden cooldown)
-   Drawing patterns via GUI grids:
    -   **Grid A**: Safe + Weakness authoring (mutually exclusive)
    -   **Grid B**: Forbidden frame authoring (Safe/Weakness shown as background)
-   Exporting custom levels as JSON:
    -   Copy JSON to clipboard
    -   Log JSON
    -   Create a `.json` file under `Assets/`

For playtesting, the window can inject the in-memory model into the running scene (Play Mode) through `CustomLevelPlaytestHarness`, without creating additional Unity assets.

---

## Contracts (what is intended to be stable)

**Stable integration surfaces**

-   `IFullGridGameControl`
-   `IViewportInterface`
-   `CustomLevelDataModel` JSON schema (level contract)

**Internal / implementation detail**

-   Kernel internals (pattern construction, resolver logic, specific pattern implementations)
-   Concrete viewport renderer/UI implementation behind `IViewportInterface`

---

## Troubleshooting (common “nothing renders” checklist)

If Play Mode runs but you see no grid output:

-   Confirm the viewport is configured and enabled (`SetViewportEnabled(true)` and a valid `ConfigureViewport(...)` was called).
-   Confirm the active level has content (e.g., `SafePattern` has at least some cells, or standby patterns are shown).
-   Confirm the grid size is configured before starting (`ConfigureFullGrid(...)` before `StartLevel()`).
-   If using Custom Level playtest: confirm `CustomLevelPlaytestHarness` has references assigned (`gameController`, `customLevel`, `linkToViewportInterface`).
