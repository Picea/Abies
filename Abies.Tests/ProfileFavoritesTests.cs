using Xunit;
using System;

namespace Abies.Tests;

public class ProfileFavoritesTests
{
    [Fact]
    public void ProfileFavorites_Route_Type_Should_Exist()
    {
        // Verify ProfileFavorites route type exists in the routing namespace
        var routingAssembly = typeof(Abies.Conduit.Routing.Route).Assembly;
        var profileFavoritesType = routingAssembly.GetType("Abies.Conduit.Routing.Route+ProfileFavorites");
        
        Assert.NotNull(profileFavoritesType);
        Assert.True(profileFavoritesType.IsClass);
    }
    
    [Fact]
    public void ProfileFavorites_Handler_Should_Exist()
    {
        // Verify the ProfileFavorites handler method exists
        var handlersType = typeof(Abies.Conduit.Routing.Handlers);
        var handlerMethod = handlersType.GetMethod("ProfileFavorites");
        
        Assert.NotNull(handlerMethod);
        Assert.True(handlerMethod.IsStatic);
        Assert.Equal("ProfileFavorites", handlerMethod.Name);
    }
    
    [Fact]
    public void Profile_And_ProfileFavorites_Routes_Should_Be_Distinct_Types()
    {
        // Verify that Profile and ProfileFavorites are distinct route types
        var routingAssembly = typeof(Abies.Conduit.Routing.Route).Assembly;
        var profileType = routingAssembly.GetType("Abies.Conduit.Routing.Route+Profile");
        var profileFavoritesType = routingAssembly.GetType("Abies.Conduit.Routing.Route+ProfileFavorites");
        
        Assert.NotNull(profileType);
        Assert.NotNull(profileFavoritesType);
        Assert.NotEqual(profileType, profileFavoritesType);
    }
}
