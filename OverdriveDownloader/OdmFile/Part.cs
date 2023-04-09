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
			Duration = Util.ParseTimeSpan(xElement.Attribute("duration")?.Value);
		}

		public int? Number { get; private init; }
		public long? FileSize { get; private init; }
		public string? Name { get; private init; }
		public string Filename { get; private init; }
		public Uri FileUrl { get; }
		public TimeSpan Duration { get; }
	}
}
