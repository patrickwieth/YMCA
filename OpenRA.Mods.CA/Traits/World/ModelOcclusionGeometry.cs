using System;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	// Rasterize the renderer's textured slice mesh, never its bounding box. Empty
	// voxels and holes remain transparent. No per-frame GPU readbacks or projected shadows.
	static class ModelOcclusionGeometry
	{
		sealed class Cache
		{
			public (ModelRenderData Data, float[] Transform)[] Meshes;
			public float3 Origin;
			public float2[][] Projected;
			public int AlphaHash;
			public Rectangle Bounds;
			public bool[] Mask;
		}
		static readonly ConditionalWeakTable<Actor, Cache> Caches = new();
		static readonly ConditionalWeakTable<Sheet, byte[]> PixelsBySheet = new();

		public static bool[] Pixels(WorldRenderer wr, Actor actor, ModelRenderable model, Rectangle bounds)
		{
			if (model.Alpha < .94f || model.Palette == null) return new bool[bounds.Width * bounds.Height];
			var origin = wr.Screen3DPosition(model.Pos);
			var meshes = model.OcclusionMeshes(wr).ToArray();
			var cache = Caches.GetOrCreateValue(actor);
			var alphaHash = model.Alpha.GetHashCode();
			for (var i = 0; i < 256; i++) alphaHash = unchecked(alphaHash * 31 + (int)(model.Palette.Palette[i] >> 24));
			if (cache.Meshes == null || cache.Origin != origin || cache.AlphaHash != alphaHash || cache.Meshes.Length != meshes.Length ||
				meshes.Where((m, i) => m.Data.Vertices != cache.Meshes[i].Data.Vertices || !m.Transform.SequenceEqual(cache.Meshes[i].Transform)).Any())
			{
				cache.Meshes = meshes; cache.Origin = origin; cache.AlphaHash = alphaHash; cache.Mask = null;
				cache.Projected = meshes.Select(mesh =>
				{
					var m = mesh.Transform;
					return mesh.Data.Vertices?.Select(v => new float2(
						origin.X + (m[0] * v.X + m[4] * v.Y + m[8] * v.Z + m[12]),
						origin.Y + (m[1] * v.X + m[5] * v.Y + m[9] * v.Z + m[13]))).ToArray();
				}).ToArray();
				var points = cache.Projected.Where(p => p != null).SelectMany(p => p).ToArray();
				cache.Bounds = points.Length == 0 ? Rectangle.Empty : Rectangle.FromLTRB(
					(int)Math.Floor(points.Min(p => p.X)), (int)Math.Floor(points.Min(p => p.Y)),
					(int)Math.Ceiling(points.Max(p => p.X)), (int)Math.Ceiling(points.Max(p => p.Y)));
				if (cache.Bounds.Width > 2048 || cache.Bounds.Height > 2048) cache.Bounds = Rectangle.Empty;
			}
			// One rasterization per pose, shared by all units behind the building.
			cache.Mask ??= Rasterize(model, cache);
			var result = new bool[bounds.Width * bounds.Height];
			var overlap = Rectangle.Intersect(bounds, cache.Bounds);
			for (var y = overlap.Top; y < overlap.Bottom && overlap.Width > 0; y++)
				Array.Copy(cache.Mask, (y - cache.Bounds.Top) * cache.Bounds.Width + overlap.Left - cache.Bounds.Left,
					result, (y - bounds.Top) * bounds.Width + overlap.Left - bounds.Left, overlap.Width);
			return result;
		}

		static bool[] Rasterize(ModelRenderable model, Cache cache)
		{
			var bounds = cache.Bounds;
			var mask = new bool[bounds.Width * bounds.Height];
			if (bounds.IsEmpty) return mask;
			var meshes = cache.Meshes;
			for (var meshIndex = 0; meshIndex < meshes.Length; meshIndex++)
			{
				var mesh = meshes[meshIndex];
				var vertices = mesh.Data.Vertices;
				if (vertices == null) continue;
				var bytes = PixelsBySheet.GetValue(mesh.Data.Sheet, sheet => sheet.GetData());
				var size = mesh.Data.Sheet.Size;
				var projected = cache.Projected[meshIndex];
				for (var i = 0; i + 2 < vertices.Length; i += 3)
				{
					var a = projected[i]; var b = projected[i + 1]; var c = projected[i + 2];
					var determinant = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
					if (Math.Abs(determinant) < .001f) continue;
					var left = Math.Max(bounds.Left, (int)Math.Floor(Math.Min(a.X, Math.Min(b.X, c.X))));
					var right = Math.Min(bounds.Right, (int)Math.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))));
					var top = Math.Max(bounds.Top, (int)Math.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y))));
					var bottom = Math.Min(bounds.Bottom, (int)Math.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y))));
					var va = vertices[i]; var vb = vertices[i + 1]; var vc = vertices[i + 2];
					var channel = (int)Math.Round((.75f - va.P) * 2);
					channel = channel == 0 ? 2 : channel == 2 ? 0 : channel;
					for (var y = top; y < bottom; y++)
						for (var x = left; x < right; x++)
						{
							var index = (y - bounds.Top) * bounds.Width + x - bounds.Left;
							if (mask[index]) continue;
							var dx = x + .5f - a.X; var dy = y + .5f - a.Y;
							var u = (dx * (c.Y - a.Y) - dy * (c.X - a.X)) / determinant;
							var v = ((b.X - a.X) * dy - (b.Y - a.Y) * dx) / determinant;
							if (u < 0 || v < 0 || u + v > 1) continue;
							var s = va.S + u * (vb.S - va.S) + v * (vc.S - va.S);
							var t = va.T + u * (vb.T - va.T) + v * (vc.T - va.T);
							var sx = Math.Clamp((int)(s * size.Width), 0, size.Width - 1);
							var sy = Math.Clamp((int)(t * size.Height), 0, size.Height - 1);
							var color = bytes[(sy * size.Width + sx) * 4 + channel];
							if ((model.Palette.Palette[color] >> 24) * model.Alpha >= 240) mask[index] = true;
						}
				}
			}
			return mask;
		}
	}
}
