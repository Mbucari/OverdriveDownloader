using Mpeg4Lib.Util;
using OverdriveDownloader.OdmFile;

namespace OverdriveDownloader
{
	internal class Program
	{
		public static void LogError(string message)
		{
			var eg = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[ERR]  {0}", message);
			Console.ForegroundColor = eg;
		}
		public static void LogInfo(string message, ConsoleColor color = default)
		{
			var eg = Console.ForegroundColor;
			Console.ForegroundColor = color == default ? Console.ForegroundColor : color;
			Console.WriteLine("[INF]  {0}", message);
			Console.ForegroundColor = eg;
		}

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

			var odm2 = await OverDriveMedia.LoadAsync(odmFile);

			if (odm2 is null)
			{
				LogError($"Couldn't open ODM file: {odmFile}");
				return;
			}

			var parts = odm2.Formats.FirstOrDefault()?.Parts;

			if (parts?.Length < 1)
			{
				LogError("ODM file doesn't contain any downloadable parts");
				return;
			}

			LogInfo($"ODM file contains {parts!.Length} downloadable parts");

			var dir = Path.GetDirectoryName(odmFile)!;

			Mp3ToMp4Writer? rr = null;

			foreach (var part in parts)
			{
				var partname = part.Name + Path.GetExtension(part.Filename);
				var partFilename = Path.Combine(dir, partname);

				if (!File.Exists(partFilename))
				{
					LogInfo($"Downloading {partname}");
					if (!await odm2.DownloadPartAsync(part, partFilename)) return;
				}
				else
				{
					LogInfo($"Found {partFilename}");
				}

				if (rr is null)
				{
					var fileOut = File.Open(Path.ChangeExtension(odmFile, "m4b"), FileMode.OpenOrCreate, FileAccess.ReadWrite);

					rr = Mp3ToMp4Writer.Create(partFilename, fileOut);

					if (rr is null)
					{
						LogError($"Failed to create {nameof(Mp3ToMp4Writer)}");
						return;
					}

					LogInfo($"Created {nameof(Mp3ToMp4Writer)} with file {partname}");

					var authors = odm2.Metadata?.Creators?.Where(c => c.Role is CreatorType.Author)?.Select(a => a.Name)?.ToArray();
					var narrators = odm2.Metadata?.Creators?.Where(c => c.Role is CreatorType.Narrator)?.Select(a => a.Name)?.ToArray();
					var genres = odm2.Metadata?.Subjects?.Select(a => a.Name)?.ToArray();

					if (odm2.Metadata?.Title is not null)
					{
						var title = odm2.Metadata.Title;
						if (odm2.Metadata?.SubTitle is not null)
							title += ": " + odm2.Metadata?.SubTitle;

						LogInfo($"Title:  {title}");

						rr.Moov.ILst.EditOrAddTag("©nam", title);
						rr.Moov.ILst.EditOrAddTag("©alb", title);
					}
					if (authors?.Length > 0)
					{
						var autStr = string.Join(", ", authors);
						LogInfo($"Author(s):  {autStr}");
						rr.Moov.ILst.EditOrAddTag("©aut", autStr);
						rr.Moov.ILst.EditOrAddTag("aART", autStr);
					}
					if (narrators?.Length > 0)
					{
						var nrtStr = string.Join(", ", narrators);
						LogInfo($"Narrator(s):  {nrtStr}");
						rr.Moov.ILst.EditOrAddTag("©nrt", nrtStr);
						rr.Moov.ILst.EditOrAddTag("TCOM", nrtStr);
					}
					if (genres?.Length > 0)
					{
						var genStr = string.Join(", ", genres);
						LogInfo($"Genre(s):  {genStr}");
						rr.Moov.ILst.EditOrAddTag("©gen", genStr);
					}
					if (odm2.Metadata?.Description is not null)
					{
						LogInfo($"Description:  {odm2.Metadata.Description}");
						rr.Moov.ILst.EditOrAddTag("©cmt", odm2.Metadata.Description);
						rr.Moov.ILst.EditOrAddTag("©des", odm2.Metadata.Description);
					}
					if (odm2.Metadata?.Publisher is not null)
					{
						LogInfo($"Publisher:  {odm2.Metadata.Publisher}");
						rr.Moov.ILst.EditOrAddTag("©pub", odm2.Metadata.Publisher);
					}
				}
				else
				{
					LogInfo($"Adding {partname}");
					rr?.AddMp3File(partFilename);
				}
			}

			rr?.Dispose();
			rr?.OutputFile.Close();

			if (rr?.OutputFile is FileStream fs)
			{
				LogInfo($"Moving moov atom to beginning of file");
				await Mpeg4Util.RelocateMoovToBeginningAsync(fs.Name, default, (a, b, c) => { });

				LogInfo($"Complete m4b saved at {fs.Name}");
			}
			LogInfo("Done!");
		}
	}
}