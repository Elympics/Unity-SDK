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

## What CI runs
- Build, test and verify jobs run only for code changes (`.code_paths` in `.gitlab-ci.yml`). Feature branches and a newly pushed release branch compare with `develop`; later pushes compare with the previous push. A newly pushed `hotfix/v*` branch has no previous push, so it runs them all. So a docs-only push to a branch whose last code push failed can still go green.
- The `main` pipeline of a release merge skips them (`detect-tested-release-merge`) when the release branch adds no code beyond its cut commit and a successful `develop` pipeline ran `test:editmode` on that commit. This relies on the unedited `Merge branch 'release/vX.Y.Z' into 'main'` message (step 4) and on `GITLAB_ACCESS_TOKEN`.
- Tag pipelines run only the release jobs.
- "Run pipeline" in GitLab runs everything. With `RELEASE_ONLY` set to `true`, it runs only the non-code jobs (version, changelog and release jobs).
- Code coverage is measured and reported only on `develop`, because it slows the tests.
