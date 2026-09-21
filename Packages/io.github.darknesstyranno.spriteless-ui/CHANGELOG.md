# Changelog

## [Unreleased]

### Added

- SpriteLessLine for straight lines and open polylines: normalized RectTransform points, uniform thickness, flat/round caps, bounded miter/round joins, inset geometry AA and UI material UVs.
- Line creation menu and Scene point editing with insertion, removal, Undo and Prefab override recording.
- Shift-drag either Line endpoint to extend it by one segment, with single-step Undo and Escape to cancel.
- Runtime geometry regressions for zero/thin strokes, repeated points, reversals, short edges and maximum tessellation.

## [0.2.1] - 2026-09-10

### Added

- Packaged editor icon for the SpriteLessImage script.

## [0.2.0] - 2026-09-07

### Added

- `PolygonCentered` / Center on Rect keeps the polygon circumcenter and size stable during rotation without changing the RectTransform.
- Regular Polygon corner rounding via `PolygonRadius`, including matching inside Border, AA, Edge Effect and Shape Raycast.
- Shared circular polygon-corner tessellation utility for authored polygon integrations.
- Regular Polygon shape with 3–64 sides, clockwise rotation, equal-edge fitting, Border, AA, Edge Effect and Shape Raycast.
- Non-serialized mesh detail multiplier for temporary high-resolution captures; default runtime tessellation is unchanged.

## [0.1.0] - 2026-08-13

### Added

- Geometry-based `SpriteLessImage` for uGUI
- Rounded Rectangle, Circle, Quadrilateral, Arc, Triangle, and Chevron shapes
- Ring through a closed 360-degree Arc
- Flat and Round Arc caps with clockwise and counter-clockwise sweep
- Per-corner XY offsets for convex Quadrilateral shapes
- Four-way Triangle direction with border and Edge Effect support
- Isosceles, fitted Equilateral, and four-corner Right Triangle layouts
- Four-way Chevron with thickness, spread, Flat or Round caps, and Miter or Round joins
- Stretch and Equilateral Chevron proportions
- RectTransform-normalized UV0 coordinates for generated geometry
- Custom Material field in the SpriteLessImage Inspector
- Optional shape-accurate raycast area for every generated shape
- Uniform and individual corner radii
- Inside border with inner-edge and inset outer-contour geometry feather anti-aliasing
- Directional inset Edge Effect for Rounded Rectangle, Circle, and Quadrilateral shapes
- Rounded Rectangle, Circle, Parallelogram, Trapezoid, Ring, Arc, Triangle, Chevron, Panel, Button, and Slider creation menus
- EditMode geometry tests
- Unity 2021.3 LTS or newer compatibility
