# Shared agent memory

Important, non-obvious knowledge about this package. Use this file as your memory for this package instead of your local/auto memory.

- Add an entry when you learn something important that the code, git history or CLAUDE.md doesn't already say. Update or remove an existing entry instead of duplicating it.
- Every entry has **Rule**, **Reason**, **Example** and **Time scope** (the condition under which to rewrite or remove it). Remove or rewrite entries whose time scope has passed.
- No personal or machine-specific data (user names, home-dir paths, local tool locations).
- Entries are grouped under `## Category` headings, each entry a `### Title`. Put a new entry under the category that fits, or add a new category if none does.
- After every edit, regenerate `MEMORY.short.md`: the same category headings, one bullet per entry with its title and a condensed Rule, no Reason/Example/Time scope. CLAUDE.md imports that file.

## Code

### jslib/jspre are ES2017
- **Rule:** no post-ES2017 syntax in `.jslib`/`.jspre` files: no class fields, `?.` or `??`.
- **Reason:** Unity 2021.3's Emscripten/Closure toolchain targets ES2017, so newer syntax breaks the WebGL build. `.scripts/ci/check_js.sh` enforces it in CI.
- **Example:** write `this.pc.sctp && this.pc.sctp.transport`, not `this.pc.sctp?.transport`.
- **Time scope:** If the minimum supported Unity version is above 2021.3, rewrite this.

### Cross-assembly use needs an asmdef reference
- **Rule:** when a file starts using another assembly's types, add the source assembly (usually by GUID) to the consuming `.asmdef` `references`. `InternalsVisibleTo` alone doesn't make a type resolvable. When sweeping for affected files, also grep factory/constructor call sites, because `var`-inferred types never appear as text.
- **Reason:** a present `InternalsVisibleTo` looks like enough on inspection, and a type used only through `var` is missed by a type-name grep. Both misses only surface as a Unity compile error.
- **Example:** `WebGL/WebRtcClient.cs` (`Elympics.WebRtc.WebGl`) started using `IDataChannel` and needed `Elympics.MatchConnection` (`86d779950baf55b4783b8ef7d9337d73`) in its references. `var webRtcClient = WebRtcFactory.CreateClient(WebRtcConfig.Default);` in `Tests/Runtime/UnityConnectors/HalfRemote/HalfRemoteGameEngineServerTests.cs` was missed by a type-name grep. Other GUIDs: `Elympics.WebRtc.Common` `e2a0cb92652b07d409c674525eba884a`, `Elympics.WebRtc.Other` `598d50f0b99ecc342bb6bff69c950dbd`, `Elympics.WebRtc.WebGl` `884c905e7a86c7444ad05287e5b8b669`, `Elympics.WebRtc` `859bd52dcb739c84bb2b971eecc6b4e4`. `Tests/Runtime/Elympics.Tests.asmdef` uses assembly names instead of GUIDs.
- **Time scope:** If the asmdef layout changes (assemblies merged or split, GUIDs changed), update or remove this.

### Create a handshake's completion source before connecting; build WebRtcConfig from Default
- **Rule:** a completion source or subscription that catches an async server reply must exist before the call that can trigger that reply. Build `WebRtcConfig` as `var c = WebRtcConfig.Default; c.X = …;`, never as a bare `new WebRtcConfig { … }`.
- **Reason:** `GameServerClient` completes the handshake with `_sessionConnectedTcs?.TrySetResult(message)`, so a `ConnectedMessage` that arrives before the TCS exists is dropped silently. The client then waits out `SessionConnectTimeout`, and this only shows up against a real server. A bare initializer leaves `IceServers` null, so `Other/WebRtcClient` throws `ArgumentNullException`. A refactor can revive a code path that never actually ran, so don't assume that path is correct just because it predates your change.
- **Example:** splitting "connect the transport" from "wait for Connected" left TCS creation in the second step, and the server's immediate reply raced it.
- **Time scope:** If `GameServerClient` stops waiting for `ConnectedMessage` through a completion source, drop the first part. If `WebRtcConfig` gets non-null field defaults, drop the second.

