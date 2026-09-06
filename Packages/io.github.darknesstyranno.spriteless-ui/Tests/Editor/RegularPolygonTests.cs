using NUnit.Framework;
using SpriteLessUI.Geometry;
using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Tests
{
    public sealed class RegularPolygonTests
    {
        [TestCase(3, 0f, 0f)] [TestCase(4, 0.000001f, 0.000001f)]
        [TestCase(5, 15f, 4f)] [TestCase(64, 1000f, 20f)] [TestCase(3, 1000f, 1000f)]
        public void RoundedPolygonRemainsFiniteAndWithinRect(int sides, float radius, float border)
        {
            var go = new GameObject("Rounded polygon", typeof(RectTransform), typeof(SpriteLessImage));
            try
            {
                var image = go.GetComponent<SpriteLessImage>();
                image.rectTransform.sizeDelta = new Vector2(100, 80);
                image.Shape = ProceduralShape.RegularPolygon;
                image.PolygonSides = sides;
                image.PolygonRadius = radius;
                image.BorderEnabled = true;
                image.BorderWidth = border;
                image.EdgeEffectEnabled = true;
                using (var vh = new VertexHelper())
                {
                    image.PopulateMesh(vh);
                    Assert.That(vh.currentVertCount, Is.GreaterThan(0).And.LessThan(65000));
                    var vertex = new UIVertex();
                    for (int i = 0; i < vh.currentVertCount; i++)
                    {
                        vh.PopulateUIVertex(ref vertex, i);
                        Assert.That(vertex.position.x, Is.InRange(-50.001f, 50.001f));
                        Assert.That(vertex.position.y, Is.InRange(-40.001f, 40.001f));
                    }
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RoundedPolygonRaycastExcludesCutCorner()
        {
            var go = new GameObject("Rounded square", typeof(RectTransform), typeof(SpriteLessImage));
            try
            {
                var image = go.GetComponent<SpriteLessImage>();
                image.rectTransform.sizeDelta = new Vector2(100, 100);
                image.Shape = ProceduralShape.RegularPolygon;
                image.PolygonSides = 4;
                image.PolygonRotation = 45;
                Assert.That(image.ContainsLocalPoint(new Vector2(49, 49)), Is.True);
                image.PolygonRadius = 20;
                Assert.That(image.ContainsLocalPoint(new Vector2(49, 49)), Is.False);
                Assert.That(image.ContainsLocalPoint(Vector2.zero), Is.True);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [TestCase(3, 0f)] [TestCase(4, 45f)] [TestCase(5, 17f)] [TestCase(64, 359f)]
        public void PerimeterHasEqualEdgesAndFitsNonSquareRect(int sides, float rotation)
        {
            var rect = new Rect(-120f, -40f, 240f, 80f);
            var points = new Vector2[sides];
            RegularPolygonGeometry.BuildPerimeter(rect, sides, rotation, points);
            float edge = Vector2.Distance(points[0], points[1]);
            for (int i = 0; i < sides; i++)
            {
                Assert.That(Vector2.Distance(points[i], points[(i + 1) % sides]), Is.EqualTo(edge).Within(0.001f));
                Assert.That(points[i].x, Is.InRange(rect.xMin - 0.001f, rect.xMax + 0.001f));
                Assert.That(points[i].y, Is.InRange(rect.yMin - 0.001f, rect.yMax + 0.001f));
            }
        }

        [TestCase(ProceduralShape.Circle)] [TestCase(ProceduralShape.RoundedRectangle)]
        [TestCase(ProceduralShape.Arc)] [TestCase(ProceduralShape.Chevron)]
        public void CaptureQualityDoesNotLeakToOtherGraphics(ProceduralShape shape)
        {
            var go = new GameObject("Quality", typeof(RectTransform), typeof(SpriteLessImage));
            try
            {
                var image = go.GetComponent<SpriteLessImage>();
                image.Shape = shape;
                image.rectTransform.sizeDelta = new Vector2(1000, 1000);
                image.CornerRadius = 100;
                using (var vh = new VertexHelper())
                {
                    image.PopulateMesh(vh); int original = vh.currentVertCount;
                    image.MeshDetailScale = 8;
                    Assert.DoesNotThrow(() => image.PopulateMesh(vh));
                    Assert.That(vh.currentVertCount, Is.GreaterThan(original));
                    Assert.That(TessellationQuality.Multiplier, Is.EqualTo(1f));
                    image.MeshDetailScale = 1;
                    image.PopulateMesh(vh);
                    Assert.That(vh.currentVertCount, Is.EqualTo(original));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [TestCase(0f)] [TestCase(0.0001f)] [TestCase(5f)] [TestCase(100f)]
        public void BorderAndEffectProduceFiniteMesh(float width)
        {
            var go = new GameObject("Polygon", typeof(RectTransform), typeof(SpriteLessImage));
            try
            {
                var image = go.GetComponent<SpriteLessImage>();
                image.Shape = ProceduralShape.RegularPolygon;
                image.rectTransform.sizeDelta = new Vector2(100f, 60f);
                image.BorderEnabled = true;
                image.BorderWidth = width;
                image.EdgeEffectEnabled = true;
                using (var vh = new VertexHelper())
                {
                    image.PopulateMesh(vh);
                    Assert.That(vh.currentVertCount, Is.GreaterThan(0));
                    var vertex = new UIVertex();
                    for (int i = 0; i < vh.currentVertCount; i++)
                    {
                        vh.PopulateUIVertex(ref vertex, i);
                        Assert.That(float.IsNaN(vertex.position.x) || float.IsInfinity(vertex.position.x), Is.False);
                    }
                }
                Assert.That(image.ContainsLocalPoint(Vector2.zero), Is.True);
                Assert.That(image.ContainsLocalPoint(new Vector2(50, 30)), Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
