# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-06-21

### Added

- Initial release of `com.kidzdev.unity.scroll-snap`.
- `ScrollSnap` component — `ScrollRect`-based snap pager for horizontal and vertical axes, with `SnapAlignment` (Start / Center / End), seamless `wrapAround` infinite loop, peek support (`peekAmount`), configurable `snapDuration` / `snapCurve`, and focus-effect system (`enableFocusEffects`, `focusRange`).
- `IScrollSnapIndicator` seam — `Setup(int pageCount)` + `OnPageChanged(int page)`; implement to create custom indicators.
- `IScrollSnapItem` seam — `UpdateFocus(float distance01, bool isFocused)`; implement on item cards to drive focus effects.
- `ScrollSnapIndicatorBase` — abstract base handling slot pooling, windowing (`maxVisible`), clickable jump, and editor preview (`previewInEditor`).
- `DotIndicator` — row of animated dots with active-colour, pill mode (`pillMode`, `activePillWidth`), custom sprites, edge-dot scaling, and windowed sliding window.
- `NumberIndicator` — `TextMeshProUGUI` label with configurable format string (default `"{0} / {1}"`).
- `PageButtonIndicator` — row of clickable numbered buttons with active/inactive colour theming and edge scaling.
- `ScrollSnapNavigator` — wires prev/next `Button`s to `SnapToPrev` / `SnapToNext`; disables at ends when `wrapAround` is off.
- `ScrollSnapAutoPlay` — advances carousel on a timer; pauses on drag and resumes after `resumeDelay`.
- `ScrollSnapItemScaler` — drives `localScale` and `CanvasGroup.alpha` from focus distance via `AnimationCurve`s; implements `IScrollSnapItem`.
- `SnapMath` utility — `AlignOffset` and `FocusDistance01` static helpers.
- `ScrollSnapAxis` enum (`Horizontal`, `Vertical`).
- `SnapAlignment` enum (`Start`, `Center`, `End`).
- Editor menu items under **GameObject → UI → Scroll Snap** for Horizontal Carousel, Peek Carousel, and Vertical Picker — each creates a fully wired `ScrollSnap` rig with viewport, `GridLayoutGroup` content, sample items, and a `DotIndicator`.
- Demo sample scene (`Samples~/Demo`) covering carousel + navigator + dots, coverflow + focus effects + counter, peek carousel, infinite loop, and vertical date picker.
- Edit-mode tests (`Tests/Editor`).
