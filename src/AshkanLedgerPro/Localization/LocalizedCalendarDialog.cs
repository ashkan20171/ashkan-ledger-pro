using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Calendar = System.Globalization.Calendar;
using System.Windows.Media;
using AshkanLedgerPro.Localization;

namespace AshkanLedgerPro.Localization;

/// <summary>Month navigator with genuine Persian calendar day cells; returns a Gregorian DateTime.</summary>
public sealed class LocalizedCalendarDialog : Window
{
    private readonly bool _persian;
    private readonly Calendar _calendar;
    private readonly TextBlock _monthTitle = new() { FontSize = 18, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly UniformGrid _days = new() { Columns = 7 };
    private int _year;
    private int _month;
    public DateTime SelectedDate { get; private set; }

    public LocalizedCalendarDialog(DateTime initial, bool persian)
    {
        _persian = persian;
        SelectedDate = initial;
        _calendar = persian ? new PersianCalendar() : new GregorianCalendar();
        _year = _calendar.GetYear(initial);
        _month = _calendar.GetMonth(initial);
        Title = persian ? "انتخاب تاریخ" : "Select date";
        Width = 390; Height = 475;
        MinWidth = 350; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(17, 31, 52));
        Foreground = Brushes.White;
        FlowDirection = persian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var layout = new DockPanel { Margin = new Thickness(16) };
        var header = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
        var prev = MakeButton("‹"); prev.ToolTip = persian ? "ماه قبل" : "Previous month"; prev.Click += (_, _) => Shift(-1); Grid.SetColumn(prev, 0); header.Children.Add(prev);
        Grid.SetColumn(_monthTitle, 1); header.Children.Add(_monthTitle);
        var next = MakeButton("›"); next.ToolTip = persian ? "ماه بعد" : "Next month"; next.Click += (_, _) => Shift(1); Grid.SetColumn(next, 2); header.Children.Add(next);
        DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
        var footer = MakeButton(persian ? "امروز" : "Today");
        footer.Margin = new Thickness(0, 14, 0, 0);
        footer.MinHeight = 36;
        footer.Click += (_, _) => { SelectedDate = DateTime.Today; DialogResult = true; };
        DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
        layout.Children.Add(_days);
        Content = layout;
        DrawMonth();
    }

    private static Button MakeButton(string caption) => new()
    {
        Content = caption, Padding = new Thickness(6), Margin = new Thickness(2),
        Background = new SolidColorBrush(Color.FromRgb(34, 61, 85)), Foreground = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(55, 95, 116)), Cursor = System.Windows.Input.Cursors.Hand
    };

    private void Shift(int offset)
    {
        var first = _calendar.ToDateTime(_year, _month, 1, 0, 0, 0, 0);
        DateTime shifted;
        try { shifted = _calendar.AddMonths(first, offset); }
        catch (ArgumentOutOfRangeException) { return; }
        _year = _calendar.GetYear(shifted);
        _month = _calendar.GetMonth(shifted);
        DrawMonth();
    }

    private void DrawMonth()
    {
        _days.Children.Clear();
        var first = _calendar.ToDateTime(_year, _month, 1, 0, 0, 0, 0);
        var monthName = _persian
            ? new[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" }[_month - 1]
            : CultureInfo.GetCultureInfo("en-US").DateTimeFormat.GetMonthName(_month);
        _monthTitle.Text = $"{monthName} {(_persian ? ToPersian(_year.ToString(CultureInfo.InvariantCulture)) : _year.ToString(CultureInfo.InvariantCulture))}";
        string[] headings = _persian
            ? new[] { "ش", "ی", "د", "س", "چ", "پ", "ج" }
            : new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
        foreach (var heading in headings)
            _days.Children.Add(new TextBlock { Text = heading, Foreground = Brushes.LightSkyBlue, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 6, 0, 12) });
        int offset = _persian ? ((int)first.DayOfWeek + 1) % 7 : (int)first.DayOfWeek;
        for (int i = 0; i < offset; i++) _days.Children.Add(new TextBlock());
        int length = _calendar.GetDaysInMonth(_year, _month);
        for (int day = 1; day <= length; day++)
        {
            var date = _calendar.ToDateTime(_year, _month, day, 0, 0, 0, 0);
            var button = MakeButton(_persian ? ToPersian(day.ToString(CultureInfo.InvariantCulture)) : day.ToString(CultureInfo.InvariantCulture));
            button.Height = 38;
            button.ToolTip = _persian ? $"{ToPersian(_year.ToString())}/{ToPersian(_month.ToString("00"))}/{ToPersian(day.ToString("00"))}" : date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (date.Date == SelectedDate.Date) button.Background = new SolidColorBrush(Color.FromRgb(0, 133, 142));
            button.Click += (_, _) => { SelectedDate = date; DialogResult = true; };
            _days.Children.Add(button);
        }
    }

    private static string ToPersian(string digits)
    {
        const string fa = "۰۱۲۳۴۵۶۷۸۹";
        return string.Concat(digits.Select(c => c >= '0' && c <= '9' ? fa[c - '0'] : c));
    }
}
