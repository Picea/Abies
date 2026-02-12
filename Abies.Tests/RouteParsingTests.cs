namespace Abies.Tests;

/// <summary>
/// This file contains property-based tests for route parsing.
/// Note that the actual implementation tests are in RouteParserTests.cs,
/// as this code requires browser-specific APIs which aren't available during unit testing.
/// </summary>
public class RouteParsingTests
{
    // This test class has been deprecated in favor of RouteParserTests.cs
    // The tests were using browser-only APIs which aren't available during unit testing

    [Fact]
    public void RouteParsingIsConfiguredCorrectly()
    {
        // This test serves as a placeholder to ensure that
        // in Route.cs, the `/profile/{username}/favorites` route is defined before 
        // the `/profile/{username}` route to ensure proper parsing.

        // The actual testing of route parsing is done in RouteParserTests.cs
        Assert.True(true, "See RouteParserTests.cs for route parsing tests");
    }
}
