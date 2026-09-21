# SpriteLess UI

Geometry-based, sprite-free graphics for Unity uGUI.

![SpriteLess UI shape showcase](sample.png)

## Requirements

- Unity 2021.3 LTS or newer
- Unity UI (`com.unity.ugui`) 1.0.0 or the version bundled with the Editor
- Git 2.14 or newer when installing from a Git URL

No TextMesh Pro, Scriptable Render Pipeline, generated texture, or custom shader dependency is required.

## Features

- Rounded Rectangle, Rectangle, and Pill
- Circle and Ellipse through RectTransform sizing
- Parallelogram, Trapezoid, and convex Quadrilateral through per-corner XY offsets
- Isosceles, Equilateral, and Right Triangle types
- Regular Polygon with 3–64 equal sides and clockwise rotation, fitted without stretching
- Chevron through Stretch or Equilateral proportions, direction, thickness, spread, cap, and join controls
- Ring and Arc through thickness, start angle, signed sweep angle, and cap style
- SpriteLessLine: editable straight lines and open polylines with flat/round caps and miter/round joins
- Individual corner radii
- Inside border with an anti-aliased inner edge
- Directional inset edge color for lightweight button and panel depth
- Inset geometry feather anti-aliasing that stays inside the shape bounds
- Screen Space Overlay, Mask, RectMask2D, Button, Slider, and Layout support
- RectTransform or shape-accurate raycast areas
- Shared default UI material and white texture
- RectTransform-based normalized UV mapping for custom UI materials

## Usage

Create an element from `GameObject > UI (Canvas) > SpriteLess`, or add a `SpriteLessImage` component to a UI GameObject.

`SpriteLessImage` exposes one Shape selector and the valid settings for that shape. Fill, border, edge effect, and anti-aliasing are generated as one CanvasRenderer mesh. Chevron uses one continuous stroke mesh and intentionally omits border and edge effect settings.

Equilateral Chevron preserves the proportions of two sides of an equilateral triangle while fitting inside the RectTransform.

Triangle supports RectTransform-filling Isosceles, fitted Equilateral, and four-corner Right Triangle layouts.

Regular Polygon uses `PolygonSides`, `PolygonRotation`, and `PolygonRadius` (default 0). The sharp base polygon keeps equal edges in non-square rectangles; Radius rounds its corners without moving its control vertices. Border, Edge Effect, AA and Shape Raycast follow the rounded perimeter. Radius is limited by the available edge length.

Enable **Center on Rect** (`PolygonCentered`) to keep the polygon center at the RectTransform center. Its circumradius is half the shorter rect dimension, so rotation keeps the same center and size, with possible extra space at the edges. Off (default) preserves the existing bounds-fit behavior. RectTransform pivot and size are not modified.

`MeshDetailScale` is a non-serialized capture hook (1–8). The default is 1; capture tools can increase it on temporary copies without changing saved/runtime defaults.

Every vertex uses RectTransform-normalized `uv0` coordinates from bottom-left `(0, 0)` to top-right `(1, 1)`.

Assign uGUI-compatible custom materials through the `Material` field in the SpriteLessImage Inspector.

Set `Raycast Area` to `Shape` for pointer input that follows non-rectangular geometry. The default `Rect Transform` option preserves standard uGUI behavior.

### SpriteLessLine

Create **GameObject > UI (Canvas) > SpriteLess > Line**. Adjust Thickness, Color, Cap, Join and AA in the Inspector.
Drag points in Scene view, **Shift-drag either endpoint** to extend the line, click a segment midpoint to insert a point, or use **Remove Selected Point**. Each endpoint drag adds one point and supports a single Undo; Escape cancels it. Clicking without dragging does not add a point. You can also edit the Inspector's Normalized Points list.

Points use RectTransform coordinates: `(0, 0)` is bottom-left and `(1, 1)` is top-right. Resizing the rect moves the points without changing the thickness in UI units. The stroke is centered on its path, so keep enough room for its width and caps. Points outside 0–1 are permitted but can be clipped by parent masks or baking bounds.

`SetPoints(Vector2[])` accepts 2–256 points and copies the input. Consecutive duplicates are ignored when drawing; zero thickness or an entirely collapsed path produces no mesh. Sharp/short joins are limited to avoid spikes. Self-intersections and retraced segments overlap; they are not polygon-unioned and can darken translucent lines.

Line supports the usual uGUI Material, Mask and RectMask2D. Raycast Target is off for newly created lines; when enabled it uses the standard rectangular hit area, not a stroke-only hit area. Bézier curves, closed paths, dashed lines and variable width are not included.

## Installation

Add the package from this Git URL:

```text
https://github.com/DarknessTyranno/SpriteLess-UI.git?path=/Packages/io.github.darknesstyranno.spriteless-ui
```

## License

MIT
