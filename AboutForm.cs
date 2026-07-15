using System.Reflection;

namespace Nimvio;

internal sealed class AboutForm : Form
{
    private readonly List<Image> _images = [];

    private AboutForm()
    {
        Text = "About Nimvio";
        ClientSize = new Size(930, 650);
        MinimumSize = new Size(850, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(10, 13, 21);
        ForeColor = Color.FromArgb(238, 244, 252);
        Font = new Font("Segoe UI", 10f);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        Controls.Add(BuildLayout());
    }

    public static void ShowAbout(IWin32Window? owner = null)
    {
        using var form = new AboutForm();
        if (owner is null) form.ShowDialog();
        else form.ShowDialog(owner);
    }

#if DEBUG
    internal static void SavePreview(string path)
    {
        using var form = new AboutForm();
        form.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(path);
        form.Hide();
    }
#endif

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            Padding = new Padding(26, 20, 26, 20),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        var title = new Label
        {
            Text = "Nimvio",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 28f, FontStyle.Bold),
            ForeColor = Color.FromArgb(86, 221, 242)
        };
        root.Controls.Add(title, 0, 0);

        var gallery = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 8, 0, 8)
        };
        gallery.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        gallery.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        gallery.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334f));
        gallery.Controls.Add(CreateProfileCard("Nova", "Cyan", "Curious", "Nimvio.Assets.nova-cyan.png", Color.FromArgb(86, 221, 242)), 0, 0);
        gallery.Controls.Add(CreateProfileCard("Mimo", "Orange", "Playful", "Nimvio.Assets.mimo-orange.png", Color.FromArgb(255, 176, 76)), 1, 0);
        gallery.Controls.Add(CreateProfileCard("Lumi", "Purple", "Calm", "Nimvio.Assets.lumi-purple.png", Color.FromArgb(211, 126, 255)), 2, 0);
        root.Controls.Add(gallery, 0, 1);

        var projectInfo = new Label
        {
            Text = "Nimvio — Your curious desktop companions     •     Created by: Mussab Muhaimeed     •     Version: 26.8",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(174, 187, 207),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular)
        };
        root.Controls.Add(projectInfo, 0, 2);

        var close = new Button
        {
            Text = "Close",
            Anchor = AnchorStyles.None,
            Size = new Size(126, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(86, 221, 242),
            ForeColor = Color.FromArgb(10, 13, 21),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK
        };
        close.FlatAppearance.BorderSize = 0;
        root.Controls.Add(close, 0, 3);
        AcceptButton = close;
        CancelButton = close;
        return root;
    }

    private Control CreateProfileCard(string name, string colorName, string personality, string resourceName, Color accent)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(9),
            Padding = new Padding(10),
            BackColor = Color.FromArgb(17, 21, 33)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = card.BackColor,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        var image = LoadEmbeddedImage(resourceName);
        _images.Add(image);
        var picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = image,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = card.BackColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(picture, 0, 0);
        layout.Controls.Add(CreateLabel(name, 18f, FontStyle.Bold, Color.White), 0, 1);
        layout.Controls.Add(CreateLabel(colorName, 11f, FontStyle.Bold, accent), 0, 2);
        layout.Controls.Add(CreateLabel(personality, 9.5f, FontStyle.Regular, Color.FromArgb(165, 177, 197)), 0, 3);
        card.Controls.Add(layout);
        return card;
    }

    private static Label CreateLabel(string text, float size, FontStyle style, Color color) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", size, style),
        ForeColor = color,
        BackColor = Color.Transparent
    };

    private static Image LoadEmbeddedImage(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded image: {resourceName}");
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var image in _images) image.Dispose();
            Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
