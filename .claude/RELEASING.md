# Releasing

The full process is in the GitLab wiki page "Contributing and releasing". `develop` and `main` are protected, so every merge goes through an MR. Release the SDK before any PlayPad release that needs its new API.

1. **Cut the branch:** `release/vX.Y.Z` from `develop` (`hotfix/vX.Y.Z` from `main`). Pick the version by semver, keeping in mind that before 1.0.0 a breaking change raises the minor version. Pushing the branch mirrors it to GitHub.
2. **Let CI bump:** `create-changelog` commits `chore: Bump version and add changelog for version X.Y.Z` as Elympics Bot, updating `package.json`, the `AssemblyInfo.cs` files and `CHANGELOG.md`. Pull it. Never create the version or the tag by hand.
3. **Fix the changelog:** commit edits as `chore: update changelog`. If nothing needs editing, push an empty `trigger pipeline` commit instead. Release branches can't be force-pushed, so fixes go on as new commits. Things the generator gets wrong:
   - `type!:` commits land in a group without a heading. Move them into Features, Bug Fixes or Code Refactoring.
   - BREAKING CHANGES notes keep commit trailers such as `Co-Authored-By`, and footers that later commits superseded. Rewrite the notes to describe the final state.
   - Removals made with `chore:`/`refactor:` commits that have no `BREAKING CHANGE` footer don't show up at all.
   - To preview the entry before pushing, run `npx conventional-changelog-cli@2.2.2 -p angular -i CHANGELOG.md -s -c .conventional-changelog-context.json -n .conventional-changelog-config.js` in a full-history clone with `package.json` set to the new version.
4. **Merge into `main`:** open an MR from the release branch to `main` and merge it with the Merge button, without squashing and without editing the message.
5. **Publish:** on the `main` pipeline, trigger the manual `release:gitlab` job. It creates the tag and the GitLab release from the `CHANGELOG.md` entry, extracted with `.scripts/ci/parse_changelog.js` and `changelog-parser@3`. The tag pipeline then runs `release:github` (needs `GH_TOKEN`/`GH_REPO`). If it fails because the tag hasn't reached GitHub yet, retry it.
6. **Merge back:** open an MR from `main` to `develop` and merge it.
