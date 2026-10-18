# CLAUDE.md

This file is a guide for coding agents working in 'NINA.Plugin.Web.sln'.

## Scope

This guide covers all projects listed in 'NINA.Plugin.Web.sln'.

## Implicit Command Approvals

Proceed without asking for confirmation before running any read-only operation — shell commands, web fetches, file reads, build verification. Ask first for anything that writes to disk, modifies remote state, or could be destructive. If the intent is ambiguous, ask.

## Overview

NINA.Plugin.Web (aka 'Web Session History Viewer', aka 'WSHV') is a plugin for the NINA astrophotography application.
It embeds a lightweight web server that serves a bundled web app, letting users view details of the current ('live') and past acquisition sessions — event timeline, saved images, image quality metrics, autofocus curves — from any browser on the local network.
- It is written in C# exclusively for Windows
- It uses Windows Presentation Foundation (WPF) for the plugin options UI
- It uses EmbedIO for the embedded HTTP server
- Session data is persisted as JSON files (Newtonsoft.Json); there is no database

## Solution Structure

The solution contains two projects:
- **NINA.Plugin.Web** — main plugin (assembly `Web.NINAPlugin`, root namespace `Web.NINAPlugin`)
- **NINA.Plugin.Web.Test** — NUnit test suite

The web client itself (the browser app) lives in a separate repository: https://github.com/tcpalmer/nina.plugin.web.client. Its built `dist` folder is shipped inside the plugin package; it is not part of this solution.

## Documentation

- User-facing documentation for the plugin is the repository [README.md](../README.md) (also shown in the NINA plugin manager). Release history is in [CHANGELOG.md](../CHANGELOG.md).
- Documentation for NINA is available from https://nighttime-imaging.eu/docs/master/site/.

## NINA Interface

The NINA repository is https://github.com/isbeorn/nina. However, all plugin interaction with runtime NINA is via the https://github.com/isbeorn/nina/tree/develop/NINA.Plugin package, also available from https://www.nuget.org/packages/NINA.Plugin/.

## Code Style And Formatting

- Maintain the existing line-ending style of every touched file; default to CRLF for new files unless the target location dictates LF.
- Treat [`.editorconfig`](.editorconfig) as the canonical C# style source. It covers indentation, line endings, namespace style, `using` placement, `var` preferences, naming, and selected analyzer severities.
- For XAML, follow surrounding file style; no repo-wide XAML formatter configuration is checked in.
- Prefer modern C# supported by the target project. For new or refactored MVVM code, prefer `CommunityToolkit.Mvvm` where it fits instead of expanding legacy relay-command patterns.
- Remove any unused `using` directives before saving a file.
- Log via NINA's `NINA.Core.Utility.Logger`. Inside `Http/`, fully qualify it because `Swan.Logging.Logger` is also in scope.

## When Making Changes

- **Protected branches**: If the current branch is `main`, refuse all code changes and warn the user. Read-only tasks — analysis, research, design discussion — are permitted on any branch.
- Scope each change to the stated task only. Don't fix unrelated issues, restructure surrounding code, or expand the footprint beyond what was asked.
- Surface ambiguities before writing code rather than making assumptions. One clarifying question upfront beats an unwanted implementation.
- Propose targeted refactorings only when they directly serve the current task, and always call them out explicitly so the user can accept or skip them.
- After any code change, verify the project builds without new errors before reporting the task complete.
- Run affected tests when modifying logic that has test coverage; add tests for new behaviour where a clear test exists nearby to follow.
- Don't add new NuGet package dependencies without explicit approval.
- Any change to the shape of the session history JSON (`History/SessionHistory.cs`, `SessionList.cs`) or to the HTTP API is a contract change with the separate web client repo. Call it out explicitly.

## Plugin Components

### Plugin Entry Point and State

**`NINAWeb.cs`** — `WebPlugin` (the MEF-exported `IPluginManifest`). Its constructor receives `IProfileService`, `IImageSaveMediator` and `IImageDataFactory` from NINA, runs `HttpSetup.Initialize()`, purges old sessions, starts a new session history, and creates the three watchers.

The plugin has three states, selected in the options UI and stored per NINA profile via `PluginOptionsAccessor`:
- **OFF** — nothing running.
- **ON** — web server and all watchers running.
- **SHARE** — watchers running but no web server. This lets a second NINA instance record its own session history while the first instance serves the web app.

`changePluginState()` implements the transitions between states. `Teardown()` stops the server and watchers; session clean-up (`RemoveEmptySessions`, `DeactivateOldSessions`) only runs when this is the last NINA process running.

Other per-profile options: `WebServerPort` (default 80), `PurgeDays` (default 10), `NonLights` (also record flats/darks). The computed URLs (`LocalAddress`, `LocalNetworkAddress`, `HostAddress`) are stored in `Properties/Settings.settings`, not per profile. The options UI is `Options.xaml`.

### File Layout on Disk

