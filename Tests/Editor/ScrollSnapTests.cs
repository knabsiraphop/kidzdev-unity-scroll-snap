using NUnit.Framework;
using KidzDev.Unity.ScrollSnap;

namespace KidzDev.Unity.ScrollSnap.Tests
{
    public class ClampOrWrapPageTests
    {
        private static int ClampPage(int page, int count) =>
            count <= 0 ? 0 : System.Math.Clamp(page, 0, count - 1);

        private static int WrapPage(int page, int count) =>
            count <= 0 ? 0 : ((page % count) + count) % count;

        [TestCase(0, 5, 0)]
        [TestCase(4, 5, 4)]
        [TestCase(5, 5, 4)]
        [TestCase(-1, 5, 0)]
        public void Clamp_StaysWithinBounds(int page, int count, int expected) =>
            Assert.AreEqual(expected, ClampPage(page, count));

        [TestCase(0, 5, 0)]
        [TestCase(5, 5, 0)]
        [TestCase(-1, 5, 4)]
        [TestCase(6, 5, 1)]
        public void Wrap_WrapsCorrectly(int page, int count, int expected) =>
            Assert.AreEqual(expected, WrapPage(page, count));

        [TestCase(0)]
        [TestCase(-1)]
        public void ZeroOrNegativeCount_ReturnsZero(int count)
        {
            Assert.AreEqual(0, ClampPage(3, count));
            Assert.AreEqual(0, WrapPage(3, count));
        }
    }

    public class SnapMathAlignOffsetTests
    {
        // Start alignment: item leading edge aligns with viewport leading edge.
        // item center=50, extent=100 → leading=0. viewport center=270, extent=540 → leading=0. delta=0.
        [Test]
        public void Start_ItemLeadingEdgeAtViewportLeading_DeltaZero()
        {
            float delta = SnapMath.AlignOffset(50f, 100f, 270f, 540f, SnapAlignment.Start);
            Assert.AreEqual(0f, delta, 0.001f);
        }

        // Start: item shifted right by 100 (center=150). Leading=100. VP leading=0. delta=-100.
        [Test]
        public void Start_ItemShiftedRight_DeltaNegative()
        {
            float delta = SnapMath.AlignOffset(150f, 100f, 270f, 540f, SnapAlignment.Start);
            Assert.AreEqual(-100f, delta, 0.001f);
        }

        // Center: item center aligns with viewport center.
        // Both centered — delta should be 0.
        [Test]
        public void Center_ItemAndViewportCentered_DeltaZero()
        {
            float delta = SnapMath.AlignOffset(270f, 100f, 270f, 540f, SnapAlignment.Center);
            Assert.AreEqual(0f, delta, 0.001f);
        }

        // Center: item center at 0, viewport center at 270. delta=270.
        [Test]
        public void Center_ItemAtOrigin_DeltaEqualsViewportCenter()
        {
            float delta = SnapMath.AlignOffset(0f, 100f, 270f, 540f, SnapAlignment.Center);
            Assert.AreEqual(270f, delta, 0.001f);
        }

        // End alignment: item trailing edge aligns with viewport trailing edge.
        // item center=50, extent=100 → trailing=100. VP trailing=540. delta=440.
        [Test]
        public void End_ItemTrailingToViewportTrailing()
        {
            float delta = SnapMath.AlignOffset(50f, 100f, 270f, 540f, SnapAlignment.End);
            Assert.AreEqual(440f, delta, 0.001f);
        }
    }

    public class SnapMathFocusDistanceTests
    {
        [Test]
        public void FocusDistance_ItemAtCenter_ReturnsZero()
        {
            Assert.AreEqual(0f, SnapMath.FocusDistance01(100f, 100f, 200f), 0.001f);
        }

        [Test]
        public void FocusDistance_ItemAtRange_ReturnsOne()
        {
            Assert.AreEqual(1f, SnapMath.FocusDistance01(300f, 100f, 200f), 0.001f);
        }

        [Test]
        public void FocusDistance_ItemBeyondRange_ClampedToOne()
        {
            Assert.AreEqual(1f, SnapMath.FocusDistance01(500f, 100f, 200f), 0.001f);
        }

        [Test]
        public void FocusDistance_ItemAtHalfRange_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, SnapMath.FocusDistance01(200f, 100f, 200f), 0.001f);
        }

        [Test]
        public void FocusDistance_ZeroRange_ReturnsZero()
        {
            Assert.AreEqual(0f, SnapMath.FocusDistance01(500f, 100f, 0f), 0.001f);
        }
    }
}
