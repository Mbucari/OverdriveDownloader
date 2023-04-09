using AAXClean;
using AAXClean.FrameFilters.Audio;
using Mpeg4Lib.Boxes;
using NAudio.Lame.ID3;
using NAudio.Wave;
using OverdriveDownloader.OdmFile;
using System.Xml.Linq;
using static OverdriveDownloader.Logging;

namespace OverdriveDownloader
{
	public class Mp3ToMp4Writer : Mp4aWriter
	{
		private Mp3FileReader Mp3FileReader { get; set; }
		public TimeSpan TotalDuration { get; set; }

		private readonly ChapterQueue ChapterQueue;
		public Mp3ToMp4Writer(Stream outputFile, FtypBox ftyp, MoovBox moov, Mp3FileReader mp3FileReader)
			: base(outputFile, ftyp, moov, mp3FileReader.Mp3WaveFormat.SampleRate, mp3FileReader.Mp3WaveFormat.Channels)
		{
			Mp3FileReader = mp3FileReader;

			var rate = (SampleRate)mp3FileReader.Mp3WaveFormat.SampleRate;
			ChapterQueue = new ChapterQueue(rate, rate);
		}

		public void AddMp3File(string file)
		{
			Mp3FileReader?.Dispose();
			Mp3FileReader = new Mp3FileReader(file);

			ReadAndQueueChapters();
			ProcessCurrent();
		}

		private void ProcessCurrent()
		{
			while (ChapterQueue.TryGetNextChapter(out var ce))
				WriteChapter(ce);

			Mp3Frame frame;
			int frameCount = 0;

			while ((frame = Mp3FileReader.ReadNextFrame()) != null)
			{
				AddFrame(frame.RawData, frameCount++ == 0);
				frameCount %= 20;
			}

			TotalDuration += Mp3FileReader.TotalTime;
		}

		public static Mp3ToMp4Writer? Create(string? mpfFiles, Stream output)
		{
			if (mpfFiles is null || !File.Exists(mpfFiles)) return null;

			var mp3 = new Mp3FileReader(mpfFiles);

			using var ms = new MemoryStream(Properties.Resources.blankmoov);

			if (BoxFactory.CreateBox(ms, null) is not MoovBox moov) return null;

			var writer = new Mp3ToMp4Writer(output, GenerateFtype(), moov, mp3);

			writer.ReadAndQueueChapters();
			writer.ProcessCurrent();
			return writer;
		}

		protected override void SaveMoov()
		{
			Moov.AudioTrack.Tkhd.Duration = (ulong)TotalDuration.TotalMilliseconds;
			Moov.AudioTrack.Mdia.Mdhd.Duration = (ulong)(Moov.AudioTrack.Mdia.Mdhd.Timescale * TotalDuration.TotalSeconds);
			Moov.Mvhd.Duration = (ulong)TotalDuration.TotalMilliseconds;
			Moov.AudioTrack.Mdia.Minf.Stbl.Stts.Samples.Clear();
			Moov.AudioTrack.Mdia.Minf.Stbl.Stts.Samples.Add(new SttsBox.SampleEntry((uint)Moov.AudioTrack.Mdia.Minf.Stbl.Stsz.SampleCount, 1152));
			base.SaveMoov();

			Mp3FileReader?.Dispose();
		}

		private void ReadAndQueueChapters()
		{
			var ms = new MemoryStream(Mp3FileReader.Id3v2Tag.RawData);
			var id3 = new Id3Tag(ms);

			var chapters = id3.Frames.OfType<TXXXFrame>().FirstOrDefault(f => f.FieldName == "OverDrive MediaMarkers");
			if (chapters is null) return;

			if (!Moov.ILst.Children.Any(t => t.Header.Type == "covr") && id3.Frames.OfType<APICFrame>().FirstOrDefault()?.Image is byte[] b)
			{
				LogInfo("Adding Cover Art");
				Moov.ILst.AddTag("covr", b, AppleDataType.JPEG);
			}

			var markers = XElement.Parse(chapters.FieldValue).Elements().Select(Marker.Parse).OfType<Marker>().ToList();

			var chinfo = new ChapterInfo(TotalDuration);

			for (int j = 0; j < markers.Count; j++)
			{
				var duration = j == markers.Count - 1 ? Mp3FileReader.TotalTime - markers[^1].Time : markers[j + 1].Time - markers[j].Time;
				chinfo.Add(markers[j].Name, duration);
				LogInfo($"Chapter:  \"{chinfo.Chapters[^1].Title}\", Start {chinfo.Chapters[^1].StartOffset}");
			}

			ChapterQueue.AddRange(chinfo);
		}

		private static FtypBox GenerateFtype()
		{
			var Ftyp = FtypBox.Create(8, null);
			Ftyp.MajorBrand = "isom";
			Ftyp.MajorVersion = 0x200;
			Ftyp.CompatibleBrands.Add("iso2");
			Ftyp.CompatibleBrands.Add("mp41");

			return Ftyp;
		}
	}
}
