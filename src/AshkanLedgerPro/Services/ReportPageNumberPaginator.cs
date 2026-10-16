using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AshkanLedgerPro.Services;

/// <summary>Decorates a WPF report with a non-interactive page footer for printed output.</summary>
public sealed class ReportPageNumberPaginator : DocumentPaginator
{
    private readonly DocumentPaginator inner;
    private readonly bool persian;

    public ReportPageNumberPaginator(DocumentPaginator inner, bool persian)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.persian = persian;
    }

    public override bool IsPageCountValid => inner.IsPageCountValid;
    public override int PageCount => inner.PageCount;
    public override Size PageSize { get => inner.PageSize; set => inner.PageSize = value; }
    public override IDocumentPaginatorSource Source => inner.Source;

    public override DocumentPage GetPage(int pageNumber)
    {
        var page = inner.GetPage(pageNumber);
        if (page == DocumentPage.Missing || page.Visual is null) return page;

        var container = new ContainerVisual();
        container.Children.Add(page.Visual);
        var footer = new DrawingVisual();
        using (var drawing = footer.RenderOpen())
        {
            var width = page.Size.Width;
            var height = page.Size.Height;
            var linePen = new Pen(Brushes.LightSteelBlue, 0.7);
            drawing.DrawLine(linePen, new Point(46, height - 40), new Point(Math.Max(46, width - 46), height - 40));
            var text = persian
                ? $"اشکان لجر پرو  |  صفحه {pageNumber + 1} از {PageCount}"
                : $"ASHKAN LEDGER PRO  |  Page {pageNumber + 1} of {PageCount}";
            var formatted = new FormattedText(text, persian ? CultureInfo.GetCultureInfo("fa-IR") : CultureInfo.InvariantCulture,
                persian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 9, Brushes.SlateGray, 1.0);
            drawing.DrawText(formatted, new Point(Math.Max(46, (width - formatted.Width) / 2), height - 33));
        }
        container.Children.Add(footer);
        return new DocumentPage(container, page.Size, page.BleedBox, page.ContentBox);
    }
}
