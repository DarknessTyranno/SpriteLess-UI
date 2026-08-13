using UnityEngine;

namespace SpriteLessUI.Geometry
{
    internal static class ShapeDirectionUtility
    {
        public static Vector2 GetForward(ShapeDirection direction)
        {
            switch (direction)
            {
                case ShapeDirection.Up:
                    return Vector2.up;

                case ShapeDirection.Right:
                    return Vector2.right;

                case ShapeDirection.Down:
                    return Vector2.down;

                case ShapeDirection.Left:
                    return Vector2.left;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static Vector2 GetLeft(Vector2 forward)
        {
            return new Vector2(-forward.y, forward.x);
        }

        public static void GetHalfExtents(
            Rect rect,
            ShapeDirection direction,
            out float forwardExtent,
            out float sideExtent)
        {
            bool horizontal = direction == ShapeDirection.Left
                || direction == ShapeDirection.Right;
            forwardExtent = (horizontal ? rect.width : rect.height) * 0.5f;
            sideExtent = (horizontal ? rect.height : rect.width) * 0.5f;
        }
    }
}
