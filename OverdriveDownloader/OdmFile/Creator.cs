using System.Xml.Linq;

namespace OverdriveDownloader.OdmFile
{
	public enum CreatorType
	{
		None,
		Author,
		Narrator
	}
	public record Creator
	{
		public static Creator? Parse(XElement xElement)
			=> xElement is null || xElement.Name != nameof(Creator) ? null
			: new Creator
			{
				Role = Enum.TryParse<CreatorType>(xElement.Attribute("role")?.Value, out var role) ? role : CreatorType.None,
				FileAs = xElement.Attribute("file-as")?.Value is string str ? str : null,
				Name = xElement.Value
			};
		public CreatorType Role { get; private init; }
		public string? FileAs { get; private init; }
		public string? Name { get; private init; }
	}
}
