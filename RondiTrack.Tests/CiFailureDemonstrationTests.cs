using Xunit;

namespace RondiTrack.Tests;

// Temporary test to demonstrate that GitHub Actions
// correctly detects failing automated tests.
public class CiFailureDemonstrationTests
{
    [Fact]
    public void CiPipeline_ShouldRejectFailingTests()
    {
        // Intentionally fail this test for Assignment 6.4 CI evidence.
        Assert.Fail("Intentional failure for Assignment 6.4 CI evidence.");
    }
}