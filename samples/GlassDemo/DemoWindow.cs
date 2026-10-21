using System.Numerics;
using Glasspane;
using static Glasspane.UiNative;

namespace GlassDemo;

/// <summary>
/// The whole integration in one place: a window, a <see cref="UiVideoView"/> that draws frames (here a synthetic animation),
/// and a glass panel painted over it in the same Direct2D pass with the live frame blurred behind it.
/// </summary>
sealed class DemoWindow : UiWindow
{
    const int FrameWidth = 1100, FrameHeight = 700;
    readonly object sceneGate = new();
    readonly UiDevices measureDevices = new();
    readonly UiText measureText;
    readonly DemoPanel panel;
    readonly UiVideoView preview;
    readonly UiTimer frames = new(33), idle = new(500), animation = new(16);
    readonly byte[] pixels = new byte[FrameWidth * FrameHeight * 4];
    long lastActivity = Environment.TickCount64, pts;
    double phase;
    bool trackingMouse;
    string mode = "aurora";
    double speed = 1;

    public DemoWindow()
    {
        IsMainWindow = true; MinWidth = 640; MinHeight = 420;
        CreateNative("Glasspane demo", WS_OVERLAPPEDWINDOW | WS_CLIPCHILDREN, 0, unchecked((int)0x80000000), unchecked((int)0x80000000), 1100, 700);
        SetClientSize((int)(1100 * Scale), (int)(700 * Scale));
        CenterOnScreen();

        measureText = new UiText(measureDevices.DWrite);
        panel = new DemoPanel(sceneGate);
        panel.Wire(() => preview?.Invalidate());
        panel.Backdrop = (paint, rect, radius, _) => preview.DrawGlass(paint, rect, radius);
        panel.InteractionStarted = () => lastActivity = Environment.TickCount64;
        panel.PillWidth = 96; panel.UsePill = true; panel.PillText = "Glasspane";
        panel.More.CheckedChanged += () => { lock (sceneGate) panel.Relayout(measureText); preview.Invalidate(); };
        panel.Mode.OpenChanged += () => { lock (sceneGate) panel.Relayout(measureText); };
        panel.Mode.SelectionChanged += () => mode = panel.Mode.SelectedTag ?? "aurora";
        panel.Speed.ValueChanged += () => { speed = panel.Speed.Value; panel.SpeedLabel.Text = $"Speed · {speed:0.0}×"; };
        panel.FullScreen.Click += () => { ToggleFullScreen(); panel.FullScreen.Icon = State == UiWindowState.FullScreen ? "shrink" : "expand"; };
        panel.Quit.Click += () => Close();
        panel.Collapse.Click += () => Animate(true);

        preview = new UiVideoView(this) { DrawOverlay = DrawOverlay, NeedsContinuousRender = () => panel.Animating };
        preview.Failed += error => Console.Error.WriteLine(error);

        frames.Tick += NextFrame;
        idle.Tick += () =>
        {
            if (panel.Animating || panel.Collapsed || panel.IsPointerOver || panel.IsCapturing) return;
            if (Environment.TickCount64 - lastActivity >= 10_000) Animate(true);
        };
        animation.Tick += () => { bool running; lock (sceneGate) running = panel.Tick(measureText); if (!running) animation.Stop(); };

        if (Program.OpenSettings) panel.More.IsChecked = true;
        OnResized();
        preview.Start();
        frames.Start(); idle.Start();
        Show(SW_SHOWNORMAL);
        Show(SW_SHOWNORMAL);
    }

    void Animate(bool toCollapsed)
    {
        if (panel.Animating) return;
        lastActivity = Environment.TickCount64;
        lock (sceneGate) panel.StartAnimation(measureText, toCollapsed);
        animation.Start();
    }

    void DrawOverlay(UiPaint paint, float width, float height)
    {
        lock (sceneGate) panel.Draw(paint);
    }

    // ---- a synthetic "remote desktop" so the glass has something to blur ----

