using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormTickMovementTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void TickSearchingTransitionsIntoWalkingOrHopping()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetRandom(form, 7);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Searching);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.True(NimvioFormTestState.GetBehavior(form) is NimvioBehavior.Walking or NimvioBehavior.Hopping);
        });
    }

    [Fact]
    public void TickWalkingArrivesAndSitsWhenNearTarget()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var center = form.WorldCenter;
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Walking);
            NimvioFormTestState.SetTarget(form, new PointF(center.X + 5, center.Y));
            NimvioFormTestState.SetBehaviorTicks(form, 50);

            // Act
            NimvioFormTestState.Tick(form, 30);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickHoppingMovesTowardTarget()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Hopping);
            NimvioFormTestState.SetTarget(form, new PointF(form.Left + 400, form.Top + 200));
            NimvioFormTestState.SetBehaviorTicks(form, 200);
            var before = form.Location;

            // Act
            NimvioFormTestState.Tick(form, 5);

            // Assert
            Assert.NotEqual(before, form.Location);
        });
    }

    [Fact]
    public void TickFleeingCursorEventuallySits()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetLastCursorPosition(form, Cursor.Position);
            NimvioFormTestState.SetCursorInteractionCooldown(form, 999);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.FleeingCursor);
            NimvioFormTestState.SetTarget(form, new PointF(form.Left - 500, form.Top - 500));
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickCompetingMovesTowardOpponentOffset()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = _scenario.CreateContext(settings, createInitialForms: true).Forms[0];
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Competing);
            NimvioFormTestState.SetTarget(form, new PointF(form.WorldCenter.X + 120, form.WorldCenter.Y));
            NimvioFormTestState.SetBehaviorTicks(form, 200);
            var before = form.Left;

            // Act
            NimvioFormTestState.Tick(form, 8);

            // Assert
            Assert.NotEqual(before, form.Left);
        });
    }

    [Fact]
    public void TickWatchingYouTubeMovesThenRestsNearTarget()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            var window = new WindowSnapshot(new IntPtr(77), new Rectangle(80, 80, 700, 420), "chrome", "YouTube");
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.BeginYouTubeWatching(window, 0, 1);
            NimvioFormTestState.SetPerchedWindow(form, IntPtr.Zero);

            // Act
            NimvioFormTestState.Tick(form, 15);

            // Assert
            Assert.Equal(NimvioBehavior.WatchingYouTube, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickThrownAndFallingPhysicsEventuallySettle()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetRandom(form, 3);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Thrown);
            NimvioFormTestState.SetVelocity(form, new PointF(8, -4));
            NimvioFormTestState.SetBehaviorTicks(form, 5);

            // Act
            NimvioFormTestState.Tick(form, 20);

            // Assert
            Assert.True(NimvioFormTestState.GetBehavior(form) is NimvioBehavior.Sad or NimvioBehavior.Angry);

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Falling);
            NimvioFormTestState.SetVelocity(form, new PointF(2, 4));
            NimvioFormTestState.SetBehaviorTicks(form, 80);

            // Act
            NimvioFormTestState.Tick(form, 30);

            // Assert
        });
    }

    [Fact]
    public void TickPeekingApproachesThenSits()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Peeking);
            NimvioFormTestState.SetTarget(form, form.WorldCenter);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TickSlidingSlowsDownIntoSitting()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetLastCursorPosition(form, Cursor.Position);
            NimvioFormTestState.SetCursorInteractionCooldown(form, 999);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sliding);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(form));
        });
    }

    public void Dispose() => _scenario.Dispose();
}
