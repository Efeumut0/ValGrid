using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using ValGrid.ViewModels;

namespace ValGrid;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MainViewModel>();
        ((App)Application.Current).WindowPlace.Register(this);

        Loaded += (s, e) =>
        {
            BringToForeground();
        };
    }

    public void BringToForeground()
    {
        try
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Show();
            Activate();
            Focus();

            Topmost = true;
            Topmost = false;

            var helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                ShowWindow(helper.Handle, SW_RESTORE);
                BringWindowToTop(helper.Handle);
                SetForegroundWindow(helper.Handle);
            }
        }
        catch { }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        try
        {
            Application.Current?.Shutdown();
        }
        catch { }
        finally
        {
            System.Environment.Exit(0);
        }
    }
}

