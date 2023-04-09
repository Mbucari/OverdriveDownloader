using Mpeg4Lib.Util;
using OverdriveDownloader.OdmFile;
using static OverdriveDownloader.Logging;

namespace OverdriveDownloader
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			if (args.Length != 1)
			{
				LogError("Call this program with an ODM files as a parameter");
				return;
			}

			var odmFile = args[0];

			if (!File.Exists(odmFile))
			{
				LogError($"Could not find \"{odmFile}\"");
				return;
			}

			var odm = await OverDriveMedia.LoadAsync(odmFile);

			if (odm is null)
			{
				LogError($"Couldn't open ODM file: {odmFile}");
				return;
			}

			var parts = odm.Formats.FirstOrDefault()?.Parts;

			if (parts is null || parts.Length < 1)
			{
				LogError("ODM file doesn't contain any downloadable parts");
				return;
			}

			LogInfo($"ODM file contains {parts.Length} downloadable parts");

			var odmDir = Path.GetDirectoryName(odmFile)!;

			Mp3ToMp4Writer? mp4Writer = null;

			foreach (var part in parts)
			{
				var partname = part.Name + Path.GetExtension(part.Filename);
				var partFilename = Path.Combine(odmDir, partname);

				if (!File.Exists(partFilename))
				{
					LogInfo($"Downloading {partname}");
					if (!await odm.DownloadPartAsync(part, partFilename)) return;
				}
				else
				{
					LogInfo($"Found {partFilename}");
				}

				if (mp4Writer is null)
				{
					var fileOut = File.OpenWrite(Path.ChangeExtension(odmFile, "m4b"));

					mp4Writer = Mp3ToMp4Writer.Create(partFilename, fileOut);

					if (mp4Writer is null)
					{
						LogError($"Failed to create {nameof(Mp3ToMp4Writer)}");
						return;
					}

					LogInfo($"Created {nameof(Mp3ToMp4Writer)} with file {partname}");

					var authors = odm.Metadata?.Creators?.Where(c => c.Role is CreatorType.Author)?.Select(a => a.Name)?.ToArray();
					var narrators = odm.Metadata?.Creators?.Where(c => c.Role is CreatorType.Narrator)?.Select(a => a.Name)?.ToArray();
					var genres = odm.Metadata?.Subjects?.Select(a => a.Name)?.ToArray();

					if (odm.Metadata?.Title is not null)
					{
						var title = odm.Metadata.Title;
						if (odm.Metadata?.SubTitle is not null)
							title += ": " + odm.Metadata?.SubTitle;

						LogInfo($"Title:  {title}");

						mp4Writer.Moov.ILst.AddTag("©nam", title);
						mp4Writer.Moov.ILst.AddTag("©alb", title);
					}
					if (authors?.Length > 0)
					{
						var autStr = string.Join(", ", authors);
						LogInfo($"Author(s):  {autStr}");
						mp4Writer.Moov.ILst.AddTag("©aut", autStr);
						mp4Writer.Moov.ILst.AddTag("aART", autStr);
					}
					if (narrators?.Length > 0)
					{
						var nrtStr = string.Join(", ", narrators);
						LogInfo($"Narrator(s):  {nrtStr}");
						mp4Writer.Moov.ILst.AddTag("©nrt", nrtStr);
						mp4Writer.Moov.ILst.AddTag("TCOM", nrtStr);
					}
					if (genres?.Length > 0)
					{
						var genStr = string.Join(", ", genres);
						LogInfo($"Genre(s):  {genStr}");
						mp4Writer.Moov.ILst.AddTag("©gen", genStr);
					}
					if (odm.Metadata?.Description is not null)
					{
						LogInfo($"Description:  {odm.Metadata.Description}");
						mp4Writer.Moov.ILst.AddTag("©cmt", odm.Metadata.Description);
						mp4Writer.Moov.ILst.AddTag("©des", odm.Metadata.Description);
					}
					if (odm.Metadata?.Publisher is not null)
					{
						LogInfo($"Publisher:  {odm.Metadata.Publisher}");
						mp4Writer.Moov.ILst.AddTag("©pub", odm.Metadata.Publisher);
					}
				}
				else
				{
					LogInfo($"Adding {partname}");
					mp4Writer?.AddMp3File(partFilename);
				}
			}

			if (mp4Writer?.Moov.ILst.Children.Any(t => t.Header.Type == "covr") is false
				&& odm.Metadata?.CoverUrl is not null)
			{
				LogInfo($"Downloading cover art from {odm.Metadata.CoverUrl}");
				var cover = await new HttpClient().GetByteArrayAsync(odm.Metadata.CoverUrl);
				mp4Writer?.Moov.ILst.AddTag("covr", cover, Mpeg4Lib.Boxes.AppleDataType.JPEG);
			}

			mp4Writer?.Dispose();
			mp4Writer?.OutputFile.Close();

			if (mp4Writer?.OutputFile is FileStream fs)
			{
				LogInfo($"Moving moov atom to beginning of file");
				await Mpeg4Util.RelocateMoovToBeginningAsync(fs.Name, default, (_, _, _) => { });
				LogInfo($"Complete m4b saved at {fs.Name}");
			}

			LogInfo("Done!");
		}
	}
}