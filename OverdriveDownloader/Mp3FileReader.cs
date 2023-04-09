namespace NAudio.Wave
{
	public class Mp3FileReader : Mp3FileReaderBase
	{
		/// <summary>Supports opening a MP3 file</summary>
		public Mp3FileReader(string mp3FileName)
			: base(File.OpenRead(mp3FileName), CreateAcmFrameDecompressor, true) { }

		public static IMp3FrameDecompressor CreateAcmFrameDecompressor(WaveFormat mp3Format)
			=> new Dummy { OutputFormat = new WaveFormat(mp3Format.SampleRate, 16, mp3Format.Channels) };

		private class Dummy : IMp3FrameDecompressor
		{
			public required WaveFormat OutputFormat { get; init; }
			public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset) => throw new NotImplementedException();
			public void Dispose() { }
			public void Reset() { }
		}
	}
}
