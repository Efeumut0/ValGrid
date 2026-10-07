using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ValGrid.Controls;

public partial class InventoryEntryControl : UserControl
{
    public static readonly DependencyProperty TooltipNameProperty = DependencyProperty.Register(
        nameof(TooltipName),
        typeof(string),
        typeof(InventoryEntryControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
        nameof(Image),
        typeof(Uri),
        typeof(InventoryEntryControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty IsRareProperty = DependencyProperty.Register(
        nameof(IsRare),
        typeof(bool),
        typeof(InventoryEntryControl),
        new PropertyMetadata(false, OnIsRareChanged)
    );

    public static readonly DependencyProperty StarVisibilityProperty = DependencyProperty.Register(
        nameof(StarVisibility),
        typeof(Visibility),
        typeof(InventoryEntryControl),
        new PropertyMetadata(Visibility.Collapsed)
    );

    public static readonly DependencyProperty GlowColorProperty = DependencyProperty.Register(
        nameof(GlowColor),
        typeof(Color),
        typeof(InventoryEntryControl),
        new PropertyMetadata(Colors.Black)
    );

    public static readonly DependencyProperty GlowBlurProperty = DependencyProperty.Register(
        nameof(GlowBlur),
        typeof(double),
        typeof(InventoryEntryControl),
        new PropertyMetadata(5.0)
    );

    public static readonly DependencyProperty GlowOpacityProperty = DependencyProperty.Register(
        nameof(GlowOpacity),
        typeof(double),
        typeof(InventoryEntryControl),
        new PropertyMetadata(0.4)
    );

    public static readonly DependencyProperty BuddyImageProperty = DependencyProperty.Register(
        nameof(BuddyImage),
        typeof(Uri),
        typeof(InventoryEntryControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty BuddyNameProperty = DependencyProperty.Register(
        nameof(BuddyName),
        typeof(string),
        typeof(InventoryEntryControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty BuddyVisibilityProperty = DependencyProperty.Register(
        nameof(BuddyVisibility),
        typeof(Visibility),
        typeof(InventoryEntryControl),
        new PropertyMetadata(Visibility.Collapsed)
    );

    public InventoryEntryControl()
    {
        InitializeComponent();
        BorderBrush = Brushes.Transparent;
        BorderThickness = new Thickness(0);
    }

    public Uri Image
    {
        get => (Uri)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public string TooltipName
    {
        get => (string)GetValue(TooltipNameProperty);
        set => SetValue(TooltipNameProperty, value);
    }

    public bool IsRare
    {
        get => (bool)GetValue(IsRareProperty);
        set => SetValue(IsRareProperty, value);
    }

    public Visibility StarVisibility
    {
        get => (Visibility)GetValue(StarVisibilityProperty);
        set => SetValue(StarVisibilityProperty, value);
    }

    public Color GlowColor
    {
        get => (Color)GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    public double GlowBlur
    {
        get => (double)GetValue(GlowBlurProperty);
        set => SetValue(GlowBlurProperty, value);
    }

    public double GlowOpacity
    {
        get => (double)GetValue(GlowOpacityProperty);
        set => SetValue(GlowOpacityProperty, value);
    }

    public Uri BuddyImage
    {
        get => (Uri)GetValue(BuddyImageProperty);
        set => SetValue(BuddyImageProperty, value);
    }

    public string BuddyName
    {
        get => (string)GetValue(BuddyNameProperty);
        set => SetValue(BuddyNameProperty, value);
    }

    public Visibility BuddyVisibility
    {
        get => (Visibility)GetValue(BuddyVisibilityProperty);
        set => SetValue(BuddyVisibilityProperty, value);
    }

    private static void OnIsRareChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InventoryEntryControl control && e.NewValue is bool isRare)
        {
            control.UpdateRareState(isRare);
        }
    }

    private void UpdateRareState(bool isRare)
    {
        if (isRare)
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xff, 0xd7, 0x00));
            BorderThickness = new Thickness(1.8);
            StarVisibility = Visibility.Visible;
            GlowColor = Color.FromRgb(0xff, 0xd7, 0x00);
            GlowBlur = 10.0;
            GlowOpacity = 0.65;
        }
        else
        {
            BorderBrush = Brushes.Transparent;
            BorderThickness = new Thickness(0);
            StarVisibility = Visibility.Collapsed;
            GlowColor = Colors.Black;
            GlowBlur = 5.0;
            GlowOpacity = 0.0;
        }
    }
}

