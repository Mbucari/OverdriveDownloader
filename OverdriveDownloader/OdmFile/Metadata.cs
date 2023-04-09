using System.Xml.Linq;

namespace OverdriveDownloader.OdmFile
{
	public record Metadata
	{
		public static Metadata? Parse(XElement? xElement)
			=> xElement is null || xElement.Name != nameof(Metadata) ? null
			: new Metadata(xElement);

		private Metadata(XElement metadataElement)
		{
			Title = metadataElement.Element(nameof(Title))?.Value;
			SubTitle = metadataElement.Element(nameof(SubTitle))?.Value;
			SortTitle = metadataElement.Element(nameof(SortTitle))?.Value;
			Publisher = metadataElement.Element(nameof(Publisher))?.Value;
			Series = metadataElement.Element(nameof(Series))?.Value;
			ThumbnailUrl = metadataElement.Element(nameof(ThumbnailUrl))?.Value;
			CoverUrl = metadataElement.Element(nameof(CoverUrl))?.Value;
			Description = metadataElement.Element(nameof(Description))?.Value;
			Subjects = metadataElement.Element(nameof(Subjects))?.Elements()?.Select(Subject.Parse).OfType<Subject>().ToArray() ?? Array.Empty<Subject>();
			Creators = metadataElement.Element(nameof(Creators))?.Elements()?.Select(Creator.Parse).OfType<Creator>().ToArray() ?? Array.Empty<Creator>();
		}

		public string? Title { get; }
		public string? SubTitle { get; }
		public string? SortTitle { get; }
		public string? Publisher { get; }
		public string? Series { get; }
		public string? ThumbnailUrl { get; }
		public string? CoverUrl { get; }
		public string? Description { get; }
		public Subject[] Subjects { get; }
		public Creator[] Creators { get; }
	}
}