    void NextFrame()
    {
        phase += .04 * speed;
        var data = pixels;
        for (var y = 0; y < FrameHeight; y++)
        {
            for (var x = 0; x < FrameWidth; x++)
            {
                double r, g, b;
                switch (mode)
                {
                    case "stripes":
                        var stripe = ((x + (int)(phase * 80)) / 40 + y / 40) % 2 == 0;
                        (r, g, b) = stripe ? (.95, .35, .30) : (.12, .16, .30);
                        break;
                    case "checker":
                        var cell = (((x + (int)(phase * 60)) / 60) + (y / 60)) % 2 == 0;
                        (r, g, b) = cell ? (.9, .9, .92) : (.15, .17, .22);
                        break;
                    default:
                        var u = x / (double)FrameWidth; var v = y / (double)FrameHeight;
                        r = .5 + .5 * Math.Sin(u * 6 + phase);
                        g = .5 + .5 * Math.Sin(v * 5 - phase * 1.3 + u * 2);
                        b = .5 + .5 * Math.Sin((u + v) * 4 + phase * .7);
                        break;
                }
                var i = (y * FrameWidth + x) * 4;
                data[i] = (byte)(b * 255); data[i + 1] = (byte)(g * 255); data[i + 2] = (byte)(r * 255); data[i + 3] = 255;
            }
        }
        // Some "window" chrome so the blur edge is visible against detail.
        for (var y = 80; y < 200; y++) for (var x = 120; x < 520; x++) { var i = (y * FrameWidth + x) * 4; data[i] = 40; data[i + 1] = 36; data[i + 2] = 34; }
        preview.Submit(new VideoFrame((byte[])data.Clone(), FrameWidth, FrameHeight, ++pts));
    }

    // ---- window messages ----

    protected override void OnResized()
    {
        if (panel == null || measureText == null) return;
        lock (sceneGate) panel.Layout(measureText, (int)(ClientWidth / Scale), (int)(ClientHeight / Scale));
        preview?.Invalidate();
    }

    protected override void OnDpiChanged() => OnResized();

    protected override void OnDestroying()
    {
        frames.Dispose(); idle.Dispose(); animation.Dispose();
        preview.Dispose(); measureDevices.Dispose();
    }

    Vector2 ToDip(nint lParam) => new(LoWord(lParam) / Scale, HiWord(lParam) / Scale);

    protected override nint OnMessage(uint message, nuint wParam, nint lParam, out bool handled)
    {
        handled = false;
        switch (message)
        {
            case WM_MOUSEMOVE:
                if (!trackingMouse) { var track = new TRACKMOUSEEVENT { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<TRACKMOUSEEVENT>(), dwFlags = 2, hwndTrack = Handle }; TrackMouseEvent(ref track); trackingMouse = true; }
                lock (sceneGate) panel.PointerMove(ToDip(lParam));
                preview.Invalidate();
                return 0;
            case WM_MOUSELEAVE:
                trackingMouse = false;
                lock (sceneGate) panel.PointerLeave();
                preview.Invalidate();
                return 0;
            case WM_LBUTTONDOWN:
            {
                bool consumed;
                lock (sceneGate) consumed = panel.PointerDown(ToDip(lParam));
                if (consumed) { SetCapture(Handle); handled = true; }
                return 0;
            }
            case WM_LBUTTONUP:
            {
                bool orbClick, consumed;
                lock (sceneGate) orbClick = panel.PointerUp(ToDip(lParam), out consumed);
                if (consumed) { if (GetCapture() == Handle) ReleaseCapture(); handled = true; }
                if (orbClick) Animate(false);
                preview.Invalidate();
                return 0;
            }
            case WM_MOUSEWHEEL:
            {
                var screen = new POINT { X = LoWord(lParam), Y = HiWord(lParam) };
                ScreenToClient(Handle, ref screen);
                bool consumed;
                lock (sceneGate) consumed = panel.Wheel(new Vector2(screen.X / Scale, screen.Y / Scale), HiWord(wParam) / 120f);
                if (consumed) { handled = true; preview.Invalidate(); }
                return 0;
            }
        }
        return 0;
    }
}