### UITK Bind() discards earlier .value writes
- **Rule:** don't set a bound field's `.value` during `CreateInspectorGUI`/`PrepareInspectorTree`. Write the serialized property instead, or derive the value in `ElympicsGameConfig.OnValidate` or a computed property.
- **Reason:** binding runs after tree construction (`ManageGamesInElympicsWindow` calls `inspector.Bind(...)`, and Unity's inspector path does the same) and overwrites the write with no error. Writing after the bind instead dirties the asset just because a window was opened. Unbound fields set with `SetValueWithoutNotify` aren't affected.
- **Example:** re-deriving `scenePath` next to the initial `Update*()` calls in `ElympicsGameConfigEditor` compiled, looked right, and did nothing.
- **Time scope:** If `ElympicsGameConfigEditor` stops using UI Toolkit binding, remove this.

## Backend contracts

### StreamingAssets upload contract
- **Rule:** treat the per-extension Content-Type map as a verified allowlist: `.json` → `application/json`, `.xml` → `application/xml`, everything else `application/octet-stream`. Don't add an extension without a real upload confirming it. Keep the upload order bundles → catalogs → `variants.meta.json`. Don't validate catalog URLs baked into builds.
- **Reason:** a version is write-once (re-initializing returns 400, and there's no resume), so any failed PUT uses up that version number. The upload order makes a partial version fail closed. The signed URL covers `content-type`, so a mismatch surfaces as GCS `SignatureDoesNotMatch`, not as a content-type error. The game runner overwrites the baked URLs at runtime. Bucket layout: `{bucket}/{gameId}/sa/{version}/{variant}/aa/…`.
- **Example:** sending `text/plain` for `.hash` failed with `SignatureDoesNotMatch` and used up that version number.
- **Time scope:** If the backend `/client-builds/streaming-assets` contract changes (resumable uploads, `Content-Type` no longer signed), rewrite this.

## API and compatibility

### Shipped [PublicAPI]: add a sibling (inactive until 1.0.0)
- **Rule:** from 1.0.0 on, don't refactor or change a `[PublicAPI]` method to share code with a new one. Add a sibling method, accept the duplication, and prove the original is untouched (`git diff --stat` shows insertions only). Below 1.0.0, refactoring is allowed.
- **Reason:** these methods are the documented surface games call, for example via `-executeMethod`, and nothing in this repo tests that path.
- **Example:** a new entry point that needs most of `ElympicsWebIntegration.UploadStreamingAssetsInBatchmode` gets its own method next to it.
- **Time scope:** Inactive below 1.0.0. At 1.0.0 or above, remove the "inactive" note.

### Breaking changes are allowed before 1.0.0; ask before designing for compatibility
- **Rule:** removing or renaming public API or serialized members doesn't need an `[Obsolete]` cycle or a migration. It must increase the minor version number, never only the patch (for example `0.27.x` → `0.28.0`). Document the change in the release notes. When a compatibility hazard comes up, ask whether compatibility is required before engineering for it.
- **Reason:** the SDK is pre-1.0, and releases on this track are breaking anyway. Below 1.0.0, the minor number is what signals a breaking change to consumers, so a breaking patch release would reach games that only expect fixes.
- **Example:** `Runtime/Communication/Respect` was deleted without deprecation. Removing a serialized enum member renumbers the values in existing `ElympicsGameConfig.asset` files. The decision was a release-note reminder, not pinned enum values plus an `OnValidate` migration.
- **Time scope:** If the version is 1.0.0 or above, remove this.

## Verification

### dotnet format is not a compile check
- **Rule:** never cite a `dotnet format` exit code as proof that code compiles.
- **Reason:** it checks style only.
- **Example:** it exited 0 on a `WebGLUploader.cs` that the compiler rejected with six errors (CS0201, CS0019, CS0029).
- **Time scope:** If the CI style check stops using `dotnet format`, remove this.

### Compile and test evidence
- **Rule:** compile with the redirected `dotnet build` from the host project dir (see CLAUDE.md), never a plain one. For tests:
  - If a Unity license seat is free, use batchmode `-runTests`.
  - If the host project has `com.unity.pipeline`, drive the running Editor with the Unity CLI (`unity command`).
  - Otherwise, ask the user to run Test Runner in the open Editor, and read the evidence yourself:
    - `<host>/Library/ScriptAssemblies/Elympics.Editor{,.Tests}.dll`: the mtime advances only on a successful compile.
    - `Editor.log` (Windows: `%LOCALAPPDATA%/Unity/Editor/Editor.log`) accumulates across sessions and Editor instances. Compare the byte offset of a match against the file size before trusting it.
    - `TestResults.xml` is written to `<LocalLow>/<companyName>/<productName>/` (Windows: `%USERPROFILE%/AppData/LocalLow/…`), using the names in the host's `ProjectSettings/ProjectSettings.asset`. Parse `total`/`passed`/`failed`/`result` on `<test-run>` and `result` on each `<test-case>`.
- **Reason:** a plain `dotnet build` writes into the `obj/`/`bin/` dirs Unity owns. Batchmode dies without a free license seat, and seats are usually held by the open Editor. It would also hit the `Library` lock.
- **Example:** `-runTests` fails with `BatchMode: Unity has not been activated with a valid License` before compiling anything.
- **Time scope:** If `com.unity.pipeline` becomes a dependency of this package, rewrite the test part.

## Git

### Pre-edit baseline via git show, never git stash; snapshot unstaged work before editing
- **Rule:** get old file content with `git show :path` (index) or `git show HEAD:path` (commit) into a scratch dir. Before editing a file that has unstaged changes, copy it.
- **Reason:** the working tree is often fully staged with unstaged edits on top. `git stash push --keep-index` followed by `git stash pop` then conflicts and writes conflict markers into sources. Unstaged work has no git copy, so `git checkout -- <file>` destroys it along with your edit.
- **Example:** a stash pop left `UU` conflicts in two edited files. Recovery was `git checkout stash@{0} -- <paths>` for the working tree, then `git reset stash@{0}^2 -- <paths>` for the index.
