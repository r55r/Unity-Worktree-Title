# Changelog

All notable changes to this package will be documented in this file.

## [0.1.2] - 2026-08-20

### Fixed

- Detect the containing Git checkout when the Unity project is inside a monorepo subdirectory.

## [0.1.1] - 2026-08-15

### Changed

- Reduced transient allocations and simplified internal Git, Codex index, and monitor state handling without changing the displayed title contract.

## [0.1.0] - 2026-08-15

### Added

- Git primary checkout and linked worktree labels in the Unity Editor main window title.
- Best-effort Codex worktree ID and task-name integration.
- Two-second session index change detection with effective-title refresh.
- Focused Editor tests for Git metadata, Codex fallback, title formatting, Unicode normalization, and rename detection.
