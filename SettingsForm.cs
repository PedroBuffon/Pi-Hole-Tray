using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PiHoleTray;

class SettingsForm : Form
{
    // ── Layout constants ──────────────────────────────────────────────────────
    private const int FW   = 640;   // form width
    private const int FH   = 560;   // form height
    private const int TH   = 52;    // title bar height
    private const int BH   = 64;    // button bar height
    private const int Pad  = 24;    // outer padding
    private const int CW   = 287;   // column width (each side)
    private const int CGap = 16;    // gap between columns
    private const int LX   = Pad;            // left column x
    private const int RX   = Pad + CW + CGap; // right column x  (= 327)
    private const int InH  = 36;    // input / combobox height

    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color CBg      = Color.White;
    private static readonly Color CBorder  = Color.FromArgb(210, 210, 220);
    private static readonly Color CAccent  = Color.FromArgb(16,  124,  64);
    private static readonly Color CAccentH = Color.FromArgb(10,   96,  50);
    private static readonly Color CInputBd = Color.FromArgb(190, 190, 205);
    private static readonly Color CInputBg = Color.FromArgb(248, 248, 252);
    private static readonly Color CTxt     = Color.FromArgb(20,  20,  24);
    private static readonly Color CTxt2    = Color.FromArgb(80,  80,  96);
    private static readonly Color CTxt3    = Color.FromArgb(140, 140, 158);
    private static readonly Color CRed     = Color.FromArgb(196,  40,  40);
    private static readonly Color COrange  = Color.FromArgb(184, 112,   0);
    private static readonly Color CBtnBg   = Color.FromArgb(234, 234, 242);
    private static readonly Color CBtnH    = Color.FromArgb(216, 216, 228);

    // ── Instance fonts (non-static prevents disposed-font crash on re-open) ──
    private readonly Font _fTitle   = new("Segoe UI Semibold", 11f);
    private readonly Font _fSection = new("Segoe UI Semibold", 8.5f);
    private readonly Font _fLabel   = new("Segoe UI", 9f);
    private readonly Font _fHint    = new("Segoe UI", 7.5f);
    private readonly Font _fInput   = new("Segoe UI", 10f);
    private readonly Font _fBtn     = new("Segoe UI", 9.5f);

    // ── State ─────────────────────────────────────────────────────────────────
    private AppConfig              _cfg;
    private readonly string        _lang;
    private readonly string        _iconState;
    private readonly Action<AppConfig>? _onSave;

    // ── Direct control references (plain TextBox avoids Text-hiding bugs) ────
    private TextBox       _urlTb     = null!;
    private TextBox       _pwTb      = null!;
    private RadioButton   _rbV6      = null!;
    private RadioButton   _rbV5      = null!;
    private NumericUpDown _pollNud   = null!;
    private CheckBox      _autoChk   = null!;
    private ComboBox      _langCombo = null!;
    private Label         _statusLbl = null!;

    private Point _drag;

    // ── Constructor ───────────────────────────────────────────────────────────

    public SettingsForm(AppConfig cfg, string iconState, Action<AppConfig>? onSave)
    {
        _cfg       = cfg;
        _lang      = Loc.GetEffectiveLang(cfg.Language);
        _iconState = iconState;
        _onSave    = onSave;
        Build();
    }

    // ── Form setup ────────────────────────────────────────────────────────────

    private void Build()
    {
        SuspendLayout();
        FormBorderStyle = FormBorderStyle.None;
        BackColor       = CBg;
        TopMost         = true;
        ClientSize      = new Size(FW, FH);
        StartPosition   = FormStartPosition.Manual;
        Font            = _fLabel;
        KeyPreview      = true;
        KeyDown        += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        // Surface panel (1 px inset so OnPaint border is visible)
        var s = new Panel { Location = new Point(1, 1), Size = new Size(FW-2, FH-2), BackColor = CBg };
        Controls.Add(s);

        BuildTitleBar(s);
        BuildContent(s);
        BuildButtonBar(s);

        ResumeLayout();
        PositionNearTaskbar();
    }

    // ── Title bar ─────────────────────────────────────────────────────────────

