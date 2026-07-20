using Nimvio;
using Xunit;

namespace Nimvio.Tests.Form;

[Collection(nameof(FormTestsCollection))]
public sealed class NimvioFormExhaustiveSimulationTests : IDisposable
{
    private readonly NimvioFormScenario _scenario = new();

    [Fact]
    public void RandomizedTickAndPaintSimulationExercisesRemainingBranches()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            // Arrange
            var settings = NimvioFormScenario.SettingsWith(
                NimvioFormScenario.Character(NimvioCharacterName.Nova, "nova"),
                NimvioFormScenario.Character(NimvioCharacterName.Mimo, "mimo"));
            settings.Autonomy = AutonomyLevel.High;
            settings.Activity = ActivityLevel.Energetic;
            var context = _scenario.CreateContext(settings, createInitialForms: true);
            var form = context.Forms[0];
            var friend = context.Forms[1];
            var screen = Screen.PrimaryScreen!;

            // Act
            for (var seed = 0; seed < 320; seed++)
            {
                NimvioFormTestState.SetRandom(form, seed);
                form.Location = new Point(
                    screen.WorkingArea.Left + seed % 400,
                    screen.WorkingArea.Top + seed % 300);
                friend.Location = new Point(form.Left + 80, form.Top);
                NimvioFormTestState.SetBehavior(form, (NimvioBehavior)(seed % 31));
                NimvioFormTestState.SetBehaviorTicks(form, 1 + seed % 40);
                NimvioFormTestState.SetRestActionsRemaining(form, 1 + seed % 4);
                NimvioFormTestState.SetRareEventCooldown(form, 0);
                NimvioFormTestState.SetWindowInteractionCooldown(form, 0);
                NimvioFormTestState.SetSocialTicks(form, 1);
                NimvioFormTestState.SetSocialInteractionCooldown(form, 0);
                NimvioFormTestState.SetActiveAccessory(form, (ActiveAppAccessory)(seed % 4));
                NimvioFormTestState.Tick(form, 4);
                NimvioFormTestState.Paint(form);
                _ = NimvioFormTestState.InvokeFindRestingPlace(form, screen);
                _ = NimvioFormTestState.InvokeTryHopToNearbyWindow(form);
                _ = NimvioFormTestState.InvokeCanHideAtSafeEdge(form);
            }

            // Assert
        });
    }

    public void Dispose() => _scenario.Dispose();
}
