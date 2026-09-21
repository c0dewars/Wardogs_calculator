using System.Globalization;
using System.Text.RegularExpressions;

namespace WardogsCalculator;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("--self-test"))
        {
            Calculator.SelfTest();
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new CalculatorForm());
    }
}

internal readonly record struct Position(double X, double Y);
internal readonly record struct Solution(double Distance, double Bearing, string Direction, double East, double North);

internal static class Calculator
{
    // Strict whole-input matching prevents silently accepting a partial coordinate.
    private static readonly Regex Pattern = new(
        @"^\s*(?:[AB]\s*=\s*)?\(?\s*x\s*[:=]?\s*(?<x>[+-]?\d+(?:[.,]\d+)?)\s*[,;]?\s*y\s*[:=]?\s*(?<y>[+-]?\d+(?:[.,]\d+)?)\s*\)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static bool TryParse(string text, out Position position)
    {
        position = default;
        if (text.Length > 200) return false;
        var match = Pattern.Match(text);
        if (!match.Success ||
            !double.TryParse(match.Groups["x"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(match.Groups["y"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !double.IsFinite(x) || !double.IsFinite(y)) return false;
        // Prevent overflow in distance arithmetic, well beyond practical game coordinates.
        if (Math.Abs(x) > 1e9 || Math.Abs(y) > 1e9) return false;
        position = new(x, y);
        return true;
    }

    public static Solution Calculate(Position a, Position b, bool ySouth)
    {
        double east = (b.X - a.X) * 100;
        double north = (b.Y - a.Y) * 100 * (ySouth ? -1 : 1);
        double distance = Math.Sqrt(east * east + north * north);
        if (distance == 0) return new(0, double.NaN, "—", east, north);
        double bearing = (Math.Atan2(east, north) * 180 / Math.PI + 360) % 360;
        string[] directions = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        return new(distance, bearing, directions[(int)Math.Floor((bearing + 22.5) / 45) % 8], east, north);
    }

    public static void SelfTest()
    {
        void Check(bool condition) { if (!condition) throw new Exception("Calculator self-test failed."); }
        Check(TryParse("A= x98.43, y110.38", out var a));
        Check(TryParse(" B= X94.53, Y109.03 ", out var b));
        var s = Calculate(a, b, false);
        Check(Math.Abs(s.Distance - 412.704494766) < 0.00001);
        Check(Math.Round(s.Bearing) == 251 && s.Direction == "W");
        Check(Math.Round(Calculate(a, b, true).Bearing) == 289);
        Check(!TryParse("x98.43, y", out _));
        foreach (var input in new[] { "x98,43 y110,38", "x98.43, y110,38", "x98,43, y110,38", "X=98,43; Y=110.38" })
        {
            Check(TryParse(input, out var parsed) && parsed == a);
        }
        Check(!TryParse("x98,43,21 y110,38", out _));
        Check(!TryParse("x98.43 y110,38,", out _));
        Check(!TryParse("x1 y2 extra", out _));
        Check(!TryParse("x1 y2 x3 y4", out _));
        Check(double.IsNaN(Calculate(a, a, false).Bearing));
        Check(Calculate(new(0, 0), new(0, 1), false).Bearing == 0);
        Check(Calculate(new(0, 0), new(1, 0), false).Bearing == 90);
        Check(Calculate(new(0, 0), new(0, -1), false).Bearing == 180);
        Check(Calculate(new(0, 0), new(-1, 0), false).Bearing == 270);
    }
}

internal sealed class CalculatorForm : Form
{
    private readonly TextBox locationA = new();
    private readonly TextBox locationB = new();
    private readonly Label range = new();
    private readonly Label bearing = new();
    private readonly Label direction = new();
    private readonly Label movement = new();
    private readonly Label status = new();
    private readonly CheckBox ySouth = new() { Text = "Y increases southward", AutoSize = true };
    private static readonly Color Background = Color.FromArgb(12, 19, 24);
    private static readonly Color Accent = Color.FromArgb(255, 216, 0);

    public CalculatorForm()
    {
        Text = "WARDOGS • Distance & Direction";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(760, 490);
        MinimumSize = new Size(740, 520);
        BackColor = Background;
        ForeColor = Color.FromArgb(225, 233, 236);
        Font = new Font("Segoe UI", 10);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 8 };
        foreach (int height in new[] { 42, 88, 88, 44, 114, 34, 34 })
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);
        root.Controls.Add(new Label { Text = "WARDOGS  /  COORDINATE CALCULATOR", Dock = DockStyle.Fill, ForeColor = Accent, Font = new Font("Segoe UI", 12, FontStyle.Bold) });
        root.Controls.Add(InputGroup("A  •  OUR LOCATION", locationA, "x98.43, y110.38"));
        root.Controls.Add(InputGroup("B  •  ENEMY LOCATION", locationB, "x94.53, y109.03"));
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var onTop = new CheckBox { Text = "Always on top", AutoSize = true, Margin = new Padding(0, 4, 24, 0) };
        onTop.CheckedChanged += (_, _) => TopMost = onTop.Checked;
        ySouth.Margin = new Padding(0, 4, 24, 0);
        ySouth.CheckedChanged += (_, _) => RefreshSolution();
        var clear = new Button { Text = "Clear", AutoSize = true, FlatStyle = FlatStyle.Flat };
        clear.Click += (_, _) => { locationA.Clear(); locationB.Clear(); locationA.Focus(); };
        options.Controls.AddRange([onTop, ySouth, clear]);
        root.Controls.Add(options);
        var results = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        foreach (int unused in new[] { 1, 2, 3 }) results.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        results.Controls.Add(ResultCard("DISTANCE", range));
        results.Controls.Add(ResultCard("BEARING", bearing));
        results.Controls.Add(ResultCard("DIRECTION", direction));
        root.Controls.Add(results);
        movement.Dock = DockStyle.Fill;
        movement.TextAlign = ContentAlignment.MiddleCenter;
        root.Controls.Add(movement);
        status.Dock = DockStyle.Fill;
        status.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(status);
        root.Controls.Add(new Label { Text = "100 m per grid unit • X increases eastward • Y defaults northward", Dock = DockStyle.Fill, ForeColor = Color.Silver, Font = new Font("Segoe UI", 9) });
        locationA.TextChanged += (_, _) => RefreshSolution();
        locationB.TextChanged += (_, _) => RefreshSolution();
        RefreshSolution();
    }

    private Control InputGroup(string title, TextBox input, string placeholder)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = new Padding(0, 0, 0, 10) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var label = new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        panel.Controls.Add(label, 0, 0);
        panel.SetColumnSpan(label, 2);
        input.Font = new Font("Consolas", 17);
        input.BackColor = Color.FromArgb(26, 36, 43);
        input.ForeColor = Color.White;
        input.BorderStyle = BorderStyle.FixedSingle;
        input.Dock = DockStyle.Fill;
        input.PlaceholderText = placeholder;
        input.MaxLength = 200;
        panel.Controls.Add(input, 0, 1);
        var paste = new Button { Text = "Paste", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 0, 0, 6) };
        paste.Click += (_, _) =>
        {
            try
            {
                if (!Clipboard.ContainsText()) { status.Text = "Clipboard has no text to paste."; return; }
                string text = Clipboard.GetText().Trim();
                if (text.Length > 200) { status.Text = "Clipboard text is too long for one coordinate pair."; return; }
                input.Text = text;
                input.Focus();
            }
            catch (System.Runtime.InteropServices.ExternalException) { status.Text = "Clipboard is busy. Try Paste again."; }
        };
        panel.Controls.Add(paste, 1, 1);
        return panel;
    }

    private static Control ResultCard(string title, Label output)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(17, 28, 34), Margin = new Padding(0, 0, 8, 0), Padding = new Padding(10) };
        var caption = new Label { Text = title, Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Silver };
        output.Dock = DockStyle.Fill;
        output.ForeColor = Accent;
        output.Font = new Font("Segoe UI", 28, FontStyle.Bold);
        output.TextAlign = ContentAlignment.MiddleLeft;
        output.AutoEllipsis = true;
        panel.Controls.Add(output);
        panel.Controls.Add(caption);
        return panel;
    }

    private void RefreshSolution()
    {
        range.Text = bearing.Text = direction.Text = "—";
        movement.Text = "";
        status.ForeColor = Color.Silver;
        if (string.IsNullOrWhiteSpace(locationA.Text) || string.IsNullOrWhiteSpace(locationB.Text))
        { status.Text = "Paste both locations to calculate A → B."; return; }
        if (!Calculator.TryParse(locationA.Text, out var a))
        { status.Text = "Invalid A. Expected: x98.43, y110.38 (dot or comma decimals)."; return; }
        if (!Calculator.TryParse(locationB.Text, out var b))
        { status.Text = "Invalid B. Expected: x94.53, y109.03 (dot or comma decimals)."; return; }
        var s = Calculator.Calculate(a, b, ySouth.Checked);
        range.Text = $"{s.Distance:0} m";
        if (s.Distance == 0) { status.Text = "Same location — direction is undefined."; return; }
        bearing.Text = $"{Math.Round(s.Bearing) % 360:000}°";
        direction.Text = s.Direction;
        movement.Text = $"{Math.Abs(s.East):0.#} m {(s.East < 0 ? "WEST" : "EAST")}    +    {Math.Abs(s.North):0.#} m {(s.North < 0 ? "SOUTH" : "NORTH")}";
        status.ForeColor = Color.FromArgb(74, 224, 175);
        status.Text = $"A → B  •  {s.Distance:0.00} m  •  {s.Bearing:0.0}°";
    }
}