    private void BuildTitleBar(Panel s)
    {
        var bar = new Panel { Bounds = new Rectangle(0, 0, FW-2, TH), BackColor = CBg };
        s.Controls.Add(bar);
        Draggable(bar);

        // Small icon
        try
        {
            var ico = IconRenderer.GetIcon(_iconState, 22);
            var pb  = new PictureBox { Bounds = new Rectangle(Pad, (TH-22)/2, 22, 22),
                                       Image = ico.ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom,
                                       BackColor = Color.Transparent };
            bar.Controls.Add(pb);
            Draggable(pb);
        }
        catch { }

        // Title text
        var lbl = new Label { Text = Loc.T("title", _lang), Font = _fTitle, ForeColor = CTxt,
                              AutoSize = true, Location = new Point(Pad+28, (TH-20)/2),
                              BackColor = Color.Transparent };
        bar.Controls.Add(lbl);
        Draggable(lbl);

        // Close button (styled button with red hover)
        var close = MkBtn("  ✕  ", CBg, CTxt3);
        close.Font = new Font("Segoe UI", 12f);
        close.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
        close.Bounds = new Rectangle(FW-2-52, 0, 52, TH);
        close.ForeColor = CTxt3;
        close.MouseEnter += (_, _) => close.ForeColor = Color.White;
        close.MouseLeave += (_, _) => close.ForeColor = CTxt3;
        close.Click += (_, _) => Close();
        bar.Controls.Add(close);

        // Bottom separator
        s.Controls.Add(new Panel { Bounds = new Rectangle(0, TH, FW-2, 1), BackColor = CBorder });
    }

    // ── Content ───────────────────────────────────────────────────────────────

    private void BuildContent(Panel s)
    {
        int cTop = TH + 1 + 18;   // content starts here (= 71)

        BuildLeft(s, cTop);
        BuildRight(s, cTop);

        // Vertical divider between columns
        int divH = FH - 2 - TH - 1 - BH - 1;
        s.Controls.Add(new Panel
        {
            Bounds    = new Rectangle(LX + CW + CGap/2, cTop, 1, divH),
            BackColor = CBorder,
        });
    }

    private void BuildLeft(Panel s, int y0)
    {
        int y = y0;

        AddSection(s, Loc.T("connection", _lang), LX, ref y);

        AddFieldLabel(s, Loc.T("url", _lang), LX, ref y);
        _urlTb = AddInput(s, LX, y, CW); y += InH + 14;
        _urlTb.Text = _cfg.PiholeUrl;
        AddHint(s, Loc.T("url_hint", _lang), LX, ref y);

        AddFieldLabel(s, Loc.T("password", _lang), LX, ref y);
        _pwTb = AddInput(s, LX, y, CW, password: true); y += InH + 14;
        _pwTb.Text = _cfg.ApiKey;
        AddHint(s, Loc.T("pw_hint", _lang), LX, ref y);

        AddFieldLabel(s, Loc.T("version", _lang), LX, ref y);
        _rbV6 = AddRadio(s, Loc.T("v6_label", _lang), LX,       y, _cfg.ApiVersion == 6);
        _rbV5 = AddRadio(s, Loc.T("v5_label", _lang), LX + 128, y, _cfg.ApiVersion == 5);
    }

    private void BuildRight(Panel s, int y0)
    {
        int y = y0;

        AddSection(s, Loc.T("options", _lang), RX, ref y);

        AddFieldLabel(s, Loc.T("poll_interval", _lang), RX, ref y);
        _pollNud = new NumericUpDown
        {
            Bounds      = new Rectangle(RX, y, 72, InH),
            Minimum     = 3, Maximum = 120,
            Value       = Math.Clamp(_cfg.PollInterval, 3, 120),
            BackColor   = CInputBg,
            ForeColor   = CTxt,
            Font        = _fInput,
            BorderStyle = BorderStyle.FixedSingle,
        };
        s.Controls.Add(_pollNud);
        s.Controls.Add(new Label { Text = Loc.T("seconds", _lang), Font = _fLabel, ForeColor = CTxt3,
                                   AutoSize = true, Location = new Point(RX+78, y+10),
                                   BackColor = Color.Transparent });
        y += InH + 20;

        _autoChk = new CheckBox
        {
            Text      = Loc.T("autostart", _lang),
            Checked   = _cfg.Autostart,
            AutoSize  = true,
            Font      = _fLabel,
            ForeColor = CTxt,
            Location  = new Point(RX, y),
            BackColor = Color.Transparent,
        };
        s.Controls.Add(_autoChk);
        y += 26 + 24;

        AddFieldLabel(s, Loc.T("language", _lang), RX, ref y);
        _langCombo = new ComboBox
        {
            Bounds        = new Rectangle(RX, y, CW, InH),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor     = CInputBg,
            ForeColor     = CTxt,
            Font          = _fInput,
            FlatStyle     = FlatStyle.Flat,
        };
        foreach (var name in Loc.Langs.Values)
            _langCombo.Items.Add(name);
        if (Loc.Langs.TryGetValue(Loc.GetEffectiveLang(_cfg.Language), out var cur))
            _langCombo.SelectedItem = cur;
        s.Controls.Add(_langCombo);
        y += InH + 24;

        AddFieldLabel(s, Loc.T("status", _lang), RX, ref y);
        _statusLbl = new Label
        {
            Text      = Loc.T("ready", _lang),
            Font      = _fLabel,
            ForeColor = CTxt3,
            AutoSize  = false,
            Bounds    = new Rectangle(RX, y, CW, 60),
            BackColor = Color.Transparent,
        };
        s.Controls.Add(_statusLbl);
    }

