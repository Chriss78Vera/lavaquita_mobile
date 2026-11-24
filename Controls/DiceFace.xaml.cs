namespace LaVaquita.Controls;

public partial class DiceFace : ContentView
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(int), typeof(DiceFace), 1, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ShowEqualsProperty = BindableProperty.Create(
        nameof(ShowEquals), typeof(bool), typeof(DiceFace), false, propertyChanged: OnVisualPropertyChanged);

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool ShowEquals
    {
        get => (bool)GetValue(ShowEqualsProperty);
        set => SetValue(ShowEqualsProperty, value);
    }

    public DiceFace()
    {
        InitializeComponent();
        UpdateFace();
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DiceFace)bindable).UpdateFace();

    private void UpdateFace()
    {
        if (PipCenter is null) return;

        var pips = new[] { PipTopLeft, PipTopRight, PipMiddleLeft, PipCenter, PipMiddleRight, PipBottomLeft, PipBottomRight };
        foreach (var pip in pips) pip.IsVisible = false;

        EqualsLabel.IsVisible = ShowEquals;
        NumericValueLabel.IsVisible = !ShowEquals && Value > 6;
        NumericValueLabel.Text = Value.ToString();
        if (ShowEquals || Value is < 1 or > 6) return;

        PipTopLeft.IsVisible = Value is 2 or 3 or 4 or 5 or 6;
        PipTopRight.IsVisible = Value is 4 or 5 or 6;
        PipMiddleLeft.IsVisible = Value == 6;
        PipCenter.IsVisible = Value is 1 or 3 or 5;
        PipMiddleRight.IsVisible = Value == 6;
        PipBottomLeft.IsVisible = Value is 4 or 5 or 6;
        PipBottomRight.IsVisible = Value is 2 or 3 or 4 or 5 or 6;
    }
}
