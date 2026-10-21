using System.Drawing;
using System.Numerics;
using Glasspane;

namespace GlassDemo;

/// <summary>A small floating control card: a status dot, a few icon buttons, and a settings area that opens below the bar.</summary>
sealed class DemoPanel : UiGlassPanel
{
    public readonly UiLabel Title = new("Glasspane", 12) { Color = UiTheme.Text };
    public readonly UiButton Mouse = new() { Label = "Input", Icon = "mouse", IsToggle = true };
    public readonly UiButton Clipboard = new() { Label = "Clipboard", Icon = "clipboard", IsToggle = true, IsChecked = true };
    public readonly UiButton FullScreen = new() { Icon = "expand", ToolTip = "Full screen" };
    public readonly UiButton More = new() { Icon = "sliders", IsToggle = true, ToolTip = "Settings" };
    public readonly UiButton Quit = new() { Icon = "power", IsDanger = true, ToolTip = "Close" };
    public readonly UiButton Collapse = new() { Icon = "chevrons", ToolTip = "Collapse to an orb (also after 10 s idle)" };

    public readonly UiLabel ModeTitle = new("Mode", 11) { Color = UiTheme.Subtle };
    public readonly UiLabel SpeedLabel = new("Speed · 1.0×", 11) { Color = UiTheme.Subtle };
    public readonly UiChoice Mode = new();
    public readonly UiSlider Speed = new() { Minimum = .2, Maximum = 3, Step = .1 };
    public readonly UiLabel Hint = new("Drag the card anywhere. Click the orb to expand it.", 11) { Color = UiTheme.Subtle };

    float dotX, dotY;
    readonly UiElement[] elements;
    readonly UiElement[] moreElements;
    public bool MoreOpen => More.IsChecked == true;

    public DemoPanel(object gate) : base(gate)
    {
        moreElements = [ModeTitle, SpeedLabel, Mode, Speed, Hint];
        elements = [Title, Mouse, Clipboard, FullScreen, More, Quit, Collapse, .. moreElements];
        Mode.Items.Add(new UiChoiceItem("Aurora", "aurora", true, null));
        Mode.Items.Add(new UiChoiceItem("Stripes", "stripes", true, null));
        Mode.Items.Add(new UiChoiceItem("Checker", "checker", true, null));
        Mode.Items.Add(new UiChoiceItem("Unavailable", "off", false, "Disabled items are listed but cannot be picked"));
        Mode.SelectedIndex = 0;
        Speed.Value = 1;
        StatusColor = UiTheme.Good;
    }

    public void Wire(Action invalidate)
    {
        Invalidate = invalidate;
        foreach (var element in elements) element.Invalidate = invalidate;
    }

    public IEnumerable<UiElement> AllElements => elements;
    protected override IEnumerable<UiElement> Elements => elements;

    protected override float MeasureContent(UiText text, float width)
    {
        var height = 28f;
        if (!MoreOpen) return height;
        height += 11 + 16 + 4 + 30;
        if (Mode.IsOpen) height += 4 + Mode.ListHeight;
        height += 8 + 18;
        return height;
    }

    protected override void LayoutContent(UiText text, RectangleF content)
    {
        var x = content.X; var y = content.Y; var width = content.Width;
        var buttons = new[] { Mouse, Clipboard, FullScreen, More, Quit, Collapse };
        var sizes = buttons.Select(button => button.Measure(text, 0)).ToArray();
        var cursor = x + width - (sizes.Sum(size => size.X) + 2 * (buttons.Length - 1));
        for (var i = 0; i < buttons.Length; i++) { buttons[i].Bounds = new RectangleF(cursor, y, sizes[i].X, 28); cursor += sizes[i].X + 2; }
        dotX = x + 12; dotY = y + 14;
        var title = Title.Measure(text, 0);
        Title.Bounds = new RectangleF(x + 23, y + 14 - title.Y / 2, title.X, title.Y);

        foreach (var element in moreElements) element.IsVisible = MoreOpen;
        if (!MoreOpen) return;
        y += 28 + 11;
        var left = x + 6; var inner = width - 12; var column = (inner - 12) / 2;
        ModeTitle.Bounds = new RectangleF(left, y, column, 16);
        SpeedLabel.Bounds = new RectangleF(left + column + 12, y, column, 16);
        y += 16 + 4;
        Mode.Bounds = new RectangleF(left, y, column, 30);
        Speed.Bounds = new RectangleF(left + column + 12, y + 4, column, 22);
        y += 30;
        if (Mode.IsOpen) y += 4 + Mode.ListHeight;
        y += 8;
        Hint.Bounds = new RectangleF(left, y, inner, 18);
    }

    protected override void DrawContent(UiPaint paint, float opacity)
    {
        paint.FillCircle(new Vector2(dotX, dotY), 4, UiPaint.WithAlpha(StatusColor, opacity));
        foreach (var element in elements)
        {
            if (!element.IsVisible || element == Mode) continue;
            element.Draw(paint, opacity);
        }
        if (MoreOpen)
        {
            paint.FillRounded(new RectangleF(CurrentRect.X + 11, CurrentRect.Y + 38, CurrentRect.Width - 22, 1), 0, UiPaint.WithAlpha(UiTheme.Line, opacity));
            Mode.Draw(paint, opacity);
            Mode.DrawList(paint, opacity);
        }
    }
}