    // ── Button bar ────────────────────────────────────────────────────────────

    private void BuildButtonBar(Panel s)
    {
        int barY = FH - 2 - BH;
        s.Controls.Add(new Panel { Bounds = new Rectangle(0, barY, FW-2, 1), BackColor = CBorder });

        int btnY = barY + (BH - 34) / 2;
        int r    = FW - 2 - Pad;

        var save   = MkBtn(Loc.T("save",            _lang), CAccent, Color.White);
        var cancel = MkBtn(Loc.T("cancel",          _lang), CBtnBg,  CTxt);
        var test   = MkBtn(Loc.T("test_connection", _lang), CBtnBg,  CTxt);

        save.FlatAppearance.MouseOverBackColor   = CAccentH;
        cancel.FlatAppearance.MouseOverBackColor = CBtnH;
        test.FlatAppearance.MouseOverBackColor   = CBtnH;

        int sw = BtnWidth(save);
        int cw = BtnWidth(cancel);
        int tw = BtnWidth(test);

        save.Bounds   = new Rectangle(r - sw,          btnY, sw, 34);
        cancel.Bounds = new Rectangle(r - sw - 8 - cw, btnY, cw, 34);
        test.Bounds   = new Rectangle(Pad,             btnY, tw, 34);

        save.Click   += (_, _) => DoSave();
        cancel.Click += (_, _) => Close();
        test.Click   += async (_, _) => await DoTestAsync();

        s.Controls.Add(save);
        s.Controls.Add(cancel);
        s.Controls.Add(test);
    }

    // ── Control factories ─────────────────────────────────────────────────────

    // Input: returns the inner TextBox directly — no property-hiding, no custom control bugs
    private TextBox AddInput(Panel parent, int x, int y, int w, bool password = false)
    {
        var wrapper = new Panel { Bounds = new Rectangle(x, y, w, InH), BackColor = CInputBg };
        var tb = new TextBox
        {
            BorderStyle  = BorderStyle.None,
            BackColor    = CInputBg,
            ForeColor    = CTxt,
            Font         = _fInput,
            PasswordChar = password ? '●' : '\0',
        };

        void LayOut() => tb.Bounds = new Rectangle(7, (wrapper.Height - tb.PreferredHeight) / 2,
                                                    Math.Max(1, wrapper.Width - 14), tb.PreferredHeight);
        wrapper.Layout += (_, _) => LayOut();
        LayOut();

        wrapper.Paint += (_, e) =>
        {
            bool f = tb.Focused;
            using var pen = new Pen(f ? CAccent : CInputBd, f ? 2f : 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, wrapper.Width-1, wrapper.Height-1);
        };
        tb.GotFocus  += (_, _) => wrapper.Invalidate();
        tb.LostFocus += (_, _) => wrapper.Invalidate();

        wrapper.Controls.Add(tb);
        parent.Controls.Add(wrapper);
        return tb;
    }

    private void AddSection(Panel p, string text, int x, ref int y)
    {
        p.Controls.Add(new Label { Text = text.ToUpperInvariant(), Font = _fSection,
                                   ForeColor = CAccent, AutoSize = true,
                                   Location = new Point(x, y), BackColor = Color.Transparent });
        y += 18 + 16;   // text height + gap below section header
    }

