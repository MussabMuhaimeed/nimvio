using System.Drawing.Drawing2D;

namespace Nimvio;

internal enum NimvioBehavior
{
    Searching, Walking, Hopping, Sitting, Pointing, LookingAround, Thinking,
    Sleeping, Waving, Inspecting, ChasingCursor, FleeingCursor, HoldingCursor, Happy, Sad, Angry, Peeking,
    PlayingTogether, Arguing, Hugging, Competing, WatchingYouTube, Sliding, Hanging, Falling, Stumbling, Surprised, Thrown
}

internal enum ActiveAppAccessory { None, Pen, Book, Headphones }

internal sealed class NimvioContextMenuStrip : ContextMenuStrip
{
    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close(ToolStripDropDownCloseReason.Keyboard);
            return true;
        }
        return base.ProcessCmdKey(ref message, keyData);
    }
}

internal sealed class NimvioForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Random _random = new(Guid.NewGuid().GetHashCode());
    private readonly NimvioApplicationContext _context;
    private readonly NimvioSettings _settings;
    private readonly NimvioMind _mind;
    private readonly NimvioContextMenuStrip _menu = new() { RightToLeft = RightToLeft.No, AutoClose = true };
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
    private Point _lastCursorPosition = Cursor.Position;
    private int _cursorInteractionCooldown;
    private int _typingCooldown;
    private HashSet<Keys> _pressedTypingKeys = [];
    private bool _ignoredReactionShown;
    private IntPtr _perchedWindow;
    private Rectangle _perchedWindowBounds;
    private HashSet<IntPtr> _knownWindows = [];
    private ActiveAppAccessory _activeAccessory;
    private IntPtr _watchedYouTubeWindow;
    private string? _speechText;
    private int _speechTicks;
    private int _speechCooldown = 180;
    private int _jealousyCooldown;

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
        var returningAfterLongAbsence = DateTime.UtcNow - Profile.LastSeenUtc > TimeSpan.FromHours(4);
        if (returningAfterLongAbsence)
        {
            _behavior = NimvioBehavior.Happy;
            _behaviorTicks = 260;
            _restActionsRemaining = 2;
            _mind.MissedUser();
            ShowSpeech("You're back!", true);
        }
        else StartSearching(false);
        Profile.LastSeenUtc = DateTime.UtcNow;
        _knownWindows = DesktopAwareness.VisibleWindowHandles();

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

    private bool IsMoving => _behavior is NimvioBehavior.Walking or NimvioBehavior.Hopping or NimvioBehavior.ChasingCursor
        or NimvioBehavior.FleeingCursor or NimvioBehavior.Peeking or NimvioBehavior.Competing
        or NimvioBehavior.Sliding or NimvioBehavior.Falling;
    private bool IsSeatedBehavior => _behavior is NimvioBehavior.Sitting or NimvioBehavior.Pointing or NimvioBehavior.LookingAround
        or NimvioBehavior.Thinking or NimvioBehavior.Sleeping or NimvioBehavior.Waving or NimvioBehavior.Inspecting
        or NimvioBehavior.HoldingCursor or NimvioBehavior.Happy or NimvioBehavior.Sad or NimvioBehavior.Angry
        or NimvioBehavior.PlayingTogether or NimvioBehavior.Arguing or NimvioBehavior.Hugging or NimvioBehavior.Hanging
        or NimvioBehavior.WatchingYouTube;

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
        _menu.Items.Add("Close menu", null, (_, _) => _menu.Close(ToolStripDropDownCloseReason.CloseCalled));
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
        if (IsDisposed || Disposing) return;
        if (_speechTicks > 0) _speechTicks--;
        if (_speechCooldown > 0) _speechCooldown--;
        if (_jealousyCooldown > 0) _jealousyCooldown--;
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
        TrackPerchedWindow();
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
                if (Distance(Center, _target) < 30)
                {
                    _behavior = NimvioBehavior.HoldingCursor;
                    _behaviorTicks = 70;
                    _cursorInteractionCooldown = 520;
                    _mind.CaughtCursor();
                    ShowSpeech("Got you!");
                }
                else if (--_behaviorTicks <= 0) ArriveAndSit();
                break;
            case NimvioBehavior.FleeingCursor:
                MoveTowardsTarget();
                if (--_behaviorTicks <= 0 || Distance(Center, Cursor.Position) > 360) ArriveAndSit();
                break;
            case NimvioBehavior.Competing:
                MoveTowardsTarget();
                if (--_behaviorTicks <= 0 || Distance(Center, _target) < 18) ArriveAndSit();
                break;
            case NimvioBehavior.WatchingYouTube:
                if (Distance(Center, _target) > 18) MoveTowardsTarget();
                else SlowDown(.2f);
                if (--_behaviorTicks <= 0) _behaviorTicks = 1200;
                break;
            case NimvioBehavior.Sliding:
                SlowDown(.25f);
                if (--_behaviorTicks <= 0) ArriveAndSit();
                break;
            case NimvioBehavior.Falling:
                MoveThrown();
                break;
            case NimvioBehavior.Peeking:
                if (Distance(Center, _target) < 74)
                {
                    SlowDown(.2f);
                    if (--_behaviorTicks <= 0) ArriveAndSit();
                }
                else { MoveTowardsTarget(); _behaviorTicks--; }
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
        Profile.LastSeenUtc = DateTime.UtcNow;
        var active = DesktopAwareness.ActiveWindow();
        _activeAccessory = active is { } window ? AccessoryFor(window.ProcessName) : ActiveAppAccessory.None;
        var watchingYouTube = active is { } activeWindow && activeWindow.Title.Contains("YouTube", StringComparison.OrdinalIgnoreCase);
        if (watchingYouTube) _context.ArrangeYouTubeWatching(active!.Value);
        else if (active is not null && _behavior == NimvioBehavior.WatchingYouTube)
        {
            _watchedYouTubeWindow = IntPtr.Zero;
            ArriveAndSit();
            ShowSpeech("Video over?");
        }
        var visibleWindows = DesktopAwareness.VisibleWindowHandles();
        if (visibleWindows.Except(_knownWindows).Any() && !IsMoving && _behavior != NimvioBehavior.Sleeping)
        {
            _behavior = NimvioBehavior.Surprised;
            _behaviorTicks = 105;
            ShowSpeech("What's that?");
        }
        _knownWindows = visibleWindows;
        if (!_ignoredReactionShown && DateTime.UtcNow - Profile.LastInteractionUtc > TimeSpan.FromMinutes(20)
            && _behavior != NimvioBehavior.Sleeping && !IsMoving)
        {
            _behavior = NimvioBehavior.Sad;
            _behaviorTicks = 260;
            _restActionsRemaining = Math.Max(1, _restActionsRemaining);
            _ignoredReactionShown = true;
            _mind.FeltIgnored();
        }
        var shouldHide = _settings.PauseInFullscreen && DesktopAwareness.IsForeignFullscreen();
        Opacity = shouldHide ? 0 : 1;
        if (_random.Next(5) == 0) SaveSettings();
    }

    private static ActiveAppAccessory AccessoryFor(string processName)
    {
        var name = processName.ToLowerInvariant();
        if (name is "spotify" or "vlc" or "wmplayer" or "musicbee" or "itunes") return ActiveAppAccessory.Headphones;
        if (name is "code" or "devenv" or "notepad" or "notepad++" or "winword" or "rider64" or "idea64") return ActiveAppAccessory.Pen;
        if (name is "chrome" or "msedge" or "firefox" or "acrord32" or "acrobat" or "sumatrapdf") return ActiveAppAccessory.Book;
        return ActiveAppAccessory.None;
    }

    private void TrackPerchedWindow()
    {
        if (_perchedWindow == IntPtr.Zero || _behavior is NimvioBehavior.Falling or NimvioBehavior.Thrown) return;
        if (!DesktopAwareness.TryGetWindowRectangle(_perchedWindow, out var bounds))
        {
            if (_behavior is NimvioBehavior.Walking or NimvioBehavior.Hopping)
            {
                _perchedWindow = IntPtr.Zero;
                return;
            }
            StartFalling();
            return;
        }
        var dx = bounds.Left - _perchedWindowBounds.Left;
        var dy = bounds.Top - _perchedWindowBounds.Top;
        if (dx == 0 && dy == 0) return;
        _perchedWindowBounds = bounds;
        if (_behavior is NimvioBehavior.Walking or NimvioBehavior.Hopping)
        {
            _target = new PointF(_target.X + dx, _target.Y + dy);
            return;
        }
        var movement = Math.Abs(dx) + Math.Abs(dy);
        if (movement > 150)
        {
            StartFalling();
            return;
        }
        Location = KeepVisible(new Point(Left + dx, Top + dy));
        if (movement > 45)
        {
            _behavior = NimvioBehavior.Hanging;
            _behaviorTicks = 75;
        }
        else
        {
            _behavior = NimvioBehavior.Sliding;
            _behaviorTicks = 32;
        }
    }

    private void StartFalling()
    {
        _perchedWindow = IntPtr.Zero;
        _velocity = new PointF(_velocity.X * .3f, 2.5f);
        _behavior = NimvioBehavior.Falling;
        _behaviorTicks = 90;
        _mind.Startled();
    }

    private void StartSocialInteraction(NimvioForm friend)
    {
        var roll = _random.Next(100);
        var relationship = Profile.Relationships.GetValueOrDefault(friend.Profile.Id);
        var interaction = relationship > 30
            ? roll < 58 ? NimvioBehavior.Hugging : NimvioBehavior.PlayingTogether
            : relationship < -20
                ? roll < 20 ? NimvioBehavior.Hugging : roll < 62 ? NimvioBehavior.Competing : NimvioBehavior.Arguing
                : Profile.Personality == NimvioPersonality.Playful
            ? roll < 42 ? NimvioBehavior.PlayingTogether : roll < 68 ? NimvioBehavior.Competing : roll < 86 ? NimvioBehavior.Hugging : NimvioBehavior.Arguing
            : Profile.Personality == NimvioPersonality.Calm
                ? roll < 48 ? NimvioBehavior.Hugging : roll < 75 ? NimvioBehavior.PlayingTogether : roll < 92 ? NimvioBehavior.Arguing : NimvioBehavior.Competing
                : roll < 38 ? NimvioBehavior.Arguing : roll < 68 ? NimvioBehavior.PlayingTogether : roll < 88 ? NimvioBehavior.Hugging : NimvioBehavior.Competing;
        BeginSocialInteraction(friend, interaction, false);
        friend.BeginSocialInteraction(this, interaction, true);
        _context.NotifySocialInteraction(this, friend);
    }

    internal void BeginSocialInteraction(NimvioForm friend, NimvioBehavior interaction, bool mirrored)
    {
        _facingRight = friend.WorldCenter.X >= Center.X;
        _behavior = interaction;
        _behaviorTicks = interaction == NimvioBehavior.Competing ? 190 : 180;
        _restActionsRemaining = Math.Max(1, _restActionsRemaining);
        if (interaction == NimvioBehavior.Competing)
        {
            var direction = mirrored ? -1 : 1;
            _target = new PointF(Center.X + direction * 135, Center.Y - 12);
        }
        var previousRelationship = Profile.Relationships.GetValueOrDefault(friend.Profile.Id);
        var relationshipChange = interaction switch
        {
            NimvioBehavior.Hugging => previousRelationship < 0 ? 9f : 5f,
            NimvioBehavior.PlayingTogether => 3f,
            NimvioBehavior.Arguing => -3f,
            NimvioBehavior.Competing => -1f,
            _ => 0f
        };
        var updatedRelationship = Math.Clamp(previousRelationship + relationshipChange, -100, 100);
        Profile.Relationships[friend.Profile.Id] = updatedRelationship;
        if (updatedRelationship > 25 && (Profile.FavoriteFriendId is null
            || updatedRelationship > Profile.Relationships.GetValueOrDefault(Profile.FavoriteFriendId)))
            Profile.FavoriteFriendId = friend.Profile.Id;
        if (interaction == NimvioBehavior.Hugging && previousRelationship < -10)
            ShowSpeech("Friends again?", true);
        else if (interaction == NimvioBehavior.PlayingTogether)
            ShowSpeech("Let's play!");
        _mind.Socialized();
        SaveSettings();
    }

    internal void ObserveFriendsInteraction(NimvioForm first, NimvioForm second)
    {
        if (_jealousyCooldown > 0 || Profile.FavoriteFriendId is null) return;
        var favorite = first.Profile.Id == Profile.FavoriteFriendId ? first
            : second.Profile.Id == Profile.FavoriteFriendId ? second : null;
        if (favorite is null) return;
        _facingRight = favorite.WorldCenter.X >= Center.X;
        _behavior = NimvioBehavior.Sad;
        _behaviorTicks = 150;
        _restActionsRemaining = Math.Max(1, _restActionsRemaining);
        _jealousyCooldown = 1800;
        _mind.Startled();
        ShowSpeech("What about me?", true);
    }

    private void WatchForInteraction()
    {
        if (_cursorInteractionCooldown > 0) _cursorInteractionCooldown--;
        if (_typingCooldown > 0) _typingCooldown--;
        if (_behavior == NimvioBehavior.WatchingYouTube) return;
        var cursor = Cursor.Position;
        var cursorMoved = Distance(cursor, _lastCursorPosition) > 3;
        _lastCursorPosition = cursor;

        if (_cursorInteractionCooldown == 0 && cursorMoved && Distance(Center, cursor) < 245
            && _behavior != NimvioBehavior.Sleeping && _behavior is not NimvioBehavior.HoldingCursor and not NimvioBehavior.Happy)
        {
            Profile.LastInteractionUtc = DateTime.UtcNow;
            _ignoredReactionShown = false;
            var fleeChance = Profile.Personality == NimvioPersonality.Calm ? .65
                : Profile.Personality == NimvioPersonality.Curious ? .25 : .12;
            if (_random.NextDouble() < fleeChance)
            {
                var dx = Center.X - cursor.X;
                var dy = Center.Y - cursor.Y;
                var length = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
                _target = new PointF(Center.X + dx / length * 310, Center.Y + dy / length * 240);
                _behavior = NimvioBehavior.FleeingCursor;
                _behaviorTicks = 150;
            }
            else
            {
                _behavior = NimvioBehavior.ChasingCursor;
                _behaviorTicks = 210;
            }
            _restActionsRemaining = Math.Max(1, _restActionsRemaining);
            return;
        }

        var pressedKeys = DesktopAwareness.PressedTypingKeys();
        var startedTyping = pressedKeys.Except(_pressedTypingKeys).Any();
        _pressedTypingKeys = pressedKeys;
        var caret = startedTyping ? DesktopAwareness.ActiveCaretPosition() : null;
        if (_typingCooldown == 0 && caret is Point typingPoint && Distance(Center, typingPoint) < 520
            && _behavior != NimvioBehavior.Sleeping && _behavior is not NimvioBehavior.ChasingCursor and not NimvioBehavior.HoldingCursor)
        {
            var side = Center.X <= typingPoint.X ? -1 : 1;
            _target = new PointF(typingPoint.X + side * 82, typingPoint.Y + 48);
            _facingRight = typingPoint.X >= Center.X;
            _behavior = NimvioBehavior.Peeking;
            _behaviorTicks = 210;
            _typingCooldown = 150;
            _restActionsRemaining = Math.Max(1, _restActionsRemaining);
            return;
        }

        var local = PointToClient(Cursor.Position);
        if (ClientRectangle.Contains(local)) _hoverTicks++;
        else _hoverTicks = 0;
        if (_hoverTicks > 90 && IsSeatedBehavior && _behavior != NimvioBehavior.Sleeping)
        {
            _hoverTicks = -250;
            _behavior = NimvioBehavior.Waving;
            _behaviorTicks = 150;
            _mind.Comforted();
            Profile.LastInteractionUtc = DateTime.UtcNow;
            _ignoredReactionShown = false;
        }

        if (--_socialTicks <= 0)
        {
            _socialTicks = _random.Next(150, 300);
            var friend = _context.FindNearbyForm(this, 230);
            if (friend is not null && _behavior != NimvioBehavior.Sleeping && !IsMoving)
            {
                StartSocialInteraction(friend);
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
        var speed = (_behavior == NimvioBehavior.Hopping ? 4.7f : _behavior == NimvioBehavior.ChasingCursor ? 5.8f
            : _behavior == NimvioBehavior.FleeingCursor ? 5.1f : _behavior == NimvioBehavior.Peeking ? 3.8f
            : _behavior == NimvioBehavior.Competing ? 4.5f : 3.15f) * _settings.Speed * activity * personality;
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
            _velocity = PointF.Empty;
            _behavior = _random.Next(2) == 0 ? NimvioBehavior.Sad : NimvioBehavior.Angry;
            _behaviorTicks = _random.Next(150, 260);
            _restActionsRemaining = Math.Max(1, _restActionsRemaining);
            _mind.Startled();
            ShowSpeech(_behavior == NimvioBehavior.Sad ? "That hurt..." : "Hey!");
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
        _perchedWindow = IntPtr.Zero;
        var area = screen.WorkingArea;
        var marginX = Width / 2 + 8;
        var marginY = Height / 2 + 5;
        var windows = DesktopAwareness.VisibleWindows(screen);
        var activeWindow = DesktopAwareness.ActiveWindow();
        var activeLedgeChance = Profile.Personality == NimvioPersonality.Curious ? .72
            : Profile.Personality == NimvioPersonality.Playful ? .52 : .38;
        if (activeWindow is { } activeWindowInfo && screen.Bounds.IntersectsWith(activeWindowInfo.Bounds)
            && activeWindowInfo.Bounds is var active
            && active.Top > area.Top + Height && active.Top < area.Bottom - Height / 2
            && _random.NextDouble() < activeLedgeChance)
        {
            var left = Math.Max(area.Left + marginX, active.Left + marginX);
            var right = Math.Min(area.Right - marginX, active.Right - marginX);
            if (right > left)
            {
                _perchedWindow = activeWindowInfo.Handle;
                _perchedWindowBounds = active;
                return new PointF(Math.Clamp(Cursor.Position.X, left, right), active.Top - marginY + 5);
            }
        }

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
        if (_behavior == NimvioBehavior.Hanging)
        {
            StartFalling();
            return;
        }
        if (_behavior == NimvioBehavior.HoldingCursor)
        {
            _behavior = NimvioBehavior.Happy;
            _behaviorTicks = 150;
            return;
        }
        if (_behavior == NimvioBehavior.Happy)
        {
            ArriveAndSit();
            return;
        }
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
            _behavior = Profile.Personality switch
            {
                NimvioPersonality.Curious => roll switch
                {
                    < 32 => NimvioBehavior.Inspecting, < 55 => NimvioBehavior.LookingAround,
                    < 72 => NimvioBehavior.Thinking, < 87 => NimvioBehavior.Pointing,
                    < 94 => NimvioBehavior.Waving, _ => NimvioBehavior.Sleeping
                },
                NimvioPersonality.Playful => roll switch
                {
                    < 29 => NimvioBehavior.ChasingCursor, < 52 => NimvioBehavior.Waving,
                    < 72 => NimvioBehavior.Pointing, < 86 => NimvioBehavior.LookingAround,
                    < 95 => NimvioBehavior.Inspecting, _ => NimvioBehavior.Sleeping
                },
                _ => roll switch
                {
                    < 36 => NimvioBehavior.Sleeping, < 58 => NimvioBehavior.Thinking,
                    < 75 => NimvioBehavior.Sitting, < 87 => NimvioBehavior.LookingAround,
                    < 95 => NimvioBehavior.Waving, _ => NimvioBehavior.Pointing
                }
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
        if (_behavior is NimvioBehavior.Thrown or NimvioBehavior.Falling) e.Graphics.RotateTransform(MathF.Sin(_phase) * 18f);
        if (_behavior == NimvioBehavior.Stumbling) e.Graphics.RotateTransform(MathF.Sin(_phase * 2f) * 12f);

        var bob = IsMoving ? MathF.Abs(MathF.Sin(_phase)) * -4f : MathF.Sin(_phase * .42f) * 1.2f;
        e.Graphics.TranslateTransform(0, bob);
        DrawShadow(e.Graphics);
        DrawForm(e.Graphics);
    }

    private void DrawShadow(Graphics g)
    {
        using var shadow = new SolidBrush(Color.FromArgb(48, 0, 0, 0));
        var airborne = _behavior is NimvioBehavior.Hopping or NimvioBehavior.Thrown or NimvioBehavior.Falling;
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
        var headTilt = _behavior == NimvioBehavior.LookingAround ? MathF.Sin(_phase * .55f) * 4f
            : _behavior == NimvioBehavior.Thinking ? -4f : _behavior == NimvioBehavior.Peeking ? 7f : 0f;
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
        DrawActiveAppAccessory(g, accent, rim, headY);
        DrawSpeechBubble(g);
    }

    private void DrawArms(Graphics g, Pen limb, Brush accent, float y)
    {
        if (_behavior == NimvioBehavior.Angry)
        {
            g.DrawLine(limb, -20, 13 + y, 16, 24 + y);
            g.DrawLine(limb, 20, 13 + y, -16, 24 + y);
        }
        else if (_behavior == NimvioBehavior.Hanging)
        {
            g.DrawLine(limb, -19, 12 + y, -28, -24 + y);
            g.DrawLine(limb, 19, 12 + y, 28, -24 + y);
        }
        else if (_behavior == NimvioBehavior.Hugging)
        {
            g.DrawArc(limb, -38, -2 + y, 30, 25, 165, 105);
            g.DrawArc(limb, 8, -2 + y, 30, 25, 270, 105);
        }
        else if (_behavior is NimvioBehavior.Arguing or NimvioBehavior.PlayingTogether)
        {
            var gesture = MathF.Sin(_phase * 1.8f) * 6f;
            g.DrawLine(limb, -20, 13 + y, -34, 7 + y + gesture);
            g.DrawLine(limb, 20, 13 + y, 39, 2 + y - gesture);
        }
        else if (_behavior is NimvioBehavior.HoldingCursor or NimvioBehavior.Happy)
        {
            var cheer = _behavior == NimvioBehavior.Happy ? MathF.Sin(_phase * 2f) * 3f : 0f;
            g.DrawLine(limb, -20, 12 + y, -34, -5 + y - cheer);
            g.DrawLine(limb, 20, 12 + y, 34, -5 + y - cheer);
        }
        else if (_behavior == NimvioBehavior.Peeking)
        {
            g.DrawLine(limb, -20, 13 + y, -31, 23 + y);
            g.DrawLine(limb, 20, 13 + y, 33, 4 + y);
        }
        else if (_behavior == NimvioBehavior.Pointing)
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
            var swing = IsMoving || _behavior == NimvioBehavior.Competing ? MathF.Sin(_phase * 1.55f) * 7f : 0f;
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

        if (_behavior == NimvioBehavior.Angry)
        {
            using var expression = new Pen(Color.White, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(expression, -23, headY + 17, -8, headY + 22);
            g.DrawLine(expression, 8, headY + 22, 23, headY + 17);
            g.DrawLine(expression, -7, headY + 41, 7, headY + 41);
        }
        else if (_behavior == NimvioBehavior.Sad)
        {
            using var sadMouth = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(sadMouth, -8, headY + 38, 16, 9, 195, 150);
            using var tear = new SolidBrush(Color.FromArgb(185, 115, 205, 255));
            g.FillEllipse(tear, 20, headY + 34, 4, 7);
        }
        else if (_behavior is NimvioBehavior.HoldingCursor or NimvioBehavior.Happy or NimvioBehavior.Hugging or NimvioBehavior.PlayingTogether)
        {
            using var smile = new Pen(Color.White, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(smile, -8, headY + 32, 16, 10, 15, 150);
        }
        else if (_behavior == NimvioBehavior.Surprised)
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
        else if (_behavior == NimvioBehavior.HoldingCursor)
        {
            using var holdPen = new Pen(Color.FromArgb(210, Color.White), 2f);
            g.DrawArc(holdPen, -42, -15, 22, 22, 300, 110);
            g.DrawArc(holdPen, 20, -15, 22, 22, 130, 110);
        }
        else if (_behavior == NimvioBehavior.Happy)
        {
            using var font = new Font("Segoe UI Symbol", 10, FontStyle.Bold);
            g.DrawString("♥", font, accent, 30, -50);
            g.DrawString("♥", font, accent, -43, -42);
        }
        else if (_behavior == NimvioBehavior.Peeking)
        {
            using var font = new Font("Segoe UI", 10, FontStyle.Bold | FontStyle.Italic);
            g.DrawString("...", font, accent, 28, -48);
        }
        else if (_behavior == NimvioBehavior.FleeingCursor)
        {
            using var font = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("!", font, accent, 32, -48);
        }
        else if (_behavior == NimvioBehavior.PlayingTogether)
        {
            g.FillEllipse(accent, 32, 2 + MathF.Sin(_phase * 1.7f) * 9f, 10, 10);
        }
        else if (_behavior == NimvioBehavior.Arguing)
        {
            using var font = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString("!?", font, accent, 27, -50);
        }
        else if (_behavior == NimvioBehavior.Hugging)
        {
            using var font = new Font("Segoe UI Symbol", 11, FontStyle.Bold);
            g.DrawString("♥", font, accent, 29, -48);
        }
        else if (_behavior == NimvioBehavior.Competing)
        {
            using var font = new Font("Segoe UI Symbol", 10, FontStyle.Bold);
            g.DrawString("⚑", font, accent, 29, -48);
        }
        else if (_behavior == NimvioBehavior.Sad)
        {
            using var font = new Font("Segoe UI", 9, FontStyle.Bold);
            g.DrawString("...", font, accent, 28, -46);
        }
        else if (_behavior == NimvioBehavior.Angry)
        {
            using var steam = new Pen(Color.FromArgb(205, Color.White), 2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawArc(steam, 31, -46, 10, 15, 90, 180);
            g.DrawArc(steam, 39, -51, 10, 15, 90, 180);
        }
        else if (_behavior == NimvioBehavior.WatchingYouTube)
        {
            using var bowl = new SolidBrush(Color.FromArgb(230, 220, 60, 60));
            using var popcorn = new SolidBrush(Color.FromArgb(245, 255, 238, 170));
            g.FillPolygon(bowl, [new PointF(18, 21), new PointF(42, 21), new PointF(38, 38), new PointF(22, 38)]);
            g.FillEllipse(popcorn, 19, 14, 9, 9);
            g.FillEllipse(popcorn, 27, 12, 10, 10);
            g.FillEllipse(popcorn, 35, 15, 8, 8);
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
        if (_behavior == NimvioBehavior.WatchingYouTube && DesktopAwareness.TryGetWindowRectangle(_watchedYouTubeWindow, out var videoWindow))
        {
            var videoFocus = new PointF(videoWindow.Left + videoWindow.Width / 2f, videoWindow.Top + videoWindow.Height / 2f);
            var watchX = Math.Clamp((videoFocus.X - Center.X) / 180f, -1f, 1f);
            var watchY = Math.Clamp((videoFocus.Y - Center.Y) / 180f, -1f, 1f);
            if (!_facingRight) watchX *= -1;
            return new PointF(watchX * 4f, watchY * 2.5f);
        }
        if (_behavior is NimvioBehavior.Searching or NimvioBehavior.Thinking) return new PointF(MathF.Sin(_phase * .55f) * 3.5f, MathF.Cos(_phase * .35f) * 1.5f);
        if (_behavior == NimvioBehavior.Peeking)
        {
            var caret = DesktopAwareness.ActiveCaretPosition();
            if (caret is Point point)
            {
                var peekX = Math.Clamp((point.X - Center.X) / 120f, -1f, 1f);
                var peekY = Math.Clamp((point.Y - Center.Y) / 120f, -1f, 1f);
                if (!_facingRight) peekX *= -1;
                return new PointF(peekX * 5f, peekY * 3f);
            }
        }
        var focus = DesktopAwareness.ActiveWindowCenter() ?? Cursor.Position;
        if (Distance(Center, Cursor.Position) < 420) focus = Cursor.Position;
        var dx = Math.Clamp((focus.X - Center.X) / 180f, -1f, 1f);
        var dy = Math.Clamp((focus.Y - Center.Y) / 180f, -1f, 1f);
        if (!_facingRight) dx *= -1;
        return new PointF(dx * 3.5f, dy * 2f);
    }

    internal void BeginYouTubeWatching(DesktopAwareness.WindowSnapshot window, int index, int viewerCount)
    {
        if (_watchedYouTubeWindow == window.Handle && _behavior == NimvioBehavior.WatchingYouTube) return;
        var spacing = window.Bounds.Width / (float)(viewerCount + 1);
        var x = window.Bounds.Left + spacing * (index + 1);
        var y = window.Bounds.Top - Height / 2f + 5;
        _target = new PointF(x, y);
        _perchedWindow = window.Handle;
        _perchedWindowBounds = window.Bounds;
        _watchedYouTubeWindow = window.Handle;
        _facingRight = window.Bounds.Left + window.Bounds.Width / 2f >= Center.X;
        _behavior = NimvioBehavior.WatchingYouTube;
        _behaviorTicks = 1800;
        _restActionsRemaining = 2;
        ShowSpeech(index == 0 ? "Let's watch!" : "Make room!", index == 0);
    }

    private void ShowSpeech(string text, bool force = false)
    {
        if (!force && (_speechCooldown > 0 || _random.NextDouble() > .38)) return;
        _speechText = text;
        _speechTicks = 190;
        _speechCooldown = _random.Next(700, 1300);
    }

    private void DrawActiveAppAccessory(Graphics g, Brush accent, Pen rim, float headY)
    {
        if (_behavior == NimvioBehavior.Sleeping)
        {
            using var bubble = new SolidBrush(Color.FromArgb(225, 245, 250, 255));
            using var outline = new Pen(Color.FromArgb(150, Color.White), 1.5f);
            g.FillEllipse(bubble, 31, headY - 24, 31, 23);
            g.DrawEllipse(outline, 31, headY - 24, 31, 23);
            g.FillEllipse(bubble, 25, headY - 5, 7, 7);
            using var fish = new SolidBrush(Color.FromArgb(Profile.AccentArgb));
            g.FillEllipse(fish, 40, headY - 17, 12, 7);
            g.FillPolygon(fish, [new PointF(40, headY - 14), new PointF(35, headY - 19), new PointF(35, headY - 9)]);
            using var eye = new SolidBrush(Color.FromArgb(30, 35, 45));
            g.FillEllipse(eye, 49, headY - 15, 2, 2);
            return;
        }
        if (_behavior == NimvioBehavior.WatchingYouTube)
        {
            if (Profile.Personality != NimvioPersonality.Calm) return;
            DrawHeadphones(g, headY, false);
            return;
        }

        switch (_activeAccessory)
        {
            case ActiveAppAccessory.Pen:
                using (var pen = new Pen(Color.FromArgb(235, 245, 248, 255), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(pen, 30, 17, 45, -2);
                break;
            case ActiveAppAccessory.Book:
                using (var page = new SolidBrush(Color.FromArgb(230, 242, 238, 222)))
                {
                    g.FillPolygon(page, [new PointF(-27, 14), new PointF(-2, 19), new PointF(-2, 37), new PointF(-28, 31)]);
                    g.FillPolygon(page, [new PointF(2, 19), new PointF(27, 14), new PointF(28, 31), new PointF(2, 37)]);
                }
                g.DrawLine(rim, 0, 19, 0, 37);
                break;
            case ActiveAppAccessory.Headphones:
                DrawHeadphones(g, headY, true);
                break;
        }
    }

    private void DrawHeadphones(Graphics g, float headY, bool showMusicNotes)
    {
        var accentColor = Color.FromArgb(Profile.AccentArgb);
        using var outerBand = new Pen(Color.FromArgb(245, accentColor), 5.5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var innerBand = new Pen(Color.FromArgb(210, 18, 22, 31), 2.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawArc(outerBand, -35, headY - 12, 70, 51, 194, 152);
        g.DrawArc(innerBand, -30, headY - 6, 60, 41, 198, 144);

        using var cupShell = new SolidBrush(Color.FromArgb(245, accentColor));
        using var cupShadow = new SolidBrush(Color.FromArgb(235, 15, 19, 28));
        using var cushion = new SolidBrush(Color.FromArgb(245, 43, 48, 62));
        using var highlight = new Pen(Color.FromArgb(185, Color.White), 1.5f);

        using (var leftCup = RoundedRect(new RectangleF(-39, headY + 17, 14, 23), 6))
        using (var rightCup = RoundedRect(new RectangleF(25, headY + 17, 14, 23), 6))
        {
            g.FillPath(cupShell, leftCup);
            g.FillPath(cupShell, rightCup);
            g.DrawPath(highlight, leftCup);
            g.DrawPath(highlight, rightCup);
        }
        g.FillEllipse(cupShadow, -36, headY + 21, 9, 16);
        g.FillEllipse(cupShadow, 27, headY + 21, 9, 16);
        g.FillEllipse(cushion, -34, headY + 23, 6, 12);
        g.FillEllipse(cushion, 28, headY + 23, 6, 12);

        if (!showMusicNotes) return;
        var pulse = MathF.Sin(_phase * 1.4f) * 2f;
        using var noteBrush = new SolidBrush(Color.FromArgb(210, accentColor));
        using var noteFont = new Font("Segoe UI Symbol", 9, FontStyle.Bold);
        g.DrawString("\u266a", noteFont, noteBrush, 38, headY - 9 + pulse);
        g.DrawString("\u266a", noteFont, noteBrush, -50, headY + 2 - pulse);
    }

    private void DrawSpeechBubble(Graphics g)
    {
        if (_speechTicks <= 0 || string.IsNullOrWhiteSpace(_speechText)) return;
        var rectangle = new RectangleF(-51, -56, 102, 20);
        using var bubble = new SolidBrush(Color.FromArgb(242, 250, 252, 255));
        using var outline = new Pen(Color.FromArgb(175, Color.FromArgb(Profile.AccentArgb)), 1.5f);
        using var textBrush = new SolidBrush(Color.FromArgb(28, 32, 43));
        using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var path = RoundedRect(rectangle, 7);
        g.FillPath(bubble, path);
        g.DrawPath(outline, path);
        g.FillPolygon(bubble, [new PointF(-5, -36), new PointF(5, -36), new PointF(0, -31)]);
        g.DrawString(_speechText, font, textBrush, rectangle, format);
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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _timer.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Tick -= TickForm;
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
