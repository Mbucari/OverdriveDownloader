using AAXClean;
using AAXClean.FrameFilters.Audio;
using Mpeg4Lib.Boxes;
using NAudio.Lame.ID3;
using NAudio.Wave;
using OverdriveDownloader.OdmFile;
using System.Xml.Linq;
using static OverdriveDownloader.Program;

namespace OverdriveDownloader
{
	public class Mp3ToMp4Writer : Mp4aWriter
	{
		private Mp3FileReader2 Mp3FileReader { get; set; }
		public TimeSpan TotalDuration { get; set; }

		private readonly ChapterQueue chQueue;
		public Mp3ToMp4Writer(Stream outputFile, FtypBox ftyp, MoovBox moov, Mp3FileReader2 mp3FileReader)
			: base(outputFile, ftyp, moov, mp3FileReader.Mp3WaveFormat.SampleRate, mp3FileReader.Mp3WaveFormat.Channels)
		{
			Mp3FileReader = mp3FileReader;

			var rate = (SampleRate)mp3FileReader.Mp3WaveFormat.SampleRate;
			chQueue = new ChapterQueue(rate, rate);

			var chinfo = GetChapters(mp3FileReader.Id3v2Tag, default, mp3FileReader.TotalTime);
			if (chinfo is not null)
				chQueue.AddRange(chinfo);
		}

		public void AddMp3File(string file)
		{
			Mp3FileReader?.Dispose();
			Mp3FileReader = new Mp3FileReader2(file);
			var chinfo = GetChapters(Mp3FileReader.Id3v2Tag, TotalDuration, Mp3FileReader.TotalTime);
			if (chinfo is not null)
				chQueue.AddRange(chinfo);
			ProcessCurrent();
		}

		private void ProcessCurrent()
		{
			Mp3Frame frame;
			int frameCount = 0;

			while (chQueue.TryGetNextChapter(out var ce))
				WriteChapter(ce);

			while ((frame = Mp3FileReader.ReadNextFrame()) != null)
			{
				var crc = new byte[2];
				var sideData = frame.ChannelMode == ChannelMode.Mono ? new byte[17] : new byte[32];

				Array.Copy(frame.RawData, 4, crc, 0, crc.Length);
				Array.Copy(frame.RawData, 6, sideData, 0, sideData.Length);

				AddFrame(frame.RawData, frameCount++ == 0);
				frameCount %= 20;
			}

			TotalDuration += Mp3FileReader.TotalTime;
		}

		public static Mp3ToMp4Writer? Create(string? mpfFiles, Stream output)
		{
			if (mpfFiles is null || !File.Exists(mpfFiles)) return null;

			var mp3 = new Mp3FileReader2(mpfFiles);

			using var ms = new MemoryStream(Properties.Resources.blankmoov);

			var moov = BoxFactory.CreateBox(ms, null) as MoovBox;

			if (moov is null) return null;

			moov.AudioTrack.Mdia.Hdlr.HandlerName = "SoundHandler";
			moov.AudioTrack.Mdia.Hdlr.HasNullTerminator = true;

			var writer = new Mp3ToMp4Writer(output, GenerateFtype(), moov, mp3);
			writer.ProcessCurrent();
			return writer;
		}

		protected override void SaveMoov()
		{
			Moov.AudioTrack.Mdia.Hdlr.HandlerName = "SoundHandler";
			Moov.AudioTrack.Mdia.Hdlr.HasNullTerminator = true;
			Moov.AudioTrack.Tkhd.Duration = (ulong)TotalDuration.TotalMilliseconds;
			Moov.AudioTrack.Tkhd.AlternateGroup = 1;
			Moov.AudioTrack.Mdia.Mdhd.Duration = (ulong)(Moov.AudioTrack.Mdia.Mdhd.Timescale * TotalDuration.TotalSeconds);
			Moov.Mvhd.Duration = (ulong)TotalDuration.TotalMilliseconds;
			Moov.Mvhd.Timescale = 1000;
			Moov.AudioTrack.Mdia.Minf.Stbl.Stts.Samples.Clear();
			Moov.AudioTrack.Mdia.Minf.Stbl.Stts.Samples.Add(new SttsBox.SampleEntry((uint)Moov.AudioTrack.Mdia.Minf.Stbl.Stsz.SampleCount, 1152));
			base.SaveMoov();

			Mp3FileReader?.Dispose();
		}

		private ChapterInfo? GetChapters(Id3v2Tag id3V2Tag, TimeSpan fileStart, TimeSpan fileDuration)
		{
			var ms = new MemoryStream(id3V2Tag.RawData);
			var id3 = new Id3Tag(ms);

			var chapters = id3.Frames.OfType<TXXXFrame>().FirstOrDefault(f => f.FieldName == "OverDrive MediaMarkers");
			if (chapters is null) return null;

			if (id3.Frames.OfType<APICFrame>().FirstOrDefault()?.Image is byte[] b)
				Moov.ILst.EditOrAddTag("covr", b, AppleDataType.JPEG);

			var elem = XElement.Parse(chapters.FieldValue);

			var markers = elem.Elements().Select(Marker.Parse).OfType<Marker>().ToList();

			var chinfo = new ChapterInfo(fileStart);

			for (int j = 0; j < markers.Count; j++)
			{
				var duration = j == markers.Count - 1 ? fileDuration - markers[^1].Time : markers[j + 1].Time - markers[j].Time;
				chinfo.Add(markers[j].Name, duration);
				LogInfo($"Chapter:  \"{chinfo.Chapters[^1].Title}\", Start {chinfo.Chapters[^1].StartOffset}");
			}

			return chinfo;
		}

		private static FtypBox GenerateFtype()
		{
			var Ftyp = FtypBox.Create(8, null);
			Ftyp.MajorBrand = "isom";
			Ftyp.MajorVersion = 0x200;
			Ftyp.CompatibleBrands.Add("iso2");
			Ftyp.CompatibleBrands.Add("mp41");
			Ftyp.CompatibleBrands.Add("M4A ");
			Ftyp.CompatibleBrands.Add("M4B ");

			return Ftyp;
		}
	}
}