    private void AddFieldLabel(Panel p, string text, int x, ref int y)
    {
        p.Controls.Add(new Label { Text = text, Font = _fLabel, ForeColor = CTxt2,
                                   AutoSize = true, Location = new Point(x, y),
                                   BackColor = Color.Transparent });
        y += 16 + 14;   // text height + gap to input below
    }

    private void AddHint(Panel p, string text, int x, ref int y)
    {
        p.Controls.Add(new Label { Text = text, Font = _fHint, ForeColor = CTxt3,
                                   AutoSize = true, Location = new Point(x, y),
                                   BackColor = Color.Transparent });
        y += 14 + 20;   // hint text height + gap to next field
    }

    private RadioButton AddRadio(Panel p, string text, int x, int y, bool chk) =>
        Add(p, new RadioButton { Text = text, Checked = chk, AutoSize = true,
                                 Font = _fLabel, ForeColor = CTxt,
                                 Location = new Point(x, y), BackColor = Color.Transparent });

    private Button MkBtn(string text, Color bg, Color fg)
    {
        var b = new Button
        {
            Text      = text,
            BackColor = bg,
            ForeColor = fg,
            Font      = _fBtn,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = bg;
        return b;
    }

    private int BtnWidth(Button b)
    {
        using var g = CreateGraphics();
        return (int)g.MeasureString(b.Text, b.Font).Width + Pad * 2;
    }

    // Helper: add and return
    private static T Add<T>(Panel p, T ctrl) where T : Control { p.Controls.Add(ctrl); return ctrl; }

    // ── Drag support ─────────────────────────────────────────────────────────

    private void Draggable(Control c)
    {
        c.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) _drag = e.Location; };
        c.MouseMove += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            Location = new Point(Location.X + e.X - _drag.X, Location.Y + e.Y - _drag.Y);
        };
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    private async Task DoTestAsync()
    {
        var url = _urlTb.Text.Trim();
        if (string.IsNullOrEmpty(url)) { SetStatus(Loc.T("enter_url", _lang), COrange); return; }
        SetStatus(Loc.T("testing", _lang), CTxt3);

        var api = new PiHoleApi(url, _pwTb.Text.Trim(), _rbV6.Checked ? 6 : 5);
        var (ok, msg) = await api.TestAsync(_lang);
        api.Dispose();
        SetStatus($"{(ok ? "✓" : "✗")}  {msg.Replace("\n", "  •  ")}", ok ? CAccent : CRed);
    }

    private void DoSave()
    {
        // Read directly from the plain TextBox controls — no custom property involved
        var url      = _urlTb.Text.Trim();
        var apiKey   = _pwTb.Text.Trim();
        var version  = _rbV6.Checked ? 6 : 5;
        var poll     = (int)_pollNud.Value;
        var autostart = _autoChk.Checked;
        var langName = _langCombo.SelectedItem?.ToString() ?? "";
        var langCode = Loc.Langs.FirstOrDefault(kv => kv.Value == langName).Key ?? "";

        _cfg.PiholeUrl    = url;
        _cfg.ApiKey       = apiKey;
        _cfg.ApiVersion   = version;
        _cfg.PollInterval = poll;
        _cfg.Autostart    = autostart;
        _cfg.Language     = langCode;

        ConfigManager.Save(_cfg);
        ConfigManager.SetAutostart(_cfg.Autostart);
        _onSave?.Invoke(_cfg);
        Close();
    }

    private void SetStatus(string text, Color color)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(text, color)); return; }
        _statusLbl.Text      = text;
        _statusLbl.ForeColor = color;
    }

    // ── Positioning ───────────────────────────────────────────────────────────

    private void PositionNearTaskbar()
    {
        var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(
            Math.Max(wa.Left + 8, wa.Right  - Width  - 12),
            Math.Max(wa.Top  + 8, wa.Bottom - Height - 12));
    }

    // ── Border ───────────────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var shadow = new Pen(Color.FromArgb(55, 0, 0, 0));
        e.Graphics.DrawRectangle(shadow, 0, 0, Width-1, Height-1);
        using var border = new Pen(CBorder);
        e.Graphics.DrawRectangle(border, 1, 1, Width-3, Height-3);
    }

    // ── Dispose fonts ────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fTitle.Dispose(); _fSection.Dispose(); _fLabel.Dispose();
            _fHint.Dispose();  _fInput.Dispose();   _fBtn.Dispose();
        }
        base.Dispose(disposing);
    }
}
