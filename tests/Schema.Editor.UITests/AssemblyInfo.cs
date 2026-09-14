// Copyright (c) 2023-2026 ktsu-dev contributors

// One ImGui context exists at a time, and ImGuiAppHarness refuses to start a second while one is
// running, so these tests cannot run concurrently with each other the way the library's can.
[assembly: DoNotParallelize]
