using System.Xml.Linq;

namespace OverdriveDownloader.OdmFile
{
	public record Part
	{
		public static Part? Parse(XElement? xElement, string baseUrl)
		{
			if (xElement is null || xElement.Name != nameof(Part)) return null;

			var filename = xElement.Attribute("filename")?.Value;

			if (filename is null) return null;

			var builder = new UriBuilder(baseUrl);
			builder.Path = Path.Combine(builder.Path, filename);

			return new Part(xElement, filename, builder.Uri);
		}

		private Part(XElement xElement, string filename, Uri fileUrl)
		{
			Number = int.TryParse(xElement.Attribute("number")?.Value, out var num) ? num : null;
			FileSize = long.TryParse(xElement.Attribute("filesize")?.Value, out var num2) ? num2 : null;
			Name = xElement.Attribute("name")?.Value;
			Filename = filename;
			FileUrl = fileUrl;
			Duration = parseDuration(xElement.Attribute("duration")?.Value);
		}

		private static TimeSpan parseDuration(string? duration)
		{
			var parts = duration?.Split(':');
			return parts?.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var sec)
				? TimeSpan.FromMinutes(min) + TimeSpan.FromSeconds(sec)
				: TimeSpan.Zero;
		}
		public int? Number { get; private init; }
		public long? FileSize { get; private init; }
		public string? Name { get; private init; }
		public string Filename { get; private init; }
		public Uri FileUrl { get; }
		public TimeSpan Duration { get; }
	}
}
