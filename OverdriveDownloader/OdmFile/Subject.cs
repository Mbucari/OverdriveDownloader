using System.Xml.Linq;

namespace OverdriveDownloader.OdmFile
{
	public record Subject
	{
		public static Subject? Parse(XElement xElement)
			=> xElement is null || xElement.Name != nameof(Subject) ? null
			: new Subject
			{
				ID = int.TryParse(xElement.Attribute("id")?.Value, out var id) ? id : 0,
				Name = xElement.Value
			};
		public int ID { get; private init; }
		public string? Name { get; private init; }
	}
}
