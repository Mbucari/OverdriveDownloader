using System.Xml.Linq;

namespace OverdriveDownloader.OdmFile
{
	public record Format
	{
		public static Format? Parse(XElement? xElement)
		{
			if (xElement is null || xElement.Name != nameof(Format)) return null;
			var protocol = xElement.Element("Protocols")?.Elements()?.Select(Protocol.Parse).FirstOrDefault(p => p?.Method == "download");

			if (protocol?.BaseUrl is null) return null;

			return new Format(xElement, protocol.BaseUrl);
		}

		private Format(XElement xElement, string baseUrl)
		{
			BaseUrl = baseUrl;
			Quality = xElement.Element("Quality")?.Attribute("level")?.Value;
			Parts = xElement.Element("Parts")?.Elements()?.Select(p => Part.Parse(p, baseUrl)).OfType<Part>().ToArray() ?? Array.Empty<Part>();
		}

		public string BaseUrl { get; }
		public string? Quality { get; }
		public Part[] Parts { get; }
	}
}
