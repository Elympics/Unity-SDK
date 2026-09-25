# Shared agent memory (short)

Generated from `MEMORY.md`; don't edit by hand. Details, reasons, examples and time scopes: `MEMORY.md`.

## Code
- **jslib/jspre are ES2017:** no class fields, `?.` or `??`.
- **Cross-assembly use needs an asmdef reference:** add the source assembly GUID to the consumer `.asmdef` `references` (`InternalsVisibleTo` isn't enough). Grep factory call sites for `var`-inferred uses.
- **Create a handshake's completion source before connecting; build WebRtcConfig from Default:** set up the TCS/subscription before the call that triggers the reply. Use `WebRtcConfig.Default` + field assignments, never a bare initializer.
- **UITK Bind() discards earlier .value writes:** don't set bound fields in `CreateInspectorGUI`/`PrepareInspectorTree`. Write the serialized property or use `OnValidate`.

## Backend contracts
- **StreamingAssets upload contract:** versions are write-once. The Content-Type map (json/xml/octet-stream) is a verified allowlist. Keep the bundles → catalogs → `variants.meta.json` order. Don't validate baked catalog URLs.

## API and compatibility
- **Shipped [PublicAPI]: add a sibling (inactive until 1.0.0):** from 1.0.0, add a sibling method instead of refactoring a `[PublicAPI]` one.
- **Breaking changes are allowed before 1.0.0; ask before designing for compatibility:** no `[Obsolete]` cycles or migrations, but they must increase the minor version. Document in release notes. Ask before engineering compatibility.

## Verification
- **dotnet format is not a compile check:** never cite its exit code as proof that code compiles.
- **Compile and test evidence:** compile with the redirected `dotnet build`. Test with batchmode if a seat is free, `unity command` if the host has `com.unity.pipeline`, or else the open Editor plus `ScriptAssemblies` mtimes, `Editor.log` offsets and `TestResults.xml`.

## Git
- **Pre-edit baseline via git show, never git stash; snapshot unstaged work before editing:** use `git show :path` / `HEAD:path`. Copy files with unstaged changes before editing.