All runtime data lives under `%LOCALAPPDATA%\NINA\WebPlugin\` (`CoreUtil.APPLICATIONTEMPPATH` + `HttpSetup.WEB_PLUGIN_HOME`). The layout is documented in `Http/HttpSetup.cs`:
- `dist\` — installed copy of the web client plus `webClientVersion.json`
- `sessions\sessions.json` — the session list
- `sessions\yyyyMMdd-HHmmss\` — one folder per NINA run, containing `sessionHistory.json`, `thumbnails\<id>.jpg`, and `images\` (cache of full-size web images and their status JSON)
- `webserver-*.log` — EmbedIO/Swan log files

`HttpSetup.Initialize()` creates these folders and copies the web client from the plugin install folder (`%LOCALAPPDATA%\NINA\Plugins\3.0.0\Web Session History Viewer\dist`) into `WebPlugin\dist` when it is missing or its `webClientVersion.json` version differs. Initialization fails if the plugin install folder can't be found under that exact name.

### Session History

`History/` holds the session data model and persistence.
- **`SessionHistory.cs`** — the JSON model: `SessionHistory` → `Target` → `ImageRecord`, plus lists of `NINALogEvent` and `AutofocusEvent`. Property names are lower camel case because they are serialized directly for the web client.
- **`SessionHistoryManager.cs`** — creates, updates, lists and purges sessions; writes thumbnails and `sessions.json`. Several instances are created (one per watcher and one per API controller), but they share a **static** `SessionHistoryVersionManager`.
- **`SessionHistoryVersionManager.cs`** — each update bumps `sessionVersion`, queues a snapshot, and continues on a deep clone. A background thread ("WSHV flush thread") writes queued versions to disk every 3 seconds. The web client polls the JSON files and uses `sessionVersion` to detect changes.
- **`SessionList.cs`** — the `sessions.json` model.

### Event Sources (Watchers)

Three watchers feed the session history. Each takes the session home folder through `setSessionHome()` and has `Start()`/`Stop()` methods driven by the plugin state.
- **`ImageSaveWatcher.cs`** — subscribes to `IImageSaveMediator.ImageSaved`. Adds an `ImageRecord` (with image statistics) under the target, and saves a thumbnail. Only light frames are recorded unless `NonLights` is on. Images with no target name are grouped under the image type plus filter.
- **`NINAEventWatcher.cs`** — wraps the NINA log watcher. It adds NINA start/stop events and restarts the log watcher when NINA rolls over to a new log file (a `FileSystemWatcher` on the Logs folder).
- **`Autofocus/`** — `AutofocusDirectoryWatcher` watches `%LOCALAPPDATA%\NINA\AutoFocus\*.json`; `AutofocusEventWatcher` turns new AF result files into `AutofocusEvent`s.

### NINA Log Event Parsing

Most timeline events are taken from the NINA log file, not from NINA APIs.
- **`LogEvent/NINALogWatcher.cs`** — tails the active NINA log file on a background thread ("WSHV log watcher thread").
- **`Utility/LogUtils.cs`** — `GetLogFileRE()` builds the regex that finds this process's active log file (`yyyyMMdd-HHmmss-<version>.<pid>-yyyyMM[dd].log`). If NINA changes its log naming, this is where things break.
- **`LogEvent/NINALogMessageProcessor.cs`** — splits each `|`-delimited log line, skips DEBUG/TRACE lines, and matches the message against regexes in `initMatchers()`. Each regex maps to an `EventMatcher` with a `NINALogEvent` type constant.
- **`LogEvent/NINALogEvent.cs`** — event type constants.

To add a new timeline event: add a type constant to `NINALogEvent`, add a regex and `EventMatcher` in `initMatchers()`, and coordinate with the web client so it can display the new type. The regexes match exact NINA log message text, so a new NINA version can break them without warning.

### HTTP Server and API

`Http/` holds the embedded web server.
- **`HttpServer.cs`** — EmbedIO `WebServer` on its own thread, listening on `http://*:<port>`. It serves the static files under `WebPlugin\` at `/` (web client at `/dist`, and the root path redirects there), plus the API at `/api/1020`. It also works out the localhost, host name and IPv4 URLs shown in the options UI.
- **`HttpServerInstance.cs`** — static singleton wrapper. `SetPort()` restarts a running server.
- **`Api1020/ApiV1020Controller.cs`** — the only endpoint: `PUT /api/1020/image/create`. It takes an `ImageStatusRequest` (session, image id, full path, stretch/scale/quality options), renders a stretched JPEG of the original image through `ImageSupport`, caches it under the session's `images\` folder (keyed by id and a hash of the request), and returns its URL path.
- **`ImageSupport.cs`** — image loading, stretching, scaling and JPEG encoding via NINA's `IImageDataFactory`.
- `WebSetup.cs` is an empty stub and is excluded from compilation in the csproj.

Session JSON and thumbnails are not served through the API. The web client fetches them directly as static files under `/sessions/...`.

### Build and Local Install

The post-build step in `NINA.Plugin.Web.csproj` copies `Web.NINAPlugin.dll`, `EmbedIO.dll` and `Swan.Lite.dll` to `%LOCALAPPDATA%\NINA\Plugins\3.0.0\Web.NINAPlugin`, then renames that folder to `Web Session History Viewer`. The rename fails if the destination folder already exists, so a stale install folder may need to be removed by hand. The post-build step does not copy the web client `dist` folder. It must already be in the plugin install folder, or `HttpSetup.Initialize()` will fail on version lookup.

### Testing

Tests live in `NINA.Plugin.Web.Test/`.
- **Framework:** NUnit (`[TestFixture]`, `[Test]`, `[SetUp]`)
- **Mocking:** Moq (`Mock<T>`, `.Setup()`, `.Returns()`)
- **Assertions:** FluentAssertions (`.Should()`, `.Be()`, etc.)

Coverage is light: `SessionHistoryTest` and `SessionHistoryManagerTest` (which uses a temporary root directory through the `SessionHistoryManager(string rootDirectory)` constructor). `TestHelper.cs` provides `ImageSavedEventArgs` and `IImageStatistics` fakes.

## Release Process

There are no release scripts in this repository; releases are done by hand (version bump in `Properties/AssemblyInfo.cs`, `CHANGELOG.md` entry, package with the web client `dist`, GitHub release, NINA plugin manifests PR). Do **not** perform or suggest any release step — tagging, creating GitHub releases, or pushing to the manifests fork — without explicit permission from the user.
