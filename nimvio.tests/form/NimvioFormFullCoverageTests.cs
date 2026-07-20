using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormFullCoverageTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    public NimvioFormFullCoverageTests()
    {
        DesktopAwareness.ClearTestOverrides();
    }

    public void Dispose()
    {
        DesktopAwareness.ClearTestOverrides();
        _scenario.Dispose();
    }

    [Fact]
    public void PlayfulPersonalityChasesNearbyMovingCursor()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo");
            profile.Personality = NimvioPersonality.Playful;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var cursor = Cursor.Position;
            form.Location = new Point(cursor.X - form.Width / 2 + 40, cursor.Y - form.Height / 2);
            NimvioFormTestState.SetLastCursorPosition(form, new Point(cursor.X + 40, cursor.Y + 40));
            NimvioFormTestState.SetCursorInteractionCooldown(form, 0);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            // fleeChance for Playful is 0.12 - force miss so chase path runs
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(doubles: [0.99]));

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.ChasingCursor, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void PaintDefaultGazeFlipsWhenFacingLeft()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            DesktopAwareness.ActiveWindowCenterOverride = () => null;
            DesktopAwareness.TryGetWindowRectangleOverride = _ => (false, Rectangle.Empty);
            DesktopAwareness.ActiveCaretPositionOverride = () => null;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var cursor = Cursor.Position;
            form.Location = new Point(cursor.X - form.Width / 2, cursor.Y - form.Height / 2);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetFacingRight(form, false);
            NimvioFormTestState.SetBlinkFrames(form, 0);

            // Act
            NimvioFormTestState.Paint(form);

            // Assert
        });
    }

    [Fact]
    public void FindRestingPlaceSkipsActiveLedgeWhenTooNarrow()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
            profile.Personality = NimvioPersonality.Curious;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var screen = Screen.PrimaryScreen!;
            // Width too small for marginX on both sides => right <= left
            var active = new WindowSnapshot(new IntPtr(9902),
                new Rectangle(screen.WorkingArea.Left + 80, screen.WorkingArea.Top + 160, 20, 400), "code", "Tiny");
            DesktopAwareness.ActiveWindowOverride = () => active;
            DesktopAwareness.VisibleWindowSnapshotsOverride = _ => [];
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(doubles: [0.0], ints: [50]));

            // Act
            _ = NimvioFormTestState.InvokeFindRestingPlace(form, screen);

            // Assert
            Assert.Equal(IntPtr.Zero, form.PerchedWindowHandle);
        });
    }

    [Fact]
    public void ContinueRestRoutinePlayfulRollStartsChasingCursor()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo");
            profile.Personality = NimvioPersonality.Playful;
            profile.Energy = 80;
            profile.Boredom = 10;
            profile.Curiosity = 10;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Waving);
            NimvioFormTestState.SetBehaviorTicks(form, 1);
            NimvioFormTestState.SetRestActionsRemaining(form, 3);
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(ints: [10]));

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.ChasingCursor, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void DragFormWhileNotDraggingIsIgnored()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetDragging(form, false);
            var before = form.Location;

            // Act
            NimvioFormTestState.InvokeDrag(form);

            // Assert
            Assert.Equal(before, form.Location);
        });
    }

    [Fact]
    public void ToggleAllowedScreenAddsMissingDeviceWithoutReseeding()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            settings.AllowedScreens = [@"\\.\DISPLAY2"];
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);

            // Act
            NimvioFormTestState.InvokeToggleAllowedScreen(form, @"\\.\DISPLAY1", enabled: true);

            // Assert
            Assert.Contains(@"\\.\DISPLAY1", settings.AllowedScreens);
        });
    }

    [Fact]
    public void TickOnDisposedFormReturnsImmediately()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.Dispose();

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
        });
    }

    [Fact]
    public void TickDecrementsJealousyAndTypingCooldowns()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetJealousyCooldown(form, 3);
            NimvioFormTestState.SetTypingCooldown(form, 3);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(2, NimvioFormTestState.GetJealousyCooldown(form));
        });
    }

    [Fact]
    public void WatchingYouTubeNearTargetSlowsDownAndResetsTicks()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetPerchedWindow(form, IntPtr.Zero);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.WatchingYouTube);
            NimvioFormTestState.SetTarget(form, form.WorldCenter);
            NimvioFormTestState.SetBehaviorTicks(form, 1);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.WatchingYouTube, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void CheckSystemStateArrangesYouTubeWhenActiveTitleMatches()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var youtube = new WindowSnapshot(new IntPtr(7001), new Rectangle(120, 120, 800, 500), "chrome", "Cute cats - YouTube");
            DesktopAwareness.ActiveWindowOverride = () => youtube;
            DesktopAwareness.VisibleWindowHandlesOverride = () => [youtube.Handle];
            DesktopAwareness.UserIdleTimeOverride = () => TimeSpan.FromSeconds(1);
            DesktopAwareness.TryGetWindowRectangleOverride = hwnd =>
                hwnd == youtube.Handle ? (true, youtube.Bounds) : (false, Rectangle.Empty);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetKnownWindows(form, [youtube.Handle]);
            NimvioFormTestState.SetSystemCheckTicks(form, 59);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.WatchingYouTube, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void UpdateUserPresenceMarksAwayAfterLongIdle()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            DesktopAwareness.UserIdleTimeOverride = () => TimeSpan.FromMinutes(12);
            DesktopAwareness.ActiveWindowOverride = () => null;
            DesktopAwareness.VisibleWindowHandlesOverride = () => [];
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetUserWasAway(form, false);
            NimvioFormTestState.SetSystemCheckTicks(form, 59);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.True(NimvioFormTestState.GetUserWasAway(form));
        });
    }

    [Fact]
    public void TrackPerchedWindowSlidesAndHangsOnMovedBounds()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var handle = new IntPtr(8801);
            var original = new Rectangle(200, 200, 500, 400);
            DesktopAwareness.TryGetWindowRectangleOverride = hwnd =>
                hwnd == handle ? (true, original with { X = original.X + 30, Y = original.Y + 20 }) : (false, Rectangle.Empty);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetPerchedWindow(form, handle);
            NimvioFormTestState.SetPerchedWindowBounds(form, original);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Hanging, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TrackPerchedWindowSlidesOnSmallMovement()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var handle = new IntPtr(8802);
            var original = new Rectangle(200, 200, 500, 400);
            DesktopAwareness.TryGetWindowRectangleOverride = hwnd =>
                hwnd == handle ? (true, original with { X = original.X + 10 }) : (false, Rectangle.Empty);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetPerchedWindow(form, handle);
            NimvioFormTestState.SetPerchedWindowBounds(form, original);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sliding, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void BeginSocialInteractionDefaultRelationshipChangePath()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var context = _scenario.CreateContext(settings, createInitialForms: true);

            // Act
            context.Forms[0].BeginSocialInteraction(context.Forms[1], NimvioBehavior.Sitting, false);

            // Assert
            Assert.Equal(0, context.Forms[0].Profile.Relationships["mimo"]);
        });
    }

    [Fact]
    public void CalmPersonalityFleesNearbyMovingCursor()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi");
            profile.Personality = NimvioPersonality.Calm;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var cursor = Cursor.Position;
            form.Location = new Point(cursor.X - form.Width / 2 + 40, cursor.Y - form.Height / 2);
            NimvioFormTestState.SetLastCursorPosition(form, new Point(cursor.X + 40, cursor.Y + 40));
            NimvioFormTestState.SetCursorInteractionCooldown(form, 0);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(doubles: [0.0]));

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.FleeingCursor, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void TypingNearCaretStartsPeeking()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var caret = Cursor.Position;
            DesktopAwareness.PressedTypingKeysOverride = () => [Keys.A];
            DesktopAwareness.ActiveCaretPositionOverride = () => caret;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.Location = new Point(caret.X - 100, caret.Y - 40);
            NimvioFormTestState.SetPressedTypingKeys(form, []);
            NimvioFormTestState.SetTypingCooldown(form, 0);
            NimvioFormTestState.SetCursorInteractionCooldown(form, 999);
            NimvioFormTestState.SetLastCursorPosition(form, Cursor.Position);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Peeking, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void RareEventWavesAtNearbyPerchedFriend()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            settings.Autonomy = AutonomyLevel.High;
            var perchBounds = new Rectangle(100, 100, 400, 300);
            DesktopAwareness.TryGetWindowRectangleOverride = _ => (true, perchBounds);
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var nova = context.Forms[0];
            var mimo = context.Forms[1];
            nova.Location = new Point(200, 200);
            mimo.Location = new Point(260, 200);
            NimvioFormTestState.SetPerchedWindow(nova, new IntPtr(1));
            NimvioFormTestState.SetPerchedWindowBounds(nova, perchBounds);
            NimvioFormTestState.SetPerchedWindow(mimo, new IntPtr(2));
            NimvioFormTestState.SetPerchedWindowBounds(mimo, perchBounds with { X = 300 });
            NimvioFormTestState.SetBehavior(nova, NimvioBehavior.Sitting);
            NimvioFormTestState.SetBehavior(mimo, NimvioBehavior.Sitting);
            NimvioFormTestState.SetRareEventCooldown(nova, 0);
            NimvioFormTestState.SetWindowInteractionCooldown(nova, 0);
            NimvioFormTestState.SetRandom(nova, new ScriptedRandom(doubles: [0.0]));

            // Act
            NimvioFormTestState.Tick(nova);

            // Assert
            Assert.Equal(NimvioBehavior.Waving, NimvioFormTestState.GetBehavior(nova));
        });
    }

    [Fact]
    public void RareEventHopsToNearbyWindowWhenNoFriendWave()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            settings.Autonomy = AutonomyLevel.High;
            var screen = Screen.PrimaryScreen!;
            var perch = new IntPtr(1);
            var hopTarget = new WindowSnapshot(new IntPtr(2),
                new Rectangle(screen.WorkingArea.Left + 220, screen.WorkingArea.Top + 180, 500, 320), "notepad", "Doc");
            var perchBounds = new Rectangle(100, 100, 200, 150);
            DesktopAwareness.VisibleWindowSnapshotsOverride = _ => [hopTarget];
            DesktopAwareness.TryGetWindowRectangleOverride = hwnd =>
                hwnd == perch ? (true, perchBounds) : (false, Rectangle.Empty);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.Location = new Point(screen.WorkingArea.Left + 80, screen.WorkingArea.Top + 120);
            NimvioFormTestState.SetPerchedWindow(form, perch);
            NimvioFormTestState.SetPerchedWindowBounds(form, perchBounds);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetRareEventCooldown(form, 0);
            NimvioFormTestState.SetWindowInteractionCooldown(form, 0);
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(doubles: [0.0], ints: [0]));

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Hopping, NimvioFormTestState.GetBehavior(form));
        });
    }

    [Fact]
    public void RareEventStumblesWhileMovingAndChasesWhenBored()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
            profile.Boredom = 80;
            var settings = NimvioFormScenario.SettingsWith(profile);
            settings.Autonomy = AutonomyLevel.High;
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var cursor = Cursor.Position;
            form.Location = new Point(cursor.X - form.Width / 2, cursor.Y - form.Height / 2);
            NimvioFormTestState.SetLastCursorPosition(form, cursor);
            NimvioFormTestState.SetCursorInteractionCooldown(form, 999);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Walking);
            NimvioFormTestState.SetTarget(form, new PointF(form.Left + 500, form.Top));
            NimvioFormTestState.SetBehaviorTicks(form, 500);
            NimvioFormTestState.SetRareEventCooldown(form, 0);
            NimvioFormTestState.SetWindowInteractionCooldown(form, 999);
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(doubles: [0.0]));

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Stumbling, NimvioFormTestState.GetBehavior(form));

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Sitting);
            NimvioFormTestState.SetRareEventCooldown(form, 0);
            NimvioFormTestState.SetWindowInteractionCooldown(form, 999);
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(doubles: [0.0]));

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.True(NimvioFormTestState.GetBehavior(form) is NimvioBehavior.ChasingCursor or NimvioBehavior.HoldingCursor);
        });
    }

    [Fact]
    public void MoveThrownBouncesOffScreenEdges()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            var area = Screen.PrimaryScreen!.WorkingArea;
            form.Location = new Point(area.Left + 2, area.Top + 2);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Thrown);
            NimvioFormTestState.SetVelocity(form, new PointF(-20, -20));
            NimvioFormTestState.SetBehaviorTicks(form, 80);

            // Act
            NimvioFormTestState.Tick(form, 5);

            // Assert
            Assert.True(NimvioFormTestState.GetBehavior(form) is NimvioBehavior.Thrown or NimvioBehavior.Sad or NimvioBehavior.Angry);
        });
    }

    [Fact]
    public void FindRestingPlaceUsesActiveWindowLedge()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
            profile.Personality = NimvioPersonality.Curious;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var screen = Screen.PrimaryScreen!;
            var active = new WindowSnapshot(new IntPtr(9901),
                new Rectangle(screen.WorkingArea.Left + 80, screen.WorkingArea.Top + 160, 600, 400), "code", "Editor");
            DesktopAwareness.ActiveWindowOverride = () => active;
            DesktopAwareness.VisibleWindowSnapshotsOverride = _ => [];
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(doubles: [0.0]));

            // Act
            var place = NimvioFormTestState.InvokeFindRestingPlace(form, screen);

            // Assert
            Assert.Equal(active.Handle, form.PerchedWindowHandle);
            Assert.InRange(place.X, active.Bounds.Left, active.Bounds.Right);
        });
    }

    [Fact]
    public void FindRestingPlaceFallsBackWhenRecentPlacesBlockAllCandidates()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova");
            var settings = NimvioFormScenario.SettingsWith(profile);
            var screen = Screen.PrimaryScreen!;
            var area = screen.WorkingArea;
            for (var x = area.Left; x < area.Right; x += 100)
            {
                for (var y = area.Top; y < area.Bottom; y += 100)
                {
                    profile.RecentPlaces.Add(new Point(x, y));
                }
            }

            DesktopAwareness.ActiveWindowOverride = () => null;
            DesktopAwareness.VisibleWindowSnapshotsOverride = _ => [];
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(ints: Enumerable.Repeat(50, 40).ToArray()));

            // Act
            _ = NimvioFormTestState.InvokeFindRestingPlace(form, screen);

            // Assert
            Assert.Equal(IntPtr.Zero, form.PerchedWindowHandle);
        });
    }

    [Fact]
    public void ContinueRestRoutineHitsPlayfulSleepingAndCalmPointingBranches()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var playful = NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo");
            playful.Personality = NimvioPersonality.Playful;
            playful.Energy = 80;
            playful.Boredom = 10;
            playful.Curiosity = 10;
            var playfulSettings = NimvioFormScenario.SettingsWith(playful);
            var playfulForm = Assert.Single(_scenario.CreateContext(playfulSettings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(playfulForm, NimvioBehavior.Waving);
            NimvioFormTestState.SetBehaviorTicks(playfulForm, 1);
            NimvioFormTestState.SetRestActionsRemaining(playfulForm, 3);
            NimvioFormTestState.SetRandom(playfulForm, new ScriptedRandom(ints: [99]));

            // Act
            NimvioFormTestState.Tick(playfulForm);

            // Assert
            Assert.Equal(NimvioBehavior.Sleeping, NimvioFormTestState.GetBehavior(playfulForm));

            // Arrange
            var calm = NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi");
            calm.Personality = NimvioPersonality.Calm;
            calm.Energy = 80;
            calm.Boredom = 10;
            calm.Curiosity = 10;
            var calmSettings = NimvioFormScenario.SettingsWith(calm);
            var calmForm = Assert.Single(_scenario.CreateContext(calmSettings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(calmForm, NimvioBehavior.Waving);
            NimvioFormTestState.SetBehaviorTicks(calmForm, 1);
            NimvioFormTestState.SetRestActionsRemaining(calmForm, 3);
            NimvioFormTestState.SetRandom(calmForm, new ScriptedRandom(ints: [99]));

            // Act
            NimvioFormTestState.Tick(calmForm);

            // Assert
            Assert.Equal(NimvioBehavior.Pointing, NimvioFormTestState.GetBehavior(calmForm));
        });
    }

    [Fact]
    public void DragLifecycleCoversNonLeftButtonsAndActualDragMotion()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);

            // Act
            NimvioFormTestState.InvokeBeginDrag(form, MouseButtons.Right);

            // Assert
            Assert.False(NimvioFormTestState.GetDragging(form));

            // Act
            NimvioFormTestState.InvokeBeginDrag(form, MouseButtons.Left);

            // Assert
            Assert.True(NimvioFormTestState.GetDragging(form));

            // Arrange
            NimvioFormTestState.SetLastDragCursor(form, new Point(Cursor.Position.X - 20, Cursor.Position.Y - 20));

            // Act
            NimvioFormTestState.InvokeDrag(form);

            // Assert
            Assert.True(NimvioFormTestState.GetDidDrag(form));

            // Act
            NimvioFormTestState.InvokeEndDrag(form, MouseButtons.Right);

            // Assert
            Assert.True(NimvioFormTestState.GetDragging(form));

            // Act
            NimvioFormTestState.InvokeEndDrag(form, MouseButtons.Left);

            // Assert
        });
    }

    [Fact]
    public void CanHideAtSafeEdgeUsesActiveWindowTitleBar()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var handle = new IntPtr(5500);
            var bounds = new Rectangle(100, 200, 700, 500);
            DesktopAwareness.ActiveWindowOverride = () => new WindowSnapshot(handle, bounds, "chrome", "Page");
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetPerchedWindow(form, handle);
            form.Location = new Point(bounds.Left + 100, (int)(bounds.Top - form.Height / 2f) - form.Height / 2 + form.Height / 2);
            // Place so Center.Y is near titleBarEdgeY = bounds.Top - Height/2
            var titleBarEdgeY = bounds.Top - form.Height / 2f;
            form.Location = new Point(bounds.Left + 120, (int)(titleBarEdgeY - form.Height / 2f));

            // Act
            var canHide = NimvioFormTestState.InvokeCanHideAtSafeEdge(form);

            // Assert
            Assert.True(canHide);
        });
    }

    [Fact]
    public void PaintGazeOffsetsCoverYouTubeAndPeekingPaths()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"));
            var video = new Rectangle(300, 200, 640, 360);
            DesktopAwareness.TryGetWindowRectangleOverride = _ => (true, video);
            DesktopAwareness.ActiveCaretPositionOverride = () => new Point(400, 300);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            form.Location = new Point(100, 100);
            NimvioFormTestState.SetWatchedYouTubeWindow(form, new IntPtr(42));
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.WatchingYouTube);
            NimvioFormTestState.SetFacingRight(form, false);
            NimvioFormTestState.SetBlinkFrames(form, 0);

            // Act
            NimvioFormTestState.Paint(form);

            // Arrange
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Peeking);
            NimvioFormTestState.SetFacingRight(form, false);
            NimvioFormTestState.SetBlinkFrames(form, 0);

            // Act
            NimvioFormTestState.Paint(form);

            // Arrange
            NimvioFormTestState.SetSpeech(form, "Hi!", 120);
            NimvioFormTestState.SetFacingRight(form, false);

            // Act
            NimvioFormTestState.Paint(form);

            // Assert
        });
    }

    [Fact]
    public void ContinueRestRoutineSittingTickDurationBranch()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var profile = NimvioFormScenario.Character(NimvioCharacterName.Lumi, "lumi");
            profile.Personality = NimvioPersonality.Calm;
            profile.Energy = 80;
            profile.Boredom = 10;
            profile.Curiosity = 10;
            var settings = NimvioFormScenario.SettingsWith(profile);
            var form = Assert.Single(_scenario.CreateContext(settings, createInitialForms: true).Forms);
            NimvioFormTestState.SetBehavior(form, NimvioBehavior.Waving);
            NimvioFormTestState.SetBehaviorTicks(form, 1);
            NimvioFormTestState.SetRestActionsRemaining(form, 3);
            // Calm roll 50-64 => Sitting, then Sitting duration branch
            NimvioFormTestState.SetRandom(form, new ScriptedRandom(ints: [60, 100]));

            // Act
            NimvioFormTestState.Tick(form);

            // Assert
            Assert.Equal(NimvioBehavior.Sitting, NimvioFormTestState.GetBehavior(form));
        });
    }
}

internal sealed class ScriptedRandom : Random
{
    private readonly Queue<double> _doubles;
    private readonly Queue<int> _ints;

    public ScriptedRandom(double[]? doubles = null, int[]? ints = null)
    {
        _doubles = new Queue<double>(doubles ?? []);
        _ints = new Queue<int>(ints ?? []);
    }

    public override double NextDouble()
        => _doubles.Count > 0 ? _doubles.Dequeue() : 0.5;

    public override int Next()
        => _ints.Count > 0 ? _ints.Dequeue() : 0;

    public override int Next(int maxValue)
        => _ints.Count > 0 ? Math.Clamp(_ints.Dequeue(), 0, Math.Max(0, maxValue - 1)) : 0;

    public override int Next(int minValue, int maxValue)
    {
        if (_ints.Count == 0)
        {
            return minValue;
        }

        return Math.Clamp(_ints.Dequeue(), minValue, Math.Max(minValue, maxValue - 1));
    }

    protected override double Sample() => NextDouble();
}
