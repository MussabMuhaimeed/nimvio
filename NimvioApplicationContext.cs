namespace Nimvio;

internal sealed class NimvioApplicationContext : ApplicationContext
{
    private readonly List<NimvioForm> _forms = [];
    private readonly NotifyIcon _tray;
    private readonly Icon _appIcon;
    public NimvioSettings Settings { get; } = NimvioSettings.Load();

    public NimvioApplicationContext()
    {
        _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        _tray = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "Nimvio",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.DoubleClick += (_, _) => SummonAll();

        foreach (var profile in Settings.Profiles.Take(3).ToArray()) AddForm(profile, false);
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Summon characters", null, (_, _) => SummonAll());
        menu.Items.Add("Add character", null, (_, _) => AddCompanion());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit Nimvio", null, (_, _) => ExitAll());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About", null, (_, _) => AboutForm.ShowAbout());
        return menu;
    }

    public void AddCompanion()
    {
        if (_forms.Count >= 3)
        {
            _tray.ShowBalloonTip(2200, "Nimvio", "A maximum of three characters is supported.", ToolTipIcon.Info);
            return;
        }

        var index = Settings.Profiles.Count;
        var names = new[] { "Nova", "Mimo", "Lumi" };
        var colors = new[]
        {
            Color.FromArgb(86, 221, 242), Color.FromArgb(255, 176, 76),
            Color.FromArgb(211, 126, 255)
        };
        var profile = new NimvioProfile
        {
            Name = names[index % names.Length],
            Personality = (NimvioPersonality)(index % 3),
            AccentArgb = colors[index % colors.Length].ToArgb()
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
            if (_forms.Count == 0) ExitAll();
        };
        form.Show();
        if (nearCursor) form.SummonTo(Screen.FromPoint(Cursor.Position), _forms.Count * 22);
    }

    public void RemoveForm(NimvioForm form)
    {
        if (_forms.Count <= 1)
        {
            ExitAll();
            return;
        }
        Settings.Profiles.Remove(form.Profile);
        Settings.Save();
        form.Close();
    }

    public void SummonAll()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        for (var i = 0; i < _forms.Count; i++) _forms[i].SummonTo(screen, i * 28);
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

    private static float Distance(PointF a, PointF b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    public void Save() => Settings.Save();

    public void ExitAll()
    {
        Settings.Save();
        _tray.Visible = false;
        _tray.Dispose();
        _appIcon.Dispose();
        foreach (var form in _forms.ToArray()) form.Close();
        ExitThread();
    }
}
