using System.Diagnostics;

namespace Nimvio;

internal sealed class NimvioApplicationContext : ApplicationContext
{
    private readonly List<NimvioForm> _forms = [];
    private readonly NotifyIcon _tray;
    private readonly Icon _appIcon;
    private readonly CancellationTokenSource _singleInstanceCts = new();
    private readonly SynchronizationContext _uiContext;
    
    public NimvioSettings Settings { get; }

    public NimvioApplicationContext() : this(NimvioSettings.Load(), startBackgroundServices: true, createInitialForms: true)
    {
    }

    internal NimvioApplicationContext(NimvioSettings settings, bool startBackgroundServices, bool createInitialForms = true)
    {
        Settings = settings;
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        _tray = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "Nimvio",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.DoubleClick += (_, _) => SummonAll();

        if (startBackgroundServices)
        {
            SingleInstance.RunActivationServer(() => _uiContext.Post(_ => SummonAll(), null), _singleInstanceCts.Token);
        }

        if (createInitialForms)
        {
            foreach (var profile in Settings.Profiles.Take(3).ToArray())
            {
                AddForm(profile, false);
            }
        }
    }

    internal void ShutdownForTests()
    {
        _singleInstanceCts.Cancel();
        _tray.Visible = false;
        _tray.Dispose();
        _appIcon.Dispose();
        foreach (var form in _forms.ToArray())
        {
            form.Close();
        }

        _forms.Clear();
    }

    internal IReadOnlyList<NimvioForm> Forms => _forms;

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Summon characters", null, (_, _) => SummonAll());
        menu.Items.Add(CreateAddCharacterMenu("Add character"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit Nimvio", null, (_, _) => ExitAll());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Privacy", null, (_, _) => OpenPrivacyPolicy());
        menu.Items.Add("About", null, (_, _) => AboutForm.ShowAbout());
        return menu;
    }

    private static void OpenPrivacyPolicy()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/MussabMuhaimeed/nimvio#privacy",
            UseShellExecute = true
        });
    }

    internal ToolStripMenuItem CreateAddCharacterMenu(string text)
    {
        var menu = new ToolStripMenuItem(text);
        menu.DropDownOpening += (_, _) => PopulateAddCharacterMenu(menu);
        PopulateAddCharacterMenu(menu);
        return menu;
    }

    private void PopulateAddCharacterMenu(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Clear();
        var availableNames = new[] { NimvioCharacterName.Nova, NimvioCharacterName.Mimo, NimvioCharacterName.Lumi }
            .Where(name => Settings.Profiles.All(profile => profile.Name != name))
            .ToArray();

        if (availableNames.Length == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem("All characters are already added") { Enabled = false });
            return;
        }

        foreach (var name in availableNames)
        {
            var characterName = name;
            menu.DropDownItems.Add(characterName.ToString(), null, (_, _) => AddCompanion(characterName));
        }
    }

    public void AddCompanion(NimvioCharacterName characterName)
    {
        if (_forms.Count >= 3)
        {
            _tray.ShowBalloonTip(2200, "Nimvio", "A maximum of three characters is supported.", ToolTipIcon.Info);
            return;
        }

        if (characterName == NimvioCharacterName.Unknown
            || Settings.Profiles.Any(profile => profile.Name == characterName))
        {
            _tray.ShowBalloonTip(2200, "Nimvio", $"{characterName} is already active or unavailable.", ToolTipIcon.Info);
            return;
        }

        var (personality, color) = characterName switch
        {
            NimvioCharacterName.Mimo => (NimvioPersonality.Playful, Color.FromArgb(255, 176, 76)),
            NimvioCharacterName.Lumi => (NimvioPersonality.Calm, Color.FromArgb(211, 126, 255)),
            _ => (NimvioPersonality.Curious, Color.FromArgb(86, 221, 242))
        };
        var profile = new NimvioProfile
        {
            Name = characterName,
            Personality = personality,
            AccentArgb = color.ToArgb()
        };
        Settings.Profiles.Add(profile);
        Settings.Save();
        AddForm(profile, true);
    }

    private void AddForm(NimvioProfile profile, bool nearCursor)
    {
        var form = new NimvioForm(this, Settings, profile);
        _forms.Add(form);
        form.FormClosed += (_, _) =>
        {
            _forms.Remove(form);
            if (_forms.Count == 0)
            {
                ExitAll();
            }
        };
        form.Show();
        if (nearCursor)
        {
            form.SummonTo(Screen.FromPoint(Cursor.Position), _forms.Count * 22);
        }
    }

    public void RemoveForm(NimvioForm form)
    {
        if (_forms.Count <= 1)
        {
            ExitAll();
            return;
        }
        Settings.Profiles.Remove(form.Profile);
        Settings.RemoveProfileMemory(form.Profile.Id);
        Settings.Save();
        _forms.Remove(form);
        form.Close();
    }

    public void SummonAll()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        for (var i = 0; i < _forms.Count; i++) {
            _forms[i].SummonTo(screen, i * 28);
        }
    }

    public NimvioForm? FindNearbyForm(NimvioForm source, float maximumDistance)
    {
        return _forms
            .Where(form => form != source && !form.IsDisposed)
            .Select(form => new { Form = form, Distance = Distance(source.WorldCenter, form.WorldCenter) })
            .Where(item => item.Distance <= maximumDistance)
            .OrderBy(item => item.Distance)
            .Select(item => item.Form)
            .FirstOrDefault();
    }

    public NimvioForm? FindPerchedFriend(NimvioForm source, float maximumDistance)
    {
        return _forms
            .Where(form => form != source && !form.IsDisposed && form.IsSafelyPerched
                && form.PerchedWindowHandle != source.PerchedWindowHandle)
            .Select(form => new { Form = form, Distance = Distance(source.WorldCenter, form.WorldCenter) })
            .Where(item => item.Distance <= maximumDistance)
            .OrderBy(item => item.Distance)
            .Select(item => item.Form)
            .FirstOrDefault();
    }

    public void ArrangeYouTubeWatching(WindowSnapshot window)
    {
        var viewers = _forms.Where(form => !form.IsDisposed).ToArray();
        for (var i = 0; i < viewers.Length; i++)
        { 
            viewers[i].BeginYouTubeWatching(window, i, viewers.Length);
        }
    }

    public void NotifySocialInteraction(NimvioForm first, NimvioForm second)
    {
        foreach (var observer in _forms.Where(form => form != first && form != second && !form.IsDisposed))
        {
            observer.ObserveFriendsInteraction(first, second);
        } 
    }

    private static float Distance(PointF a, PointF b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    public void Save() => Settings.Save();

    public void ExitAll()
    {
        Settings.Save();
        _singleInstanceCts.Cancel();
        _tray.Visible = false;
        _tray.Dispose();
        _appIcon.Dispose();
        foreach (var form in _forms.ToArray())
        {
            form.Close();
        }

        ExitThread();
    }
}
