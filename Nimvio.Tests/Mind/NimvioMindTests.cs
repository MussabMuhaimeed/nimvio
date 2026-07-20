using Nimvio;
using Xunit;

namespace Nimvio.Tests.Mind;

public sealed class NimvioMindTests
{
    [Fact]
    public void TickOnlyUpdatesStatsEverySixtiethCall()
    {
        // Arrange
        var profile = new NimvioProfile { Energy = 50, Boredom = 50, Curiosity = 50, Happiness = 50 };
        var mind = new NimvioMind(profile);

        // Act
        for (var i = 0; i < 59; i++)
        {
            mind.Tick(NimvioBehavior.Walking, ActivityLevel.Normal);
        }

        // Assert
        Assert.Equal(50, mind.Energy);
        Assert.Equal(50, mind.Boredom);
        Assert.Equal(50, mind.Curiosity);
        Assert.Equal(50, mind.Happiness);

        // Act
        mind.Tick(NimvioBehavior.Walking, ActivityLevel.Normal);

        // Assert
        Assert.NotEqual(50, mind.Energy);
    }

    [Fact]
    public void TickSleepingIncreasesEnergy()
    {
        // Arrange
        var profile = new NimvioProfile { Energy = 50 };

        // Act
        AdvanceTicks(new NimvioMind(profile), NimvioBehavior.Sleeping, ActivityLevel.Normal, 60);

        // Assert
        Assert.Equal(50.9f, profile.Energy);
    }

    [Fact]
    public void TickFeedingIncreasesEnergy()
    {
        // Arrange
        var profile = new NimvioProfile { Energy = 50 };

        // Act
        AdvanceTicks(new NimvioMind(profile), NimvioBehavior.Feeding, ActivityLevel.Normal, 60);

        // Assert
        Assert.Equal(50.42f, profile.Energy);
    }

    [Fact]
    public void TickSharingMilkIncreasesEnergy()
    {
        // Arrange
        var profile = new NimvioProfile { Energy = 50 };

        // Act
        AdvanceTicks(new NimvioMind(profile), NimvioBehavior.SharingMilk, ActivityLevel.Normal, 60);

        // Assert
        Assert.Equal(50.3f, profile.Energy);
    }

    [Fact]
    public void TickEnergeticActivityDrainsEnergyFasterWhileMoving()
    {
        // Arrange
        var normalProfile = new NimvioProfile { Energy = 50 };
        var energeticProfile = new NimvioProfile { Energy = 50 };
        var normalMind = new NimvioMind(normalProfile);
        var energeticMind = new NimvioMind(energeticProfile);

        // Act
        AdvanceTicks(normalMind, NimvioBehavior.Walking, ActivityLevel.Normal, 60);
        AdvanceTicks(energeticMind, NimvioBehavior.Walking, ActivityLevel.Energetic, 60);

        // Assert
        Assert.True(energeticMind.Energy < normalMind.Energy);
    }

    [Fact]
    public void TickSearchingAndInspectingReduceCuriosity()
    {
        // Arrange
        var searching = new NimvioProfile { Curiosity = 50 };
        var inspecting = new NimvioProfile { Curiosity = 50 };

        // Act
        AdvanceTicks(new NimvioMind(searching), NimvioBehavior.Searching, ActivityLevel.Normal, 60);
        AdvanceTicks(new NimvioMind(inspecting), NimvioBehavior.Inspecting, ActivityLevel.Normal, 60);

        // Assert
        Assert.Equal(49.45f, searching.Curiosity);
        Assert.Equal(49.45f, inspecting.Curiosity);
    }

    [Fact]
    public void ComfortedBoostsHappinessAndLowersBoredom()
    {
        // Arrange
        var profile = new NimvioProfile { Happiness = 40, Boredom = 40 };
        var mind = new NimvioMind(profile);

        // Act
        mind.Comforted();

        // Assert
        Assert.Equal(48, mind.Happiness);
        Assert.Equal(32, mind.Boredom);
    }

    [Fact]
    public void ExploredReducesCuriosityAndBoredom()
    {
        // Arrange
        var profile = new NimvioProfile { Curiosity = 50, Boredom = 50 };
        var mind = new NimvioMind(profile);

        // Act
        mind.Explored();

        // Assert
        Assert.Equal(32, mind.Curiosity);
        Assert.Equal(38, mind.Boredom);
    }

    [Fact]
    public void CaughtCursorImprovesMood()
    {
        // Arrange
        var profile = new NimvioProfile { Happiness = 50, Boredom = 50 };
        var mind = new NimvioMind(profile);

        // Act
        mind.CaughtCursor();

        // Assert
        Assert.Equal(60, mind.Happiness);
        Assert.Equal(38, mind.Boredom);
    }

    [Fact]
    public void SocialEventsAdjustHappinessAndBoredomWithinBounds()
    {
        // Arrange
        var startled = new NimvioProfile { Happiness = 2 };
        var ignored = new NimvioProfile { Happiness = 2 };
        var missed = new NimvioProfile { Happiness = 95 };
        var socialized = new NimvioProfile { Happiness = 95, Boredom = 2 };

        // Act
        new NimvioMind(startled).Startled();
        new NimvioMind(ignored).FeltIgnored();
        new NimvioMind(missed).MissedUser();
        new NimvioMind(socialized).Socialized();

        // Assert
        Assert.Equal(0, startled.Happiness);
        Assert.Equal(0, ignored.Happiness);
        Assert.Equal(100, missed.Happiness);
        Assert.Equal(98, socialized.Happiness);
        Assert.Equal(0, socialized.Boredom);
    }

    [Fact]
    public void TickCalmActivityIncreasesBoredomSlowerThanEnergeticWhileIdle()
    {
        // Arrange
        var calmProfile = new NimvioProfile { Boredom = 50 };
        var energeticProfile = new NimvioProfile { Boredom = 50 };

        // Act
        AdvanceTicks(new NimvioMind(calmProfile), NimvioBehavior.Sitting, ActivityLevel.Calm, 60);
        AdvanceTicks(new NimvioMind(energeticProfile), NimvioBehavior.Sitting, ActivityLevel.Energetic, 60);

        // Assert
        Assert.True(calmProfile.Boredom < energeticProfile.Boredom);
        Assert.Equal(50.21f, calmProfile.Boredom);
        Assert.Equal(50.35f, energeticProfile.Boredom);
    }

    [Fact]
    public void TickFunBehaviorIncreasesHappinessOnCadence()
    {
        // Arrange
        var profile = new NimvioProfile { Happiness = 50, Boredom = 50 };

        // Act
        AdvanceTicks(new NimvioMind(profile), NimvioBehavior.Waving, ActivityLevel.Normal, 60);

        // Assert
        Assert.Equal(50.085f, profile.Happiness);
        Assert.Equal(49.3f, profile.Boredom);
    }

    [Fact]
    public void TickIdleBehaviorRegeneratesEnergySlowly()
    {
        // Arrange
        var profile = new NimvioProfile { Energy = 50 };

        // Act
        AdvanceTicks(new NimvioMind(profile), NimvioBehavior.Sitting, ActivityLevel.Normal, 60);

        // Assert
        Assert.Equal(50.09f, profile.Energy);
    }

    private static void AdvanceTicks(NimvioMind mind, NimvioBehavior behavior, ActivityLevel activity, int count)
    {
        for (var i = 0; i < count; i++)
        {
            mind.Tick(behavior, activity);
        }
    }
}
