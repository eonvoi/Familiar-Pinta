using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools.Brushes;
 
internal sealed class SoftBrush : BasePaintBrush
{
	public override string Name => Translations.GetString ("Soft");

	public override int Priority => -90;

	private ImageSurface? maskSurface;
	private Context? maskContext;
	private RectangleI dirty;
	private ImageSurface? currentSurface;

	protected override RectangleI OnMouseMove (
		Context g,
		ImageSurface surface,
		BrushStrokeArgs strokeArgs)
	{
		surface.Clear ();

		currentSurface = surface;
		if (maskSurface == null) {
			maskSurface = new ImageSurface (
				Format.A8,
				currentSurface.Width,
				currentSurface.Height);

			maskContext = new Context (maskSurface);
			maskContext.Operator = Operator.Add;

			dirty = RectangleI.Zero;
		}

		if (maskContext is null) {
			return RectangleI.Zero;
		}

		PointI from = strokeArgs.LastPosition;
		PointI to = strokeArgs.CurrentPosition;

		double radius = g.LineWidth / 2.0;
		double spacing = radius * 0.5;

		double dx = to.X - from.X;
		double dy = to.Y - from.Y;
		double distance = Math.Sqrt (dx * dx + dy * dy);

		// try to interpolate, spacing is half the brush size
		int steps = Math.Max (1, (int) Math.Ceiling (distance / spacing));

		// surface was cleared, draw each stamp again
		for (int i = 0; i <= steps; i++) {
			double t = i / (double) steps;
			double x = from.X + dx * t;
			double y = from.Y + dy * t;

			StampMask (maskContext, x, y, radius);

			// extend dirty to capture this stamp's rectangle
			dirty = dirty.Union (new RectangleI (
				(int) (x - radius - 1),
				(int) (y - radius - 1),
				(int) (radius * 2 + 2),
				(int) (radius * 2 + 2)));
		}

		// preview
		g.Save ();
		g.SetSourceColor (strokeArgs.StrokeColor);
		g.MaskSurface (maskSurface, 0, 0);
		g.Restore ();

		return dirty;
	}
	private static void StampMask (
		Context g,
		double x,
		double y,
		double radius)
	{
		using RadialGradient grad = new (
			x, y, 0,
			x, y, radius);

		// Cairo's radial falloff looks kind of weird when using it like one would think to
		// grad.AddColorStopRgba (0, 1, 1, 1, 1);
		// grad.AddColorStopRgba (1, 1, 1, 1, 0);

		// so instead I eyed out a nicer looking alpha falloff
		grad.AddColorStopRgba (0, 1, 1, 1, 0.4);
		grad.AddColorStopRgba (0.1, 1, 1, 1, 0.35);
		grad.AddColorStopRgba (0.25, 1, 1, 1, 0.3);
		grad.AddColorStopRgba (0.5, 1, 1, 1, 0.2);
		grad.AddColorStopRgba (0.92, 1, 1, 1, 0.015);
		grad.AddColorStopRgba (0.97, 1, 1, 1, 0.005);
		grad.AddColorStopRgba (1, 1, 1, 1, 0);

		g.Save ();
		g.SetSource (grad);
		g.Arc (x, y, radius, 0, Math.PI * 2);
		g.Fill ();
		g.Restore ();
	}

	protected override void OnMouseUp ()
	{
		maskContext?.Dispose ();
		maskSurface?.Dispose ();

		maskContext = null;
		maskSurface = null;
		currentSurface = null;
	}
}