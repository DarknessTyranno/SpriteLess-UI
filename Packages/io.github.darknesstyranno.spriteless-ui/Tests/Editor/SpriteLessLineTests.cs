using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpriteLessUI.Tests
{
    public sealed class SpriteLessLineTests
    {
        private GameObject m_Object;
        private SpriteLessLine m_Line;

        [SetUp]
        public void SetUp()
        {
            m_Object = new GameObject("Line", typeof(RectTransform), typeof(SpriteLessLine));
            m_Line = m_Object.GetComponent<SpriteLessLine>();
            m_Line.rectTransform.sizeDelta = new Vector2(100, 100);
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(m_Object);

        [TestCase(ArcCap.Flat, false)] [TestCase(ArcCap.Flat, true)]
        [TestCase(ArcCap.Round, false)] [TestCase(ArcCap.Round, true)]
        public void StraightLineHasCorrectBoundsColorAndUv(ArcCap cap, bool aa)
        {
            m_Line.SetPoints(new[] { new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f) });
            m_Line.Thickness = 10;
            m_Line.Cap = cap;
            m_Line.AntiAliasingEnabled = aa;
            m_Line.color = new Color(1, 0.2f, 0.4f, 0.5f);
            using (var vh = new VertexHelper())
            {
                m_Line.PopulateMesh(vh);
                var mesh = new Mesh();
                try
                {
                    vh.FillMesh(mesh);
                    Assert.That(mesh.bounds.size.x, Is.EqualTo(cap == ArcCap.Flat ? 60 : 70).Within(0.001f));
                    Assert.That(mesh.bounds.size.y, Is.EqualTo(10).Within(0.001f));
                    foreach (Color32 color in mesh.colors32)
                    {
                        Assert.That(color.r, Is.EqualTo(255));
                        Assert.That(color.a, Is.EqualTo(128).Or.EqualTo(0));
                    }
                    if (aa) Assert.That(Array.Exists(mesh.colors32, c => c.a == 0), Is.True);
                    for (int i = 0; i < mesh.vertexCount; i++)
                        Assert.That(Vector2.Distance(mesh.uv[i], ((Vector2)mesh.vertices[i] + Vector2.one * 50) / 100), Is.LessThan(0.00001f));
                }
                finally { Object.DestroyImmediate(mesh); }
            }
            Assert.That(m_Object.GetComponent<CanvasRenderer>(), Is.Not.Null);
        }

        [TestCase(0f)] [TestCase(0.000001f)] [TestCase(0.01f)] [TestCase(1f)] [TestCase(20f)] [TestCase(1000f)]
        public void EditingDegeneratePathsDoesNotThrowOrExceedVertexLimit(float thickness)
        {
            var paths = new[] {
                new[] { Vector2.zero, Vector2.zero },
                new[] { Vector2.zero, Vector2.one, Vector2.one },
                new[] { Vector2.zero, Vector2.one * 0.5f, Vector2.one },
                new[] { Vector2.zero, Vector2.one, Vector2.zero },
                new[] { Vector2.zero, Vector2.one, new Vector2(0.0001f, 0) },
                new[] { Vector2.zero, new Vector2(0.5f, 0), new Vector2(0.50001f, 0.00001f), Vector2.one },
                new[] { Vector2.zero, Vector2.one, new Vector2(0, 1), new Vector2(1, 0) }
            };
            foreach (var path in paths)
            foreach (ArcCap cap in Enum.GetValues(typeof(ArcCap)))
            foreach (StrokeJoin join in Enum.GetValues(typeof(StrokeJoin)))
            {
                m_Line.SetPoints(path);
                m_Line.Cap = cap;
                m_Line.Join = join;
                m_Line.Thickness = thickness;
                using (var vh = new VertexHelper())
                {
                    m_Line.PopulateMesh(vh);
                    if (thickness == 0) Assert.That(vh.currentVertCount, Is.Zero);
                    CheckMesh(vh);
                }
            }
        }

        [TestCase(StrokeJoin.Miter)] [TestCase(StrokeJoin.Round)]
        public void CornerFillHasNoOverlappingTriangles(StrokeJoin join)
        {
            m_Line.SetPoints(new[] { new Vector2(0.1f, 0.3f), new Vector2(0.7f, 0.3f), new Vector2(0.7f, 0.9f) });
            m_Line.Thickness = 12;
            m_Line.AntiAliasingEnabled = false;
            m_Line.Join = join;
            using (var vh = new VertexHelper())
            {
                m_Line.PopulateMesh(vh);
                var triangles = new List<UIVertex>();
                vh.GetUIVertexStream(triangles);
                for (float y = -29.123f; y < 45; y += 0.731f)
                for (float x = 9.321f; x < 29; x += 0.763f)
                {
                    int coverage = 0;
                    for (int i = 0; i < triangles.Count; i += 3)
                        if (Inside(new Vector2(x, y), triangles[i].position, triangles[i + 1].position, triangles[i + 2].position)) coverage++;
                    Assert.That(coverage, Is.LessThanOrEqualTo(1), $"Overlap at {x},{y}");
                }
            }
        }

        [Test]
        public void ManyPointsAndMaximumCaptureDetailStayWithinUiMeshBudget()
        {
            var points = new Vector2[SpriteLessLine.MaximumPoints];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector2(i / 255f, i % 2 == 0 ? 0.1f : 0.9f);
            m_Line.SetPoints(points);
            m_Line.MeshDetailScale = 8;
            using (var vh = new VertexHelper()) { m_Line.PopulateMesh(vh); CheckMesh(vh); }
        }

        [Test]
        public void SetPointsValidatesAndCopiesInput()
        {
            Assert.Throws<ArgumentException>(() => m_Line.SetPoints(null));
            Assert.Throws<ArgumentException>(() => m_Line.SetPoints(new Vector2[1]));
            Assert.Throws<ArgumentException>(() => m_Line.SetPoints(new Vector2[257]));
            Assert.Throws<ArgumentException>(() => m_Line.SetPoints(new[] { Vector2.zero, new Vector2(float.NaN, 0) }));
            var points = new[] { Vector2.zero, Vector2.one };
            m_Line.SetPoints(points);
            points[0] = Vector2.one;
            Assert.That(m_Line.Points[0], Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void RectResizeChangesPathButNotThicknessAndSerializedCloneRetainsSettings()
        {
            m_Line.Cap = ArcCap.Flat;
            m_Line.Thickness = 7;
            m_Line.rectTransform.sizeDelta = new Vector2(400, 100);
            var clone = new GameObject("Clone", typeof(RectTransform), typeof(SpriteLessLine));
            try
            {
                var copy = clone.GetComponent<SpriteLessLine>();
                UnityEditor.EditorUtility.CopySerialized(m_Line, copy);
                Assert.That(copy.Points, Is.EqualTo(m_Line.Points));
                Assert.That(copy.Thickness, Is.EqualTo(7));
                using (var vh = new VertexHelper())
                {
                    m_Line.PopulateMesh(vh);
                    var vertex = new UIVertex();
                    vh.PopulateUIVertex(ref vertex, 2);
                    Assert.That(vertex.position.x, Is.EqualTo(-160).Within(0.001f));
                    Assert.That(vertex.position.y, Is.EqualTo(3.5f).Within(0.001f));
                }
            }
            finally { Object.DestroyImmediate(clone); }
        }

        private static void CheckMesh(VertexHelper vh)
        {
            Assert.That(vh.currentVertCount, Is.LessThan(65000));
            var vertices = new List<UIVertex>();
            vh.GetUIVertexStream(vertices);
            for (int i = 0; i < vertices.Count; i++)
            {
                Assert.That(float.IsNaN(vertices[i].position.x) || float.IsInfinity(vertices[i].position.x), Is.False);
                Assert.That(float.IsNaN(vertices[i].position.y) || float.IsInfinity(vertices[i].position.y), Is.False);
            }
        }

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            return Cross(b - a, p - a) < -0.00001f && Cross(c - b, p - b) < -0.00001f && Cross(a - c, p - c) < -0.00001f;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
