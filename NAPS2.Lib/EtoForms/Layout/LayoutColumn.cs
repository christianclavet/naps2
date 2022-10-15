using Eto.Drawing;
using Eto.Forms;

namespace NAPS2.EtoForms.Layout;

public class LayoutColumn : LayoutLine<LayoutRow>
{
    public LayoutColumn(LayoutElement[] children)
    {
        Children = children;
    }

    public LayoutColumn(LayoutColumn original, Padding? padding = null, int? spacing = null, bool? xScale = null,
        bool? yScale = null, bool? aligned = null)
    {
        Children = original.Children;
        Padding = padding ?? original.Padding;
        Spacing = spacing ?? original.Spacing;
        XScale = xScale ?? original.XScale;
        YScale = yScale ?? original.YScale;
        Aligned = aligned ?? original.Aligned;
    }

    private Padding? Padding { get; }

    public override void AddTo(DynamicLayout layout)
    {
        Size? spacing = Spacing == null ? null : new Size(Spacing.Value, Spacing.Value);
        layout.BeginVertical(padding: Padding, spacing: spacing, xscale: XScale, yscale: YScale);
        foreach (var child in Children)
        {
            child.AddTo(layout);
        }
        layout.EndVertical();
    }

    protected override PointF UpdatePosition(PointF position, float delta)
    {
        position.Y += delta;
        return position;
    }

    protected override SizeF UpdateTotalSize(SizeF size, SizeF childSize, int spacing)
    {
        size.Height += childSize.Height + spacing;
        size.Width = Math.Max(size.Width, childSize.Width);
        return size;
    }

    protected internal override bool DoesChildScale(LayoutElement child) => child.YScale;

    protected override float GetBreadth(SizeF size) => size.Width;
    protected override float GetLength(SizeF size) => size.Height;
    protected override SizeF GetSize(float length, float breadth) => new(breadth, length);
}