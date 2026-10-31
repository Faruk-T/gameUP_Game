# `feat/interaction` — reviewer guide

## Purpose and scope

This branch implements the MVP interaction loop for the fixed workstation: look around from the desk, inspect the active product, submit an approve/reject decision, and sort rejected packaging into a waste target. It also provides a standalone Unity test scene so the loop can be exercised without changing `SampleScene` or depending on the main game flow.

The interaction code delegates rule evaluation and result recording to `InspectionService`; it owns input, product presentation, and the timing of physical moves. This is a prototype/test integration, not a complete main-scene or final UI integration.

## Main code paths

| File | Responsibility |
| --- | --- |
| `Assets/Scripts/Interaction/WorkstationInteractionController.cs` | Mouse look and raycast targeting; inspect/restore product transform; wheel zoom; analysis, decision, and sorting input; world-input lock and status HUD. |
| `Assets/Scripts/Interaction/ProductPresentation.cs` | Spawns the current product (or a fallback cube), binds its world label, and moves it between arrival, desk, decision, and waste points. Emits `ArrivalFinished` after arrival. |
| `Assets/Scripts/Interaction/WorkstationTarget.cs` | Identifies raycast targets and maps waste-target actions to `WasteBinType`. |
| `Assets/Scripts/Interaction/InteractionTestBootstrap.cs` | Starts an in-memory sample run, advances through Day 1 test products, and provides test-only keyboard shortcuts. |
| `Assets/Scripts/Interaction/Editor/InteractionTestSceneBuilder.cs` | Unity menu command that creates the standalone scene, target labels, colors, camera, and serialized component references. |
| `Assets/Scripts/Interaction/Editor/InteractionSceneEditModeTests.cs` | Edit Mode scene wiring and required target presence test. |
| `Assets/Scripts/Interaction/INTERACTION_SETUP.md` | Short setup and control reference. |

The assembly definition references the shared Contracts and Rules assemblies. Keep ownership boundaries intact: interaction code sends decisions and completed physical actions to the rules service; it does not determine whether a product is valid or calculate score itself.

## Interaction flow

1. The test bootstrap starts a run and begins the next sample product.
2. `ProductPresentation` spawns it at the arrival point, binds its display labels, and moves it to the desk.
3. The player can inspect the product. Its starting position and rotation are saved on pickup and restored when returned to the desk. The mouse wheel moves it along the camera direction while it is inspected.
4. The player can request analysis or submit Approve/Reject. The controller asks `InspectionService` to process the request, then waits for the presentation movement callback before notifying completion.
5. Approve routes the product to the approved exit. Reject routes it to food disposal, then waits for a packaging target. Sorting completion is sent to `InspectionService` with the selected bin.
6. Once the outcome is committed, the test bootstrap allows `N` to begin the next sample product.

## Run the test scene

Open the project with the Unity version recorded in `ProjectSettings/ProjectVersion.txt`. In the Editor, use **GameUP32 → Interaction → Build Test Scene**. This generates `Assets/Scenes/Tests_Interaction.unity`; open that scene and press Play. The command uses Unity's current scene as its temporary build workspace, so save any unrelated open scene edits first.

The generated scene has colored, labeled analyzer, decision, and waste targets. Their text uses the `LabelText` material with depth testing so an inspected product occludes text behind its surface. The builder only writes the interaction test scene and its test materials; it does not replace `SampleScene`.

### Controls

| Input | Action |
| --- | --- |
| Mouse movement | Look around from the fixed workstation |
| `Esc` | Lock/unlock the cursor |
| Aim at product + left click, or `E` | Pick up / inspect the product |
| Hold left mouse button while inspecting | Rotate the product |
| Mouse wheel while inspecting | Move product closer/farther |
| Short left click on the inspected product, or `E` | Return product to its saved desk position and rotation |
| Aim at analyzer + left click, or `F1` | Request analysis |
| Aim at Approve/Reject + left click, or `F2`/`F3` | Submit a decision |
| Aim at a waste target + left click, or `1`–`4` | Sort packaging after Reject (paper, glass, plastic, contaminated) |
| `N` | Start the next sample after the current outcome completes |

The action keys are shortcuts in `InteractionTestBootstrap`, not a substitute for validating the in-world raycast controls.

## Verification

An Edit Mode NUnit test checks that the generated scene contains the wired runtime components, a camera, and every MVP target. Run it from Unity Test Runner under **EditMode** after generating the scene.

For a manual Play Mode check, verify:

- The first sample product arrives and can be picked up, rotated, zoomed, and returned without losing its original rotation.
- Target text is visible on the target face and is hidden where the foreground product occludes it.
- Analysis produces a report in the test status display.
- Approve completes the exit move; Reject requires a packaging target before the next product can start.
- The test hotkeys trigger the same actions as the world targets, and `N` only advances after completion.

## MVP boundaries and integration notes

- The test bootstrap uses generated in-memory Day 1 content and a fixed test run ID. It is not the production day-flow owner.
- The scene uses primitive graybox geometry and generated materials. Replace these through scene/prefab integration when final workstation assets are ready.
- Analysis text is shown in the test HUD. Production fax/document presentation is outside this branch's interaction responsibility.
- `SetWorldInputLocked` is the integration hook for a document UI to prevent camera and world actions while a document is open.
- The test bootstrap's `F1`–`F3` and `1`–`4` shortcuts are test conveniences; do not treat them as final player bindings.
- No main scene, product-rule logic, scoring logic, or production game-flow implementation should be inferred from the test scene alone.
