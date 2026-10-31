# Interaction integration

The interaction branch is merged into `feat/lead-day-flow`. The production setup uses **First → Menu → Game**, a serialized product pool and the shared day flow. Test bootstraps, fallback cubes, OnGUI and the second InspectionService loop have been removed.

See [LEAD_INTEGRATION.md](LEAD_INTEGRATION.md) for the current architecture, controls, scene authoring, build and verification commands. The interaction source and its authored target validation remain under `Assets/Scripts/Interaction`.
