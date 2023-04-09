using System.Xml.Linq;

namespace OverdriveDownloader.OdmFile
{
	public record Protocol
	{
		public static Protocol? Parse(XElement? xElement)
			=> xElement is null || xElement.Name != nameof(Protocol) ? null
			: new Protocol
			{
				Method = xElement.Attribute("method")?.Value,
				BaseUrl = xElement.Attribute("baseurl")?.Value,
			};

		public string? Method { get; private init; }
		public string? BaseUrl { get; private init; }
	}
}
