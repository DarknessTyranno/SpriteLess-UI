using NUnit.Framework;
using SpriteLessUI.Geometry;
using UnityEngine;
using UnityEngine.UI;

namespace SpriteLessUI.Tests
{
    public sealed class SpriteLessImageTests
    {
        private GameObject m_GameObject;
        private SpriteLessImage m_Image;

        [SetUp]
        public void SetUp()
        {
            m_GameObject = new GameObject(
                "SpriteLess Image",
                typeof(RectTransform),
                typeof(SpriteLessImage));
            m_Image = m_GameObject.GetComponent<SpriteLessImage>();
            m_Image.rectTransform.sizeDelta = new Vector2(100f, 60f);
            m_Image.AntiAliasingEnabled = false;
        }

        [Test]
        public void SpriteLessImageRequiresCanvasRenderer()
        {
            Assert.That(m_GameObject.GetComponent<CanvasRenderer>(), Is.Not.Null);
        }

        [Test]
        public void SpriteLessImageUsesOneUnitAntiAliasingByDefault()
        {
            Assert.That(m_Image.AntiAliasingWidth, Is.EqualTo(1f));
        }

        [Test]
        public void RaycastModeDefaultsToRectTransform()
        {
            Assert.That(
                m_Image.RaycastMode,
                Is.EqualTo(SpriteLessRaycastMode.RectTransform));
        }

        [Test]
        public void RoundedRectangleShapeRaycastRejectsRoundedCorner()
        {
            m_Image.Shape = ProceduralShape.RoundedRectangle;
            m_Image.CornerRadius = 12f;

            Assert.That(m_Image.ContainsLocalPoint(Vector2.zero), Is.True);
            Assert.That(m_Image.ContainsLocalPoint(new Vector2(-49f, 29f)), Is.False);
        }

        [Test]
        public void CircleShapeRaycastRejectsRectTransformCorner()
        {
            m_Image.Shape = ProceduralShape.Circle;

            Assert.That(m_Image.ContainsLocalPoint(Vector2.zero), Is.True);
            Assert.That(m_Image.ContainsLocalPoint(new Vector2(49f, 29f)), Is.False);
        }

        [Test]
        public void QuadrilateralShapeRaycastUsesCornerOffsets()
        {
            m_Image.Shape = ProceduralShape.Quadrilateral;
            m_Image.CornerOffsets = new CornerOffsets(
                new Vector2(20f, 0f),
                Vector2.zero,
                new Vector2(-20f, 0f),
                Vector2.zero);

            Assert.That(m_Image.ContainsLocalPoint(Vector2.zero), Is.True);
            Assert.That(m_Image.ContainsLocalPoint(new Vector2(-49f, 29f)), Is.False);
        }

        [Test]
        public void TriangleShapeRaycastRejectsEmptyCorner()
        {
            m_Image.Shape = ProceduralShape.Triangle;
            m_Image.Direction = ShapeDirection.Up;

            Assert.That(m_Image.ContainsLocalPoint(Vector2.zero), Is.True);
            Assert.That(m_Image.ContainsLocalPoint(new Vector2(-49f, 29f)), Is.False);
        }

        [Test]
        public void ArcShapeRaycastUsesSweepAndThickness()
        {
            m_Image.Shape = ProceduralShape.Arc;
            m_Image.ArcThickness = 10f;
            m_Image.ArcStartAngle = 0f;
            m_Image.ArcSweepAngle = 90f;
            m_Image.ArcCap = ArcCap.Flat;

            Assert.That(m_Image.ContainsLocalPoint(new Vector2(0f, 25f)), Is.True);
            Assert.That(m_Image.ContainsLocalPoint(Vector2.zero), Is.False);
            Assert.That(m_Image.ContainsLocalPoint(new Vector2(-25f, 0f)), Is.False);
        }

