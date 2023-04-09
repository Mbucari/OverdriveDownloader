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

			var odm = await OverDriveMedia.LoadAsync(odmFile);

			if (odm is null)
			{
				LogError($"Couldn't open ODM file: {odmFile}");
				return;
			}

			var parts = odm.Formats.FirstOrDefault()?.Parts;

			if (parts?.Length < 1)
			{
				LogError("ODM file doesn't contain any downloadable parts");
				return;
			}

			LogInfo($"ODM file contains {parts!.Length} downloadable parts");

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
					var fileOut = File.Open(Path.ChangeExtension(odmFile, "m4b"), FileMode.OpenOrCreate, FileAccess.ReadWrite);

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

						mp4Writer.Moov.ILst.EditOrAddTag("©nam", title);
						mp4Writer.Moov.ILst.EditOrAddTag("©alb", title);
					}
					if (authors?.Length > 0)
					{
						var autStr = string.Join(", ", authors);
						LogInfo($"Author(s):  {autStr}");
						mp4Writer.Moov.ILst.EditOrAddTag("©aut", autStr);
						mp4Writer.Moov.ILst.EditOrAddTag("aART", autStr);
					}
					if (narrators?.Length > 0)
					{
						var nrtStr = string.Join(", ", narrators);
						LogInfo($"Narrator(s):  {nrtStr}");
						mp4Writer.Moov.ILst.EditOrAddTag("©nrt", nrtStr);
						mp4Writer.Moov.ILst.EditOrAddTag("TCOM", nrtStr);
					}
					if (genres?.Length > 0)
					{
						var genStr = string.Join(", ", genres);
						LogInfo($"Genre(s):  {genStr}");
						mp4Writer.Moov.ILst.EditOrAddTag("©gen", genStr);
					}
					if (odm.Metadata?.Description is not null)
					{
						LogInfo($"Description:  {odm.Metadata.Description}");
						mp4Writer.Moov.ILst.EditOrAddTag("©cmt", odm.Metadata.Description);
						mp4Writer.Moov.ILst.EditOrAddTag("©des", odm.Metadata.Description);
					}
					if (odm.Metadata?.Publisher is not null)
					{
						LogInfo($"Publisher:  {odm.Metadata.Publisher}");
						mp4Writer.Moov.ILst.EditOrAddTag("©pub", odm.Metadata.Publisher);
					}
				}
				else
				{
					LogInfo($"Adding {partname}");
					mp4Writer?.AddMp3File(partFilename);
				}
			}

			mp4Writer?.Dispose();
			mp4Writer?.OutputFile.Close();

			if (mp4Writer?.OutputFile is FileStream fs)
			{
				LogInfo($"Moving moov atom to beginning of file");
				await Mpeg4Util.RelocateMoovToBeginningAsync(fs.Name, default, (a, b, c) => { });
				LogInfo($"Complete m4b saved at {fs.Name}");
			}

			LogInfo("Done!");
		}
	}
}