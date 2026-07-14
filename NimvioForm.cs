using System.Drawing.Drawing2D;

namespace Nimvio;

internal enum NimvioBehavior
{
    Searching, Walking, Hopping, Sitting, Pointing, LookingAround, Thinking,
    Sleeping, Waving, Inspecting, ChasingCursor, Stumbling, Surprised, Thrown
}

internal sealed class NimvioForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Random _random = new(Guid.NewGuid().GetHashCode());
    private readonly NimvioApplicationContext _context;
    private readonly NimvioSettings _settings;
    private readonly NimvioMind _mind;
    private readonly ContextMenuStrip _menu = new() { RightToLeft = RightToLeft.No };
    private readonly ToolStripMenuItem _playItem = new();
    private PointF _velocity;
    private PointF _target;
    private Point _dragOffset;
    private Point _lastDragCursor;
    private NimvioBehavior _behavior = NimvioBehavior.Searching;
    private int _behaviorTicks;
    private int _restActionsRemaining;
    private int _systemCheckTicks;
    private int _rareEventCooldown = 500;
    private int _hoverTicks;
    private int _socialTicks = 180;
    private int _blinkTicks = 150;
    private int _blinkFrames;
    private float _phase;
    private float _sitBlend;
    private bool _dragging;
    private bool _didDrag;
    private bool _searchOtherScreen;
    private bool _facingRight = true;

    public NimvioProfile Profile { get; }
    internal PointF WorldCenter => Center;

    public NimvioForm(NimvioApplicationContext context, NimvioSettings settings, NimvioProfile profile)
    {
        _context = context;
        _settings = settings;
        Profile = profile;
        _mind = new NimvioMind(profile);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        ClientSize = new Size(_settings.Size, _settings.Size);
        StartPosition = FormStartPosition.Manual;

        var enabled = _settings.EnabledScreens();
        var start = enabled[_random.Next(enabled.Length)].WorkingArea;
        Location = new Point(start.Left + start.Width / 2 - Width / 2, start.Top + start.Height / 2 - Height / 2);
        BuildMenu();
        StartSearching(false);

        _timer.Tick += TickForm;
        _timer.Start();
        MouseDown += BeginDrag;
        MouseMove += DragForm;
        MouseUp += EndDrag;
        MouseDoubleClick += (_, _) => TogglePlaying();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x80;
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    private bool IsMoving => _behavior is NimvioBehavior.Walking or NimvioBehavior.Hopping or NimvioBehavior.ChasingCursor;
    private bool IsSeatedBehavior => _behavior is NimvioBehavior.Sitting or NimvioBehavior.Pointing or NimvioBehavior.LookingAround
        or NimvioBehavior.Thinking or NimvioBehavior.Sleeping or NimvioBehavior.Waving or NimvioBehavior.Inspecting;

    private void BuildMenu()
    {
        _playItem.Text = _settings.Playing ? "Pause activity" : "Resume activity";
        _playItem.Click += (_, _) => TogglePlaying();
        _menu.Items.Add(_playItem);
        _menu.Items.Add($"Summon {Profile.Name}", null, (_, _) => SummonTo(Screen.FromPoint(Cursor.Position), 0));

        var activity = new ToolStripMenuItem("Activity level");
        AddChoice(activity, "Calm", ActivityLevel.Calm, () => _settings.Activity, value => _settings.Activity = value);
        AddChoice(activity, "Normal", ActivityLevel.Normal, () => _settings.Activity, value => _settings.Activity = value);
        AddChoice(activity, "Energetic", ActivityLevel.Energetic, () => _settings.Activity, value => _settings.Activity = value);
        _menu.Items.Add(activity);

        var autonomy = new ToolStripMenuItem("Autonomy");
        AddChoice(autonomy, "Low", AutonomyLevel.Low, () => _settings.Autonomy, value => _settings.Autonomy = value);
        AddChoice(autonomy, "Normal", AutonomyLevel.Normal, () => _settings.Autonomy, value => _settings.Autonomy = value);
        AddChoice(autonomy, "High", AutonomyLevel.High, () => _settings.Autonomy, value => _settings.Autonomy = value);
        _menu.Items.Add(autonomy);

        var size = new ToolStripMenuItem("Size");
        AddSizeChoice(size, "Small", 84);
        AddSizeChoice(size, "Medium", 112);
        AddSizeChoice(size, "Large", 144);
        _menu.Items.Add(size);

        var screens = new ToolStripMenuItem("Allowed screens");
        foreach (var screen in Screen.AllScreens)
        {
            var item = new ToolStripMenuItem(screen.DeviceName)
            {
                CheckOnClick = true,
                Checked = _settings.AllowedScreens.Count == 0 || _settings.AllowedScreens.Contains(screen.DeviceName)
            };
            item.CheckedChanged += (_, _) => ToggleAllowedScreen(screen.DeviceName, item.Checked);
            screens.DropDownItems.Add(item);
        }
        _menu.Items.Add(screens);

        var goTo = new ToolStripMenuItem("Go to screen");
        for (var i = 0; i < Screen.AllScreens.Length; i++)
        {
            var index = i;
            goTo.DropDownItems.Add($"Screen {i + 1}", null, (_, _) => GoToScreen(index));
        }
        _menu.Items.Add(goTo);

        var personality = new ToolStripMenuItem("Personality");
        AddChoice(personality, "Curious", NimvioPersonality.Curious, () => Profile.Personality, value => Profile.Personality = value);
        AddChoice(personality, "Calm", NimvioPersonality.Calm, () => Profile.Personality, value => Profile.Personality = value);
        AddChoice(personality, "Playful", NimvioPersonality.Playful, () => Profile.Personality, value => Profile.Personality = value);
        _menu.Items.Add(personality);

        var name = new ToolStripMenuItem("Character name");
        foreach (var candidate in new[] { "Nova", "Mimo", "Lumi" })
        {
            var item = new ToolStripMenuItem(candidate) { Checked = Profile.Name == candidate };
            item.Click += (_, _) =>
            {
                Profile.Name = candidate;
                foreach (ToolStripMenuItem sibling in name.DropDownItems.OfType<ToolStripMenuItem>()) sibling.Checked = sibling == item;
                SaveSettings();
            };
            name.DropDownItems.Add(item);
        }
        _menu.Items.Add(name);

        var color = new ToolStripMenuItem("Character color");
        AddColorChoice(color, "Cyan", Color.FromArgb(86, 221, 242));
        AddColorChoice(color, "Orange", Color.FromArgb(255, 176, 76));
        AddColorChoice(color, "Green", Color.FromArgb(116, 226, 144));
        AddColorChoice(color, "Purple", Color.FromArgb(211, 126, 255));
        _menu.Items.Add(color);

        var quiet = new ToolStripMenuItem("Quiet hours 22:00–07:00") { CheckOnClick = true, Checked = _settings.QuietHoursEnabled };
        quiet.CheckedChanged += (_, _) => { _settings.QuietHoursEnabled = quiet.Checked; SaveSettings(); };
        _menu.Items.Add(quiet);

        var fullscreen = new ToolStripMenuItem("Hide during fullscreen") { CheckOnClick = true, Checked = _settings.PauseInFullscreen };
        fullscreen.CheckedChanged += (_, _) => { _settings.PauseInFullscreen = fullscreen.Checked; SaveSettings(); };
        _menu.Items.Add(fullscreen);

        var startup = new ToolStripMenuItem("Start with Windows") { Checked = NimvioSettings.StartsWithWindows(), CheckOnClick = true };
        startup.CheckedChanged += (_, _) => NimvioSettings.SetStartWithWindows(startup.Checked);
        _menu.Items.Add(startup);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Add another character", null, (_, _) => _context.AddCompanion());
        _menu.Items.Add($"Remove {Profile.Name}", null, (_, _) => _context.RemoveForm(this));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("About", null, (_, _) => AboutForm.ShowAbout(this));
        ContextMenuStrip = _menu;
    }

    private void AddChoice<T>(ToolStripMenuItem parent, string text, T value, Func<T> getter, Action<T> setter) where T : struct, Enum
    {
        var item = new ToolStripMenuItem(text) { Checked = EqualityComparer<T>.Default.Equals(getter(), value) };
        item.Click += (_, _) =>
        {
            setter(value);
            foreach (ToolStripMenuItem sibling in parent.DropDownItems.OfType<ToolStripMenuItem>()) sibling.Checked = sibling == item;
            SaveSettings();
        };
        parent.DropDownItems.Add(item);
    }

    private void AddSizeChoice(ToolStripMenuItem parent, string text, int pixels)
    {
        var item = new ToolStripMenuItem(text) { Checked = _settings.Size == pixels };
        item.Click += (_, _) =>
        {
            _settings.Size = pixels;
            foreach (ToolStripMenuItem sibling in parent.DropDownItems.OfType<ToolStripMenuItem>()) sibling.Checked = sibling == item;
            ClientSize = new Size(pixels, pixels);
            SaveSettings();
        };
        parent.DropDownItems.Add(item);
    }

    private void AddColorChoice(ToolStripMenuItem parent, string text, Color color)
    {
        var item = new ToolStripMenuItem(text) { Checked = Profile.AccentArgb == color.ToArgb() };
        item.Click += (_, _) =>
        {
            Profile.AccentArgb = color.ToArgb();
            foreach (ToolStripMenuItem sibling in parent.DropDownItems.OfType<ToolStripMenuItem>()) sibling.Checked = sibling == item;
            SaveSettings();
            Invalidate();
        };
        parent.DropDownItems.Add(item);
    }

    private void ToggleAllowedScreen(string deviceName, bool enabled)
    {
        if (_settings.AllowedScreens.Count == 0)
            _settings.AllowedScreens = Screen.AllScreens.Select(screen => screen.DeviceName).ToList();
        if (enabled && !_settings.AllowedScreens.Contains(deviceName)) _settings.AllowedScreens.Add(deviceName);
        if (!enabled && _settings.AllowedScreens.Count > 1) _settings.AllowedScreens.Remove(deviceName);
        SaveSettings();
    }

    private void SaveSettings() => _context.Save();

    private void TogglePlaying()
    {
        _settings.Playing = !_settings.Playing;
        _playItem.Text = _settings.Playing ? "Pause activity" : "Resume activity";
        SaveSettings();
        Invalidate();
    }

    public void SummonTo(Screen screen, int offset)
    {
        var area = screen.WorkingArea;
        Location = new Point(
            Math.Clamp(Cursor.Position.X - Width / 2 + offset, area.Left, area.Right - Width),
            Math.Clamp(Cursor.Position.Y - Height / 2, area.Top, area.Bottom - Height));
        Opacity = 1;
        _behavior = NimvioBehavior.Waving;
        _behaviorTicks = 170;
        _restActionsRemaining = 1;
        _settings.Playing = true;
    }

    private void GoToScreen(int index)
    {
        if (index < 0 || index >= Screen.AllScreens.Length) return;
        _target = FindRestingPlace(Screen.AllScreens[index]);
        _behavior = NimvioBehavior.Walking;
        _behaviorTicks = 1800;
        _settings.Playing = true;
    }

    private void TickForm(object? sender, EventArgs e)
    {
        _phase += .12f;
        _sitBlend = Lerp(_sitBlend, IsSeatedBehavior ? 1f : 0f, .09f);
        UpdateBlink();
        CheckSystemState();
        _mind.Tick(_behavior, _settings.Activity);

        if (_dragging || !_settings.Playing || Opacity == 0) { Invalidate(); return; }
        if (_settings.IsQuietTime(DateTime.Now) && _behavior != NimvioBehavior.Sleeping)
        {
            _behavior = NimvioBehavior.Sleeping;
            _behaviorTicks = 900;
            _restActionsRemaining = 1;
        }

        WatchForInteraction();
        TryRareEvent();

        switch (_behavior)
        {
            case NimvioBehavior.Searching:
                SlowDown(.12f);
                if (--_behaviorTicks <= 0) ChoosePlaceAndWalk();
                break;
            case NimvioBehavior.Walking:
            case NimvioBehavior.Hopping:
                MoveTowardsTarget();
                if (Distance(Center, _target) < 15 || --_behaviorTicks <= 0) ArriveAndSit();
                break;
            case NimvioBehavior.ChasingCursor:
                _target = Cursor.Position;
                MoveTowardsTarget();
                if (--_behaviorTicks <= 0 || Distance(Center, _target) < 22) ArriveAndSit();
                break;
            case NimvioBehavior.Thrown:
                MoveThrown();
                break;
            default:
                SlowDown(.18f);
                if (--_behaviorTicks <= 0) AdvanceStationaryBehavior();
                break;
        }
        Invalidate();
    }

    private void CheckSystemState()
    {
        if (++_systemCheckTicks < 60) return;
        _systemCheckTicks = 0;
        var shouldHide = _settings.PauseInFullscreen && DesktopAwareness.IsForeignFullscreen();
        Opacity = shouldHide ? 0 : 1;
        if (_random.Next(5) == 0) SaveSettings();
    }

    private void WatchForInteraction()
    {
        var local = PointToClient(Cursor.Position);
        if (ClientRectangle.Contains(local)) _hoverTicks++;
        else _hoverTicks = 0;
        if (_hoverTicks > 90 && IsSeatedBehavior && _behavior != NimvioBehavior.Sleeping)
        {
            _hoverTicks = -250;
            _behavior = NimvioBehavior.Waving;
            _behaviorTicks = 150;
            _mind.Comforted();
        }

        if (--_socialTicks <= 0)
        {
            _socialTicks = _random.Next(150, 300);
            var friend = _context.FindNearbyForm(this, 230);
            if (friend is not null && _behavior != NimvioBehavior.Sleeping && !IsMoving)
            {
                _facingRight = friend.WorldCenter.X >= Center.X;
                _behavior = NimvioBehavior.Waving;
                _behaviorTicks = 145;
                _restActionsRemaining = Math.Max(1, _restActionsRemaining);
                _mind.Socialized();
            }
        }
    }

    private void TryRareEvent()
    {
        if (_rareEventCooldown-- > 0) return;
        var autonomyFactor = _settings.Autonomy == AutonomyLevel.High ? 2.0 : _settings.Autonomy == AutonomyLevel.Low ? .45 : 1.0;
        if (IsMoving && _random.NextDouble() < .0014 * autonomyFactor)
        {
            _behavior = NimvioBehavior.Stumbling;
            _behaviorTicks = 75;
            _mind.Startled();
            _rareEventCooldown = 900;
        }
        else if (IsSeatedBehavior && _mind.Boredom > 55 && Distance(Center, Cursor.Position) < 380 && _random.NextDouble() < .0022 * autonomyFactor)
        {
            _behavior = NimvioBehavior.ChasingCursor;
            _behaviorTicks = 260;
            _rareEventCooldown = 1100;
        }
    }

    private void MoveTowardsTarget()
    {
        var dx = _target.X - Center.X;
        var dy = _target.Y - Center.Y;
        var distance = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
        var activity = _settings.Activity == ActivityLevel.Energetic ? 1.28f : _settings.Activity == ActivityLevel.Calm ? .72f : 1f;
        var personality = Profile.Personality == NimvioPersonality.Playful ? 1.12f : Profile.Personality == NimvioPersonality.Calm ? .86f : 1f;
        var speed = (_behavior == NimvioBehavior.Hopping ? 4.7f : _behavior == NimvioBehavior.ChasingCursor ? 5.4f : 3.15f) * _settings.Speed * activity * personality;
        var desired = new PointF(dx / distance * speed, dy / distance * speed);
        _velocity = new PointF(Lerp(_velocity.X, desired.X, .075f), Lerp(_velocity.Y, desired.Y, .075f));
        if (_behavior == NimvioBehavior.Hopping) _velocity.Y += MathF.Sin(_phase * 1.7f) * 1.2f;
        if (Math.Abs(_velocity.X) > .1f) _facingRight = _velocity.X > 0;
        Location = KeepVisible(new Point((int)(Left + _velocity.X), (int)(Top + _velocity.Y)));
    }

    private void MoveThrown()
    {
        _velocity = new PointF(_velocity.X * .975f, _velocity.Y + .36f);
        var area = Screen.FromPoint(Point.Round(Center)).WorkingArea;
        var next = new Point((int)(Left + _velocity.X), (int)(Top + _velocity.Y));
        if (next.X < area.Left || next.X > area.Right - Width) _velocity.X *= -.65f;
        if (next.Y < area.Top || next.Y > area.Bottom - Height) _velocity.Y *= -.58f;
        Location = new Point(Math.Clamp(next.X, area.Left, area.Right - Width), Math.Clamp(next.Y, area.Top, area.Bottom - Height));
        if (--_behaviorTicks <= 0 || Math.Abs(_velocity.X) + Math.Abs(_velocity.Y) < 1)
        {
            _behavior = NimvioBehavior.Surprised;
            _behaviorTicks = 120;
        }
    }

    private void SlowDown(float amount) => _velocity = new PointF(Lerp(_velocity.X, 0, amount), Lerp(_velocity.Y, 0, amount));

    private void StartSearching(bool otherScreen)
    {
        _behavior = NimvioBehavior.Searching;
        var autonomy = _settings.Autonomy == AutonomyLevel.High ? .65 : _settings.Autonomy == AutonomyLevel.Low ? 1.5 : 1;
        _behaviorTicks = (int)(_random.Next(55, 135) * autonomy);
        _searchOtherScreen = otherScreen;
    }

    private void ChoosePlaceAndWalk()
    {
        var current = Screen.FromPoint(Point.Round(Center));
        var screens = _settings.EnabledScreens();
        var destination = current;
        if (_searchOtherScreen && screens.Length > 1)
        {
            var others = screens.Where(screen => screen.DeviceName != current.DeviceName).ToArray();
            if (others.Length > 0)
            {
                destination = Profile.FavoriteScreen is not null && others.Any(s => s.DeviceName == Profile.FavoriteScreen) && _random.NextDouble() < .42
                    ? others.First(s => s.DeviceName == Profile.FavoriteScreen)
                    : others[_random.Next(others.Length)];
            }
        }

        _target = FindRestingPlace(destination);
        var hopChance = Profile.Personality == NimvioPersonality.Playful ? .38 : Profile.Personality == NimvioPersonality.Calm ? .08 : .2;
        _behavior = _random.NextDouble() < hopChance ? NimvioBehavior.Hopping : NimvioBehavior.Walking;
        _behaviorTicks = 1800;
        _mind.Explored();
    }

    private PointF FindRestingPlace(Screen screen)
    {
        var area = screen.WorkingArea;
        var marginX = Width / 2 + 8;
        var marginY = Height / 2 + 5;
        var windows = DesktopAwareness.VisibleWindows(screen);

        for (var attempt = 0; attempt < 7; attempt++)
        {
            PointF candidate;
            var usableLedges = windows.Where(window => window.Top > area.Top + Height && window.Top < area.Bottom - Height / 2).ToArray();
            var roll = _random.Next(100);
            if (roll < 34 && usableLedges.Length > 0)
            {
                var ledge = usableLedges[_random.Next(usableLedges.Length)];
                var left = Math.Max(area.Left + marginX, ledge.Left + marginX);
                var right = Math.Min(area.Right - marginX, ledge.Right - marginX);
                candidate = right > left
                    ? new PointF(_random.Next(left, right), ledge.Top - marginY + 5)
                    : new PointF(area.Left + marginX, area.Bottom - marginY);
            }
            else if (roll < 70)
            {
                candidate = new PointF(_random.Next(area.Left + marginX, Math.Max(area.Left + marginX + 1, area.Right - marginX)), area.Bottom - marginY);
            }
            else if (roll < 88)
            {
                candidate = new PointF(_random.Next(2) == 0 ? area.Left + marginX : area.Right - marginX,
                    _random.Next(2) == 0 ? area.Top + marginY : area.Bottom - marginY);
            }
            else candidate = RandomPoint(area);

            if (!Profile.RecentPlaces.Any(place => Distance(candidate, place) < 150)) return candidate;
        }
        return RandomPoint(area);
    }

    private void ArriveAndSit()
    {
        _velocity = PointF.Empty;
        _behavior = NimvioBehavior.Sitting;
        _behaviorTicks = _random.Next(80, 170);
        _restActionsRemaining = _random.Next(1, _settings.Autonomy == AutonomyLevel.High ? 5 : 4);
        var place = Point.Round(Center);
        Profile.RecentPlaces.Add(place);
        while (Profile.RecentPlaces.Count > 6) Profile.RecentPlaces.RemoveAt(0);
        Profile.FavoriteScreen = Screen.FromPoint(place).DeviceName;
    }

    private void AdvanceStationaryBehavior()
    {
        if (_behavior == NimvioBehavior.Stumbling)
        {
            _behavior = NimvioBehavior.Surprised;
            _behaviorTicks = 110;
            return;
        }
        if (_behavior == NimvioBehavior.Surprised)
        {
            StartSearching(false);
            return;
        }
        if (_behavior == NimvioBehavior.Sleeping && _settings.IsQuietTime(DateTime.Now))
        {
            _behaviorTicks = 600;
            return;
        }
        ContinueRestRoutine();
    }

    private void ContinueRestRoutine()
    {
        if (_behavior != NimvioBehavior.Sitting) _restActionsRemaining--;
        if (_restActionsRemaining <= 0)
        {
            var changeScreenChance = _settings.Autonomy == AutonomyLevel.High ? .55 : _settings.Autonomy == AutonomyLevel.Low ? .16 : .36;
            StartSearching(_mind.Boredom > 65 || _random.NextDouble() < changeScreenChance);
            return;
        }

        if (_mind.Energy < 24)
            _behavior = NimvioBehavior.Sleeping;
        else if (_mind.Boredom > 70 && Distance(Center, Cursor.Position) < 450)
            _behavior = NimvioBehavior.ChasingCursor;
        else if (_mind.Curiosity > 68)
            _behavior = NimvioBehavior.Inspecting;
        else
        {
            var roll = _random.Next(100);
            _behavior = roll switch
            {
                < 22 => NimvioBehavior.Pointing,
                < 44 => NimvioBehavior.LookingAround,
                < 60 => NimvioBehavior.Thinking,
                < 75 => NimvioBehavior.Waving,
                < 84 => NimvioBehavior.Inspecting,
                < 93 => NimvioBehavior.Sleeping,
                _ => NimvioBehavior.Sitting
            };
        }

        _behaviorTicks = _behavior switch
        {
            NimvioBehavior.Sleeping => _random.Next(280, 580),
            NimvioBehavior.ChasingCursor => _random.Next(180, 300),
            NimvioBehavior.Sitting => _random.Next(80, 170),
            _ => _random.Next(130, 280)
        };
        if (_behavior is NimvioBehavior.Pointing or NimvioBehavior.Waving) _facingRight = Cursor.Position.X >= Center.X;
    }

    private PointF RandomPoint(Rectangle area) => new(
        _random.Next(area.Left + Width / 2, Math.Max(area.Left + Width / 2 + 1, area.Right - Width / 2)),
        _random.Next(area.Top + Height / 2, Math.Max(area.Top + Height / 2 + 1, area.Bottom - Height / 2)));

    private Point KeepVisible(Point candidate)
    {
        var currentScreen = Screen.FromPoint(Point.Round(Center));
        var targetScreen = Screen.FromPoint(Point.Round(_target));
        if (currentScreen.DeviceName != targetScreen.DeviceName)
        {
            var virtualArea = SystemInformation.VirtualScreen;
            return new Point(Math.Clamp(candidate.X, virtualArea.Left - Width / 3, virtualArea.Right - Width * 2 / 3),
                Math.Clamp(candidate.Y, virtualArea.Top - Height / 3, virtualArea.Bottom - Height * 2 / 3));
        }
        var area = Screen.FromPoint(new Point(candidate.X + Width / 2, candidate.Y + Height / 2)).WorkingArea;
        return new Point(Math.Clamp(candidate.X, area.Left - Width / 3, area.Right - Width * 2 / 3),
            Math.Clamp(candidate.Y, area.Top - Height / 3, area.Bottom - Height * 2 / 3));
    }

    private PointF Center => new(Left + Width / 2f, Top + Height / 2f);
    private static float Distance(PointF a, PointF b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _didDrag = false;
        _dragOffset = e.Location;
        _lastDragCursor = Cursor.Position;
        _velocity = PointF.Empty;
        Capture = true;
    }

    private void DragForm(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var mouse = Cursor.Position;
        var dx = mouse.X - _lastDragCursor.X;
        var dy = mouse.Y - _lastDragCursor.Y;
        if (Math.Abs(dx) + Math.Abs(dy) > 2) _didDrag = true;
        _velocity = new PointF(dx, dy);
        _lastDragCursor = mouse;
        Location = new Point(mouse.X - _dragOffset.X, mouse.Y - _dragOffset.Y);
    }

    private void EndDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = false;
        Capture = false;
        if (!_didDrag)
        {
            _mind.Comforted();
            _behavior = NimvioBehavior.Waving;
            _behaviorTicks = 160;
            _restActionsRemaining = 1;
        }
        else if (Math.Abs(_velocity.X) + Math.Abs(_velocity.Y) > 12)
        {
            _velocity = new PointF(_velocity.X * .72f, _velocity.Y * .72f);
            _behavior = NimvioBehavior.Thrown;
            _behaviorTicks = 100;
            _mind.Startled();
        }
        else StartSearching(false);
    }

    private void UpdateBlink()
    {
        if (_blinkFrames > 0) { _blinkFrames--; return; }
        if (--_blinkTicks <= 0)
        {
            _blinkFrames = 7;
            _blinkTicks = _random.Next(130, 340);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TranslateTransform(Width / 2f, Height / 2f);
        var scale = Width / 112f;
        e.Graphics.ScaleTransform(scale, scale);
        if (!_facingRight) e.Graphics.ScaleTransform(-1, 1);
        if (_behavior == NimvioBehavior.Thrown) e.Graphics.RotateTransform(MathF.Sin(_phase) * 18f);
        if (_behavior == NimvioBehavior.Stumbling) e.Graphics.RotateTransform(MathF.Sin(_phase * 2f) * 12f);

        var bob = IsMoving ? MathF.Abs(MathF.Sin(_phase)) * -4f : MathF.Sin(_phase * .42f) * 1.2f;
        e.Graphics.TranslateTransform(0, bob);
        DrawShadow(e.Graphics);
        DrawForm(e.Graphics);
    }

    private void DrawShadow(Graphics g)
    {
        using var shadow = new SolidBrush(Color.FromArgb(48, 0, 0, 0));
        var airborne = _behavior is NimvioBehavior.Hopping or NimvioBehavior.Thrown;
        var width = 58 + 18 * _sitBlend - (airborne ? 18 : 0);
        g.FillEllipse(shadow, -width / 2, 43, width, airborne ? 7 : 10);
    }

    private void DrawForm(Graphics g)
    {
        var accentColor = Color.FromArgb(Profile.AccentArgb);
        var sleeping = _behavior == NimvioBehavior.Sleeping || !_settings.Playing;
        var sit = _sitBlend;
        var sittingOffset = 7f * sit;
        var breathing = 1f + MathF.Sin(_phase * .38f) * .012f;
        using var shell = new LinearGradientBrush(new Rectangle(-34, -43, 68, 82), Color.FromArgb(43, 48, 64), Color.FromArgb(12, 15, 22), 70f);
        using var rim = new Pen(Color.FromArgb(220, accentColor), 3f);
        using var accent = new SolidBrush(accentColor);
        using var eyeWhite = new SolidBrush(Color.FromArgb(235, 240, 249));
        using var pupil = new SolidBrush(Color.FromArgb(22, 26, 36));
        using var limb = new Pen(Color.FromArgb(220, accentColor), 5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        var step = IsMoving ? MathF.Sin(_phase * 1.55f) * 9f : 0f;
        var leftFootX = Lerp(-16 - step, -29, sit);
        var rightFootX = Lerp(16 + step, 29, sit);
        var legY = Lerp(43, 40 + sittingOffset, sit);
        g.DrawLine(limb, -13, 29 + sittingOffset, leftFootX, legY);
        g.DrawLine(limb, 13, 29 + sittingOffset, rightFootX, legY);
        g.DrawLine(limb, leftFootX, legY, leftFootX - 8, legY);
        g.DrawLine(limb, rightFootX, legY, rightFootX + 8, legY);

        g.ScaleTransform(breathing, 1);
        using (var torso = RoundedRect(new RectangleF(-24, 5 + sittingOffset, 48, 32), 14))
        {
            g.FillPath(shell, torso);
            g.DrawPath(rim, torso);
        }
        g.ScaleTransform(1 / breathing, 1);
        g.FillEllipse(accent, -5, 17 + sittingOffset, 10, 7);

        DrawArms(g, limb, accent, sittingOffset);

        var headY = -39 + sittingOffset;
        var headTilt = _behavior == NimvioBehavior.LookingAround ? MathF.Sin(_phase * .55f) * 4f : _behavior == NimvioBehavior.Thinking ? -4f : 0f;
        var state = g.Save();
        g.RotateTransform(headTilt);
        using (var head = RoundedRect(new RectangleF(-33, headY, 66, 49), 23))
        {
            g.FillPath(shell, head);
            g.DrawPath(rim, head);
        }
        g.FillEllipse(accent, -23, headY + 5, 12, 7);
        g.FillEllipse(accent, 11, headY + 5, 12, 7);
        DrawFace(g, sleeping, headY, eyeWhite, pupil, accent);
        g.Restore(state);

        DrawBehaviorProp(g, accent, rim, headY);
    }

    private void DrawArms(Graphics g, Pen limb, Brush accent, float y)
    {
        if (_behavior == NimvioBehavior.Pointing)
        {
            g.DrawLine(limb, 20, 13 + y, 39, 5 + y);
            g.DrawLine(limb, 39, 5 + y, 49, 5 + y);
        }
        else if (_behavior == NimvioBehavior.Waving)
        {
            var wave = MathF.Sin(_phase * 2.2f) * 7f;
            g.DrawLine(limb, 20, 13 + y, 35, -2 + y);
            g.DrawLine(limb, 35, -2 + y, 39 + wave, -17 + y);
        }
        else if (_behavior == NimvioBehavior.Thinking)
        {
            g.DrawLine(limb, 20, 14 + y, 29, -1 + y);
            g.FillEllipse(accent, 26, -6 + y, 7, 7);
        }
        else
        {
            var swing = IsMoving ? MathF.Sin(_phase * 1.55f) * 7f : 0f;
            g.DrawLine(limb, -20, 13 + y, -31 + swing, 25 + y);
            g.DrawLine(limb, 20, 13 + y, 31 - swing, 25 + y);
        }
    }

    private void DrawFace(Graphics g, bool sleeping, float headY, Brush eyeWhite, Brush pupil, Brush accent)
    {
        if (sleeping)
        {
            using var eyePen = new Pen(Color.White, 3.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(eyePen, -23, headY + 20, 17, 9, 15, 150);
            g.DrawArc(eyePen, 6, headY + 20, 17, 9, 15, 150);
            using var zFont = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("z", zFont, accent, 27, headY - 8);
            return;
        }

        var blinkHeight = _blinkFrames > 0 ? 3f : 19f;
        var eyeY = headY + 17 + (19 - blinkHeight) / 2;
        g.FillEllipse(eyeWhite, -24, eyeY, 18, blinkHeight);
        g.FillEllipse(eyeWhite, 6, eyeY, 18, blinkHeight);
        if (_blinkFrames == 0)
        {
            var gaze = GetGazeOffset();
            g.FillEllipse(pupil, -17 + gaze.X, headY + 23 + gaze.Y, 7, 9);
            g.FillEllipse(pupil, 13 + gaze.X, headY + 23 + gaze.Y, 7, 9);
        }

        if (_behavior == NimvioBehavior.Surprised)
        {
            using var mouth = new Pen(Color.White, 2f);
            g.DrawEllipse(mouth, -4, headY + 38, 8, 6);
        }
    }

    private void DrawBehaviorProp(Graphics g, Brush accent, Pen rim, float headY)
    {
        if (_behavior == NimvioBehavior.Searching)
        {
            using var searchPen = new Pen(Color.FromArgb(190, Color.FromArgb(Profile.AccentArgb)), 2f);
            g.DrawArc(searchPen, 35, -35, 12, 12, 20, 300);
            g.DrawLine(searchPen, 44, -24, 50, -18);
        }
        else if (_behavior == NimvioBehavior.Thinking)
        {
            using var font = new Font("Segoe UI", 12, FontStyle.Bold);
            g.DrawString("?", font, accent, 32, -48);
        }
        else if (_behavior == NimvioBehavior.Inspecting)
        {
            g.DrawEllipse(rim, -25, headY + 17, 20, 18);
            g.DrawEllipse(rim, 5, headY + 17, 20, 18);
            g.DrawLine(rim, -5, headY + 25, 5, headY + 25);
        }
        else if (_behavior == NimvioBehavior.Stumbling)
        {
            using var font = new Font("Segoe UI Symbol", 11, FontStyle.Bold);
            g.DrawString("★", font, accent, 29, -47);
        }
    }

    private PointF GetGazeOffset()
    {
        if (_behavior == NimvioBehavior.LookingAround) return new PointF(MathF.Sin(_phase * .8f) * 4f, MathF.Cos(_phase * .45f) * 2f);
        if (_behavior is NimvioBehavior.Searching or NimvioBehavior.Thinking) return new PointF(MathF.Sin(_phase * .55f) * 3.5f, MathF.Cos(_phase * .35f) * 1.5f);
        var focus = DesktopAwareness.ActiveWindowCenter() ?? Cursor.Position;
        if (Distance(Center, Cursor.Position) < 420) focus = Cursor.Position;
        var dx = Math.Clamp((focus.X - Center.X) / 180f, -1f, 1f);
        var dy = Math.Clamp((focus.Y - Center.Y) / 180f, -1f, 1f);
        if (!_facingRight) dx *= -1;
        return new PointF(dx * 3.5f, dy * 2f);
    }

    private static GraphicsPath RoundedRect(RectangleF rectangle, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
