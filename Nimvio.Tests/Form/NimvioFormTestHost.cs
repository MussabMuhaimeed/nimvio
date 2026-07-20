using System.Reflection;
using Nimvio;

namespace Nimvio.Tests.Form;

internal static class NimvioFormTestHost
{
    public static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(30));
        if (error is not null)
        {
            throw error;
        }
    }
}

internal sealed class NimvioFormScenario : IDisposable
{
    private readonly List<NimvioApplicationContext> _contexts = [];

    public NimvioFormScenario()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nimvio-form-tests", Guid.NewGuid().ToString("N"));
        SettingsPath = Path.Combine(directory, "settings.json");
    }

    public string SettingsPath { get; }

    public NimvioApplicationContext CreateContext(NimvioSettings settings, bool createInitialForms = false)
    {
        settings.PersistenceFilePath = SettingsPath;
        var context = new NimvioApplicationContext(settings, startBackgroundServices: false, createInitialForms);
        _contexts.Add(context);
        return context;
    }

    public static NimvioSettings SettingsWith(params NimvioProfile[] profiles)
        => new() { Playing = true, Profiles = profiles.ToList() };

    public static NimvioProfile Character(NimvioCharacterName name, string id)
        => new() { Name = name, Id = id };

    public void Dispose()
    {
        NimvioFormTestHost.RunSta(() =>
        {
            foreach (var context in _contexts)
            {
                context.ShutdownForTests();
            }
        });

        var directory = Path.GetDirectoryName(SettingsPath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}

internal static class NimvioFormTestState
{
    private static FieldInfo Field(string name)
        => typeof(NimvioForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static MethodInfo Method(string name)
        => typeof(NimvioForm).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static NimvioBehavior GetBehavior(NimvioForm form)
        => (NimvioBehavior)Field("_behavior").GetValue(form)!;

    public static void SetBehavior(NimvioForm form, NimvioBehavior behavior)
        => Field("_behavior").SetValue(form, behavior);

    public static void SetBehaviorTicks(NimvioForm form, int ticks)
        => Field("_behaviorTicks").SetValue(form, ticks);

    public static void SetRestActionsRemaining(NimvioForm form, int count)
        => Field("_restActionsRemaining").SetValue(form, count);

    public static void SetTarget(NimvioForm form, PointF target)
        => Field("_target").SetValue(form, target);

    public static void SetVelocity(NimvioForm form, PointF velocity)
        => Field("_velocity").SetValue(form, velocity);

    public static void SetPhase(NimvioForm form, float phase)
        => Field("_phase").SetValue(form, phase);

    public static void SetRandom(NimvioForm form, int seed)
        => Field("_random").SetValue(form, new Random(seed));

    public static void SetRandom(NimvioForm form, Random random)
        => Field("_random").SetValue(form, random);

    public static void SetTypingCooldown(NimvioForm form, int ticks)
        => Field("_typingCooldown").SetValue(form, ticks);

    public static void SetCursorInteractionCooldown(NimvioForm form, int ticks)
        => Field("_cursorInteractionCooldown").SetValue(form, ticks);

    public static void SetLastCursorPosition(NimvioForm form, Point position)
        => Field("_lastCursorPosition").SetValue(form, position);

    public static void SetPressedTypingKeys(NimvioForm form, HashSet<Keys> keys)
        => Field("_pressedTypingKeys").SetValue(form, keys);

    public static void SetLastDragCursor(NimvioForm form, Point position)
        => Field("_lastDragCursor").SetValue(form, position);

    public static bool GetDragging(NimvioForm form)
        => (bool)Field("_dragging").GetValue(form)!;

    public static bool GetDidDrag(NimvioForm form)
        => (bool)Field("_didDrag").GetValue(form)!;

    public static bool GetUserWasAway(NimvioForm form)
        => (bool)Field("_userWasAway").GetValue(form)!;

    public static void SetDragging(NimvioForm form, bool dragging)
        => Field("_dragging").SetValue(form, dragging);

    public static void SetDidDrag(NimvioForm form, bool didDrag)
        => Field("_didDrag").SetValue(form, didDrag);

    public static void SetSocialInteractionCooldown(NimvioForm form, int ticks)
        => Field("_socialInteractionCooldown").SetValue(form, ticks);

    public static void SetJealousyCooldown(NimvioForm form, int ticks)
        => Field("_jealousyCooldown").SetValue(form, ticks);

    public static void SetWindowInteractionCooldown(NimvioForm form, int ticks)
        => Field("_windowInteractionCooldown").SetValue(form, ticks);

    public static void SetRareEventCooldown(NimvioForm form, int ticks)
        => Field("_rareEventCooldown").SetValue(form, ticks);

    public static void SetPerchedWindow(NimvioForm form, IntPtr handle)
        => Field("_perchedWindow").SetValue(form, handle);

    public static void SetPerchedWindowBounds(NimvioForm form, Rectangle bounds)
        => Field("_perchedWindowBounds").SetValue(form, bounds);

    public static void SetWatchedYouTubeWindow(NimvioForm form, IntPtr handle)
        => Field("_watchedYouTubeWindow").SetValue(form, handle);

    public static void SetActiveAccessory(NimvioForm form, ActiveAppAccessory accessory)
        => Field("_activeAccessory").SetValue(form, accessory);

    public static void SetFacingRight(NimvioForm form, bool facingRight)
        => Field("_facingRight").SetValue(form, facingRight);

    public static void SetBlinkFrames(NimvioForm form, int frames)
        => Field("_blinkFrames").SetValue(form, frames);

    public static void SetSpeech(NimvioForm form, string text, int ticks = 120)
    {
        Field("_speechText").SetValue(form, text);
        Field("_speechTicks").SetValue(form, ticks);
        Field("_speechCooldown").SetValue(form, 0);
    }

    public static void SetSystemCheckTicks(NimvioForm form, int ticks)
        => Field("_systemCheckTicks").SetValue(form, ticks);

    public static void SetUserWasAway(NimvioForm form, bool away)
        => Field("_userWasAway").SetValue(form, away);

    public static void SetIgnoredReactionShown(NimvioForm form, bool shown)
        => Field("_ignoredReactionShown").SetValue(form, shown);

    public static void SetSocialTicks(NimvioForm form, int ticks)
        => Field("_socialTicks").SetValue(form, ticks);

    public static void SetHoverTicks(NimvioForm form, int ticks)
        => Field("_hoverTicks").SetValue(form, ticks);

    public static string? GetSpeechText(NimvioForm form)
        => (string?)Field("_speechText").GetValue(form);

    public static int GetSocialInteractionCooldown(NimvioForm form)
        => (int)Field("_socialInteractionCooldown").GetValue(form)!;

    public static int GetJealousyCooldown(NimvioForm form)
        => (int)Field("_jealousyCooldown").GetValue(form)!;

    public static PointF GetTarget(NimvioForm form)
        => (PointF)Field("_target").GetValue(form)!;

    public static void Tick(NimvioForm form, int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            form.RunOneTickForTests();
        }
    }

    public static void Paint(NimvioForm form)
    {
        using var image = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
        using var graphics = Graphics.FromImage(image);
        form.RunPaintForTests(graphics);
    }

    public static void InvokeBeginDrag(NimvioForm form, MouseButtons button = MouseButtons.Left)
        => Method("BeginDrag").Invoke(form, [form, new MouseEventArgs(button, 1, 10, 10, 0)]);

    public static void InvokeDrag(NimvioForm form)
        => Method("DragForm").Invoke(form, [form, new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0)]);

    public static void InvokeEndDrag(NimvioForm form, MouseButtons button = MouseButtons.Left)
        => Method("EndDrag").Invoke(form, [form, new MouseEventArgs(button, 1, 10, 10, 0)]);

    public static void InvokeTogglePlaying(NimvioForm form)
        => Method("TogglePlaying").Invoke(form, null);

    public static void InvokeGoToScreen(NimvioForm form, int index)
        => Method("GoToScreen").Invoke(form, [index]);

    public static void InvokeStartSocialInteraction(NimvioForm form, NimvioForm friend)
        => Method("StartSocialInteraction").Invoke(form, [friend]);

    public static void InvokeBeginWindowWave(NimvioForm form, NimvioForm friend)
        => Method("BeginWindowWave").Invoke(form, [friend]);

    public static bool InvokeTryHopToNearbyWindow(NimvioForm form)
        => (bool)Method("TryHopToNearbyWindow").Invoke(form, null)!;

    public static bool InvokeCanHideAtSafeEdge(NimvioForm form)
        => (bool)Method("CanHideAtSafeEdge").Invoke(form, null)!;

    public static void InvokeStartSearching(NimvioForm form, bool otherScreen)
        => Method("StartSearching").Invoke(form, [otherScreen]);

    public static PointF InvokeFindRestingPlace(NimvioForm form, Screen screen)
        => (PointF)Method("FindRestingPlace").Invoke(form, [screen])!;

    public static void InvokeChoosePlaceAndWalk(NimvioForm form)
        => Method("ChoosePlaceAndWalk").Invoke(form, null);

    public static ContextMenuStrip GetContextMenu(NimvioForm form)
        => (ContextMenuStrip)form.ContextMenuStrip!;

    public static void SetKnownWindows(NimvioForm form, HashSet<IntPtr> handles)
        => Field("_knownWindows").SetValue(form, handles);

    public static void InvokeToggleAllowedScreen(NimvioForm form, string deviceName, bool enabled)
        => Method("ToggleAllowedScreen").Invoke(form, [deviceName, enabled]);

    public static void SetSearchOtherScreen(NimvioForm form, bool value)
        => Field("_searchOtherScreen").SetValue(form, value);
}
