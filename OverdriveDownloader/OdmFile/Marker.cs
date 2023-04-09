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
			var nmval = markerElement.Element(nameof(Time))?.Value;
			if (nmval is not null)
			{
				var parts = nmval.Split(':');
				Array.Reverse(parts);
				if (double.TryParse(parts[0], out var secs)) Time = TimeSpan.FromSeconds(secs);
				if (int.TryParse(parts[1], out var mins)) Time += TimeSpan.FromMinutes(mins);
				if (parts.Length > 2 && int.TryParse(parts[2], out var hrs)) Time += TimeSpan.FromHours(hrs);
			}
		}
		public string? Name { get; }
		public TimeSpan Time { get; }
	}
}