        [Test]
        public void ArcShapeRaycastIncludesRoundCap()
        {
            m_Image.Shape = ProceduralShape.Arc;
            m_Image.ArcThickness = 10f;
            m_Image.ArcStartAngle = 0f;
            m_Image.ArcSweepAngle = 90f;
            m_Image.ArcCap = ArcCap.Flat;
            Assert.That(m_Image.ContainsLocalPoint(new Vector2(-4f, 25f)), Is.False);

            m_Image.ArcCap = ArcCap.Round;
            Assert.That(m_Image.ContainsLocalPoint(new Vector2(-4f, 25f)), Is.True);
        }

        [Test]
        public void RingShapeRaycastRejectsCenterHole()
        {
            m_Image.Shape = ProceduralShape.Arc;
            m_Image.ArcThickness = 10f;
            m_Image.ArcSweepAngle = 360f;

            Assert.That(m_Image.ContainsLocalPoint(new Vector2(0f, 25f)), Is.True);
            Assert.That(m_Image.ContainsLocalPoint(Vector2.zero), Is.False);
        }

        [Test]
        public void ChevronShapeRaycastUsesStrokeContour()
        {
            m_Image.Shape = ProceduralShape.Chevron;
            m_Image.Direction = ShapeDirection.Up;
            m_Image.ChevronThickness = 8f;
            m_Image.ChevronSpread = 1f;
            m_Image.ChevronCap = ArcCap.Round;
            m_Image.ChevronJoin = StrokeJoin.Round;

            Assert.That(m_Image.ContainsLocalPoint(new Vector2(-23f, 0f)), Is.True);
            Assert.That(m_Image.ContainsLocalPoint(Vector2.zero), Is.False);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_GameObject);
        }

        [Test]
        public void RectangleWithoutEffectsUsesMinimalGeometry()
        {
            m_Image.Shape = ProceduralShape.RoundedRectangle;
            m_Image.CornerRadius = 0f;
            m_Image.BorderEnabled = false;

            using (VertexHelper vertexHelper = new VertexHelper())
            {
                m_Image.PopulateMesh(vertexHelper);

                Assert.That(vertexHelper.currentVertCount, Is.EqualTo(5));
                Assert.That(vertexHelper.currentIndexCount, Is.EqualTo(12));
            }
        }

