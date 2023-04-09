using System.Xml.Linq;

namespace OverdriveDownloader.OdmFile
{
	public class Marker
	{
		public static Marker? Parse(XElement? xElement)
			=> xElement is null || xElement.Name != nameof(Marker) ? null
			: new Marker(xElement);
		private Marker(XElement markerElement)
		{
			Name = markerElement.Element(nameof(Name))?.Value;
			Time = Util.ParseTimeSpan(markerElement.Element(nameof(Time))?.Value);
		}
		public string? Name { get; }
		public TimeSpan Time { get; }
	}
}