        [TestCase(ProceduralShape.RoundedRectangle)]
        [TestCase(ProceduralShape.Circle)]
        [TestCase(ProceduralShape.Quadrilateral)]
        [TestCase(ProceduralShape.Arc)]
        [TestCase(ProceduralShape.Triangle)]
        [TestCase(ProceduralShape.Chevron)]
        public void UvCoordinatesFollowRectTransform(ProceduralShape shape)
        {
            m_Image.Shape = shape;
            m_Image.ArcSweepAngle = 270f;
            m_Image.ChevronThickness = 8f;

            Mesh mesh = BuildMesh();
            try
            {
                Vector3[] vertices = mesh.vertices;
                Vector2[] uv = mesh.uv;
                Assert.That(uv.Length, Is.EqualTo(vertices.Length));

                for (int i = 0; i < vertices.Length; i++)
                {
                    float expectedX = Mathf.InverseLerp(-50f, 50f, vertices[i].x);
                    float expectedY = Mathf.InverseLerp(-30f, 30f, vertices[i].y);
                    Assert.That(uv[i].x, Is.EqualTo(expectedX).Within(0.0001f));
                    Assert.That(uv[i].y, Is.EqualTo(expectedY).Within(0.0001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void RoundedRectangleBorderContainsFillAndBorderColors()
        {
            Color32 fillColor = Color.blue;
            Color32 borderColor = Color.red;
            m_Image.color = fillColor;
            m_Image.CornerRadius = 12f;
            m_Image.BorderEnabled = true;
            m_Image.BorderWidth = 4f;
            m_Image.BorderColor = borderColor;

            Mesh mesh = BuildMesh();
            try
            {
                CollectionAssert.Contains(mesh.colors32, fillColor);
                CollectionAssert.Contains(mesh.colors32, borderColor);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void CircleFollowsRectTransformAspectRatio()
        {
            m_Image.Shape = ProceduralShape.Circle;
            m_Image.rectTransform.sizeDelta = new Vector2(120f, 60f);

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.bounds.size.x, Is.EqualTo(120f).Within(0.01f));
                Assert.That(mesh.bounds.size.y, Is.EqualTo(60f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void TriangleDirectionMovesItsTip()
        {
            m_Image.Shape = ProceduralShape.Triangle;
            m_Image.Direction = ShapeDirection.Up;
            Mesh upMesh = BuildMesh();

            m_Image.Direction = ShapeDirection.Right;
            Mesh rightMesh = BuildMesh();

            try
            {
                Assert.That(
                    System.Array.Exists(
                        upMesh.vertices,
                        vertex => Mathf.Abs(vertex.x) <= 0.001f
                            && Mathf.Abs(vertex.y - 30f) <= 0.001f),
                    Is.True);
                Assert.That(
                    System.Array.Exists(
                        rightMesh.vertices,
                        vertex => Mathf.Abs(vertex.x - 50f) <= 0.001f
                            && Mathf.Abs(vertex.y) <= 0.001f),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(upMesh);
                Object.DestroyImmediate(rightMesh);
            }
        }

        [TestCase(TriangleType.Isosceles)]
        [TestCase(TriangleType.Equilateral)]
        [TestCase(TriangleType.Right)]
        public void TriangleWithoutEffectsUsesOneFace(TriangleType type)
        {
            m_Image.Shape = ProceduralShape.Triangle;
            m_Image.TriangleType = type;
            m_Image.BorderEnabled = false;
            m_Image.AntiAliasingEnabled = false;

            using (VertexHelper vertexHelper = new VertexHelper())
            {
                m_Image.PopulateMesh(vertexHelper);

                Assert.That(vertexHelper.currentVertCount, Is.EqualTo(3));
                Assert.That(vertexHelper.currentIndexCount, Is.EqualTo(3));
            }
        }

        [TestCase(ShapeDirection.Up)]
        [TestCase(ShapeDirection.Right)]
        [TestCase(ShapeDirection.Down)]
        [TestCase(ShapeDirection.Left)]
        public void EquilateralTriangleKeepsEqualSideLengths(ShapeDirection direction)
        {
            m_Image.Shape = ProceduralShape.Triangle;
            m_Image.TriangleType = TriangleType.Equilateral;
            m_Image.Direction = direction;

            Mesh mesh = BuildMesh();
            try
            {
                Vector3[] vertices = mesh.vertices;
                float firstSide = Vector3.Distance(vertices[0], vertices[1]);
                float secondSide = Vector3.Distance(vertices[1], vertices[2]);
                float thirdSide = Vector3.Distance(vertices[2], vertices[0]);

                Assert.That(firstSide, Is.EqualTo(secondSide).Within(0.001f));
                Assert.That(secondSide, Is.EqualTo(thirdSide).Within(0.001f));
                Assert.That(mesh.bounds.min.x, Is.GreaterThanOrEqualTo(-50.01f));
                Assert.That(mesh.bounds.max.x, Is.LessThanOrEqualTo(50.01f));
                Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-30.01f));
                Assert.That(mesh.bounds.max.y, Is.LessThanOrEqualTo(30.01f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(TriangleRightAngle.BottomLeft, -49f, -29f, 49f, 29f)]
        [TestCase(TriangleRightAngle.BottomRight, 49f, -29f, -49f, 29f)]
        [TestCase(TriangleRightAngle.TopRight, 49f, 29f, -49f, -29f)]
        [TestCase(TriangleRightAngle.TopLeft, -49f, 29f, 49f, -29f)]
        public void RightTriangleUsesSelectedCorner(
            TriangleRightAngle rightAngle,
            float insideX,
            float insideY,
            float outsideX,
            float outsideY)
        {
            m_Image.Shape = ProceduralShape.Triangle;
            m_Image.TriangleType = TriangleType.Right;
            m_Image.TriangleRightAngle = rightAngle;

            Assert.That(m_Image.ContainsLocalPoint(new Vector2(insideX, insideY)), Is.True);
            Assert.That(m_Image.ContainsLocalPoint(new Vector2(outsideX, outsideY)), Is.False);
        }

        [TestCase(TriangleType.Isosceles)]
        [TestCase(TriangleType.Equilateral)]
        [TestCase(TriangleType.Right)]
        public void TriangleSupportsBorderAndAntiAliasing(TriangleType type)
        {
            Color32 fillColor = Color.blue;
            Color32 borderColor = Color.red;
            m_Image.Shape = ProceduralShape.Triangle;
            m_Image.TriangleType = type;
            m_Image.color = fillColor;
            m_Image.BorderEnabled = true;
            m_Image.BorderWidth = 4f;
            m_Image.BorderColor = borderColor;
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1f;

            Mesh mesh = BuildMesh();
            try
            {
                CollectionAssert.Contains(mesh.colors32, fillColor);
                CollectionAssert.Contains(mesh.colors32, borderColor);
                Assert.That(System.Array.Exists(mesh.colors32, color => color.a == 0), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void ChevronStaysInsideRectTransformBounds()
        {
            m_Image.Shape = ProceduralShape.Chevron;
            m_Image.Direction = ShapeDirection.Right;
            m_Image.ChevronThickness = 8f;
            m_Image.ChevronSpread = 1f;
            m_Image.ChevronCap = ArcCap.Round;
            m_Image.ChevronJoin = StrokeJoin.Miter;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                Assert.That(mesh.bounds.min.x, Is.GreaterThanOrEqualTo(-50.01f));
                Assert.That(mesh.bounds.max.x, Is.LessThanOrEqualTo(50.01f));
                Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-30.01f));
                Assert.That(mesh.bounds.max.y, Is.LessThanOrEqualTo(30.01f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void ChevronRoundCapAndJoinAddGeometry()
        {
            m_Image.Shape = ProceduralShape.Chevron;
            m_Image.Direction = ShapeDirection.Right;
            m_Image.ChevronThickness = 8f;
            m_Image.ChevronCap = ArcCap.Flat;
            m_Image.ChevronJoin = StrokeJoin.Miter;
            Mesh angularMesh = BuildMesh();

            m_Image.ChevronCap = ArcCap.Round;
            m_Image.ChevronJoin = StrokeJoin.Round;
            Mesh roundedMesh = BuildMesh();

            try
            {
                Assert.That(roundedMesh.vertexCount, Is.GreaterThan(angularMesh.vertexCount));
            }
            finally
            {
                Object.DestroyImmediate(angularMesh);
                Object.DestroyImmediate(roundedMesh);
            }
        }

        [Test]
        public void ChevronSpreadControlsEndpointDistance()
        {
            m_Image.Shape = ProceduralShape.Chevron;
            m_Image.Direction = ShapeDirection.Right;
            m_Image.ChevronThickness = 6f;
            m_Image.ChevronSpread = 1f;
            Mesh wideMesh = BuildMesh();

            m_Image.ChevronSpread = 0.25f;
            Mesh narrowMesh = BuildMesh();

            try
            {
                Assert.That(narrowMesh.bounds.size.y, Is.LessThan(wideMesh.bounds.size.y));
            }
            finally
            {
                Object.DestroyImmediate(wideMesh);
                Object.DestroyImmediate(narrowMesh);
            }
        }

        [TestCase(ShapeDirection.Up)]
        [TestCase(ShapeDirection.Right)]
        [TestCase(ShapeDirection.Down)]
        [TestCase(ShapeDirection.Left)]
        public void EquilateralChevronUsesEqualVirtualTriangleSides(
            ShapeDirection direction)
        {
            bool hasPath = ChevronGeometry.TryGetPath(
                new Rect(-100f, -30f, 200f, 60f),
                ChevronType.Equilateral,
                direction,
                8f,
                0.75f,
                StrokeJoin.Round,
                out Vector2 start,
                out Vector2 middle,
                out Vector2 end,
                out _);

            Assert.That(hasPath, Is.True);
            float firstArm = Vector2.Distance(start, middle);
            float secondArm = Vector2.Distance(middle, end);
            float virtualBase = Vector2.Distance(end, start);
            Assert.That(firstArm, Is.EqualTo(secondArm).Within(0.001f));
            Assert.That(secondArm, Is.EqualTo(virtualBase).Within(0.001f));
        }

        [Test]
        public void ChevronAntiAliasingUsesTransparentOuterVertices()
        {
            m_Image.Shape = ProceduralShape.Chevron;
            m_Image.ChevronThickness = 8f;
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1f;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(System.Array.Exists(mesh.colors32, color => color.a == 0), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(ShapeDirection.Up)]
        [TestCase(ShapeDirection.Right)]
        [TestCase(ShapeDirection.Down)]
        [TestCase(ShapeDirection.Left)]
        public void ChevronSupportsEveryDirection(ShapeDirection direction)
        {
            m_Image.Shape = ProceduralShape.Chevron;
            m_Image.Direction = direction;
            m_Image.ChevronThickness = 8f;
            m_Image.ChevronSpread = 0.75f;
            m_Image.ChevronCap = ArcCap.Round;
            m_Image.ChevronJoin = StrokeJoin.Round;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                Assert.That(mesh.bounds.min.x, Is.GreaterThanOrEqualTo(-50.01f));
                Assert.That(mesh.bounds.max.x, Is.LessThanOrEqualTo(50.01f));
                Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-30.01f));
                Assert.That(mesh.bounds.max.y, Is.LessThanOrEqualTo(30.01f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(StrokeJoin.Miter)]
        [TestCase(StrokeJoin.Round)]
        public void ChevronClampsExtremeSettings(StrokeJoin join)
        {
            m_Image.Shape = ProceduralShape.Chevron;
            m_Image.rectTransform.sizeDelta = new Vector2(200f, 10f);
            m_Image.ChevronThickness = 1000f;
            m_Image.ChevronSpread = 0f;
            m_Image.ChevronJoin = join;
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1000f;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                Assert.That(
                    System.Array.TrueForAll(
                        mesh.vertices,
                        vertex => !float.IsNaN(vertex.x)
                            && !float.IsNaN(vertex.y)
                            && !float.IsInfinity(vertex.x)
                            && !float.IsInfinity(vertex.y)),
                    Is.True);
                Assert.That(mesh.bounds.min.x, Is.GreaterThanOrEqualTo(-100.01f));
                Assert.That(mesh.bounds.max.x, Is.LessThanOrEqualTo(100.01f));
                Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-5.01f));
                Assert.That(mesh.bounds.max.y, Is.LessThanOrEqualTo(5.01f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(0f)]
        [TestCase(0.000001f)]
        [TestCase(0.00001f)]
        [TestCase(0.0001f)]
        [TestCase(0.001f)]
        [TestCase(0.01f)]
        public void ChevronSupportsZeroAndThinThickness(float thickness)
        {
            m_Image.Shape = ProceduralShape.Chevron;
            m_Image.ChevronThickness = thickness;
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1f;

            foreach (ArcCap cap in new[] { ArcCap.Flat, ArcCap.Round })
            {
                foreach (StrokeJoin join in new[] { StrokeJoin.Miter, StrokeJoin.Round })
                {
                    m_Image.ChevronCap = cap;
                    m_Image.ChevronJoin = join;

                    Mesh mesh = null;
                    Assert.DoesNotThrow(
                        () => mesh = BuildMesh(),
                        $"Thickness {thickness}, cap {cap}, join {join}");
                    Object.DestroyImmediate(mesh);
                }
            }
        }

        [Test]
        public void ArcWithFullSweepCreatesRing()
        {
            m_Image.Shape = ProceduralShape.Arc;
            m_Image.ArcThickness = 10f;
            m_Image.ArcSweepAngle = 360f;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.bounds.size.x, Is.EqualTo(100f).Within(0.01f));
                Assert.That(mesh.bounds.size.y, Is.EqualTo(60f).Within(0.01f));
                Assert.That(
                    System.Array.TrueForAll(
                        mesh.vertices,
                        vertex => new Vector2(vertex.x, vertex.y).sqrMagnitude > 1f),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void ArcUsesTopOriginAndClockwiseSweep()
        {
            m_Image.Shape = ProceduralShape.Arc;
            m_Image.ArcThickness = 10f;
            m_Image.ArcStartAngle = 0f;
            m_Image.ArcSweepAngle = 90f;
            m_Image.ArcCap = ArcCap.Flat;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.bounds.min.x, Is.GreaterThanOrEqualTo(-0.01f));
                Assert.That(mesh.bounds.max.x, Is.EqualTo(50f).Within(0.01f));
                Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-0.01f));
                Assert.That(mesh.bounds.max.y, Is.EqualTo(30f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void ArcSupportsCounterClockwiseSweep()
        {
            m_Image.Shape = ProceduralShape.Arc;
            m_Image.ArcThickness = 10f;
            m_Image.ArcStartAngle = 0f;
            m_Image.ArcSweepAngle = -90f;
            m_Image.ArcCap = ArcCap.Flat;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.bounds.min.x, Is.EqualTo(-50f).Within(0.01f));
                Assert.That(mesh.bounds.max.x, Is.LessThanOrEqualTo(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void RoundArcCapsAddGeometry()
        {
            m_Image.Shape = ProceduralShape.Arc;
            m_Image.ArcThickness = 10f;
            m_Image.ArcSweepAngle = 180f;
            m_Image.ArcCap = ArcCap.Flat;
            Mesh flatCapMesh = BuildMesh();

            m_Image.ArcCap = ArcCap.Round;
            Mesh roundCapMesh = BuildMesh();

            try
            {
                Assert.That(roundCapMesh.vertexCount, Is.GreaterThan(flatCapMesh.vertexCount));
            }
            finally
            {
                Object.DestroyImmediate(flatCapMesh);
                Object.DestroyImmediate(roundCapMesh);
            }
        }

        [Test]
        public void ArcAntiAliasingStaysInsideShapeBounds()
        {
            m_Image.Shape = ProceduralShape.Arc;
            m_Image.ArcThickness = 10f;
            m_Image.ArcStartAngle = -45f;
            m_Image.ArcSweepAngle = 270f;
            m_Image.ArcCap = ArcCap.Round;
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1f;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.bounds.min.x, Is.GreaterThanOrEqualTo(-50.01f));
                Assert.That(mesh.bounds.max.x, Is.LessThanOrEqualTo(50.01f));
                Assert.That(mesh.bounds.min.y, Is.GreaterThanOrEqualTo(-30.01f));
                Assert.That(mesh.bounds.max.y, Is.LessThanOrEqualTo(30.01f));
                Assert.That(System.Array.Exists(mesh.colors32, color => color.a == 0), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(ProceduralShape.RoundedRectangle)]
        [TestCase(ProceduralShape.Circle)]
        [TestCase(ProceduralShape.Quadrilateral)]
        [TestCase(ProceduralShape.Triangle)]
        public void AntiAliasingStaysInsideShapeBounds(ProceduralShape shape)
        {
            m_Image.Shape = shape;
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1f;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.bounds.size.x, Is.EqualTo(100f).Within(0.01f));
                Assert.That(mesh.bounds.size.y, Is.EqualTo(60f).Within(0.01f));
                Assert.That(System.Array.Exists(mesh.colors32, color => color.a == 0), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void QuadrilateralCornerOffsetsCreateParallelogram()
        {
            m_Image.Shape = ProceduralShape.Quadrilateral;
            m_Image.CornerOffsets = new CornerOffsets(
                new Vector2(20f, 0f),
                Vector2.zero,
                new Vector2(-20f, 0f),
                Vector2.zero);

            Mesh mesh = BuildMesh();
            try
            {
                Vector3[] vertices = mesh.vertices;
                float bottomCenter = (vertices[1].x + vertices[2].x) * 0.5f;
                float topCenter = (vertices[3].x + vertices[4].x) * 0.5f;
                float bottomWidth = vertices[2].x - vertices[1].x;
                float topWidth = vertices[3].x - vertices[4].x;

                Assert.That(topCenter - bottomCenter, Is.EqualTo(20f).Within(0.001f));
                Assert.That(topWidth, Is.EqualTo(bottomWidth).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void QuadrilateralCornerOffsetsCreateTrapezoid()
        {
            m_Image.Shape = ProceduralShape.Quadrilateral;
            m_Image.CornerOffsets = new CornerOffsets(
                new Vector2(10f, 0f),
                new Vector2(-10f, 0f),
                Vector2.zero,
                Vector2.zero);

            Mesh mesh = BuildMesh();
            try
            {
                Vector3[] vertices = mesh.vertices;
                float bottomWidth = vertices[2].x - vertices[1].x;
                float topWidth = vertices[3].x - vertices[4].x;

                Assert.That(bottomWidth, Is.EqualTo(100f).Within(0.001f));
                Assert.That(topWidth, Is.EqualTo(80f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void QuadrilateralSupportsBorderAndAntiAliasing()
        {
            Color32 fillColor = Color.blue;
            Color32 borderColor = Color.red;
            m_Image.Shape = ProceduralShape.Quadrilateral;
            m_Image.CornerOffsets = new CornerOffsets(
                new Vector2(18f, -2f),
                new Vector2(-6f, -4f),
                new Vector2(-16f, 3f),
                new Vector2(4f, 1f));
            m_Image.color = fillColor;
            m_Image.BorderEnabled = true;
            m_Image.BorderWidth = 4f;
            m_Image.BorderColor = borderColor;
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1f;

            Mesh mesh = BuildMesh();
            try
            {
                CollectionAssert.Contains(mesh.colors32, fillColor);
                CollectionAssert.Contains(mesh.colors32, borderColor);
                Assert.That(System.Array.Exists(mesh.colors32, color => color.a == 0), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(ProceduralShape.RoundedRectangle)]
        [TestCase(ProceduralShape.Circle)]
        [TestCase(ProceduralShape.Quadrilateral)]
        [TestCase(ProceduralShape.Triangle)]
        public void EdgeEffectAddsDirectionalGradient(ProceduralShape shape)
        {
            m_Image.Shape = shape;
            m_Image.CornerRadius = 12f;
            Mesh baseMesh = BuildMesh();

            Color32 effectColor = new Color(1f, 0f, 0f, 0.5f);
            m_Image.EdgeEffectEnabled = true;
            m_Image.EdgeEffectWidth = 6f;
            m_Image.EdgeEffectDirection = Vector2.down;
            m_Image.EdgeEffectColor = effectColor;
            Mesh effectMesh = BuildMesh();

            try
            {
                Assert.That(effectMesh.vertexCount, Is.GreaterThan(baseMesh.vertexCount));
                Assert.That(
                    System.Array.Exists(
                        effectMesh.colors32,
                        color => color.r == effectColor.r
                            && color.g == effectColor.g
                            && color.b == effectColor.b
                            && color.a > 0),
                    Is.True);
                Assert.That(
                    System.Array.Exists(
                        effectMesh.colors32,
                        color => color.r == effectColor.r
                            && color.g == effectColor.g
                            && color.b == effectColor.b
                            && color.a == 0),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(baseMesh);
                Object.DestroyImmediate(effectMesh);
            }
        }

        [Test]
        public void EdgeEffectColorsOnlyTheRequestedRectangleSide()
        {
            m_Image.Shape = ProceduralShape.RoundedRectangle;
            m_Image.CornerRadius = 0f;
            Mesh baseMesh = BuildMesh();

            m_Image.EdgeEffectEnabled = true;
            m_Image.EdgeEffectWidth = 6f;
            m_Image.EdgeEffectDirection = Vector2.down;
            m_Image.EdgeEffectColor = new Color(0f, 0f, 0f, 0.5f);
            Mesh effectMesh = BuildMesh();

            try
            {
                Color32[] colors = effectMesh.colors32;
                int outerEffectStart = baseMesh.vertexCount;
                Assert.That(colors[outerEffectStart].a, Is.GreaterThan(0));
                Assert.That(colors[outerEffectStart + 1].a, Is.GreaterThan(0));
                Assert.That(colors[outerEffectStart + 2].a, Is.Zero);
                Assert.That(colors[outerEffectStart + 3].a, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(baseMesh);
                Object.DestroyImmediate(effectMesh);
            }
        }

        [Test]
        public void QuadrilateralClampsIntersectingCornerOffsets()
        {
            m_Image.Shape = ProceduralShape.Quadrilateral;
            m_Image.rectTransform.sizeDelta = new Vector2(0.01f, 0.01f);
            m_Image.CornerOffsets = new CornerOffsets(
                new Vector2(1000f, -1000f),
                new Vector2(-1000f, -1000f),
                new Vector2(-1000f, 1000f),
                new Vector2(1000f, 1000f));
            m_Image.BorderEnabled = true;
            m_Image.BorderWidth = 1000f;
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1f;

            Mesh mesh = BuildMesh();
            try
            {
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                Assert.That(
                    System.Array.TrueForAll(
                        mesh.vertices,
                        vertex => !float.IsNaN(vertex.x)
                            && !float.IsNaN(vertex.y)
                            && !float.IsInfinity(vertex.x)
                            && !float.IsInfinity(vertex.y)),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase(ProceduralShape.RoundedRectangle)]
        [TestCase(ProceduralShape.Circle)]
        [TestCase(ProceduralShape.Quadrilateral)]
        [TestCase(ProceduralShape.Triangle)]
        public void BorderInnerEdgeUsesAntiAliasing(ProceduralShape shape)
        {
            m_Image.Shape = shape;
            m_Image.CornerRadius = 12f;
            m_Image.BorderEnabled = true;
            m_Image.BorderWidth = 4f;

            Mesh hardEdgeMesh = BuildMesh();
            m_Image.AntiAliasingEnabled = true;
            m_Image.AntiAliasingWidth = 1f;
            Mesh antiAliasedMesh = BuildMesh();

            try
            {
                int perimeterPointCount = (hardEdgeMesh.vertexCount - 1) / 3;
                Assert.That(
                    antiAliasedMesh.vertexCount,
                    Is.EqualTo(hardEdgeMesh.vertexCount + perimeterPointCount * 4));
            }
            finally
            {
                Object.DestroyImmediate(hardEdgeMesh);
                Object.DestroyImmediate(antiAliasedMesh);
            }
        }

        [Test]
        public void CornerRadiiAreScaledWithoutChangingTheirRatio()
        {
            const float tolerance = 0.001f;
            CornerRadii radii = new CornerRadii(80f, 40f, 20f, 10f);

            CornerRadii normalized = radii.Normalized(100f, 50f);

            Assert.That(normalized.TopLeft + normalized.BottomLeft, Is.LessThanOrEqualTo(50f + tolerance));
            Assert.That(normalized.TopRight + normalized.BottomRight, Is.LessThanOrEqualTo(50f + tolerance));
            Assert.That(normalized.TopLeft / normalized.TopRight, Is.EqualTo(2f).Within(tolerance));
        }

        [Test]
        public void NegativePublicValuesAreClampedToZero()
        {
            m_Image.CornerRadius = -1f;
            m_Image.BorderWidth = -1f;
            m_Image.AntiAliasingWidth = -1f;
            m_Image.EdgeEffectWidth = -1f;
            m_Image.ArcThickness = -1f;
            m_Image.ArcSweepAngle = 500f;
            m_Image.ChevronThickness = -1f;
            m_Image.ChevronSpread = 2f;

            Assert.That(m_Image.CornerRadius, Is.Zero);
            Assert.That(m_Image.BorderWidth, Is.Zero);
            Assert.That(m_Image.AntiAliasingWidth, Is.Zero);
            Assert.That(m_Image.EdgeEffectWidth, Is.Zero);
            Assert.That(m_Image.ArcThickness, Is.Zero);
            Assert.That(m_Image.ArcSweepAngle, Is.EqualTo(360f));
            Assert.That(m_Image.ChevronThickness, Is.Zero);
            Assert.That(m_Image.ChevronSpread, Is.EqualTo(1f));
        }

        private Mesh BuildMesh()
        {
            using (VertexHelper vertexHelper = new VertexHelper())
            {
                m_Image.PopulateMesh(vertexHelper);

                Mesh mesh = new Mesh();
                vertexHelper.FillMesh(mesh);
                return mesh;
            }
        }
    }
}
