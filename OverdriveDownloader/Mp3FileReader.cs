using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{

    /// <summary>
    /// Class for reading from MP3 files
    /// </summary>
    public class Mp3FileReader2 : Mp3FileReaderBase
    {
        /// <summary>Supports opening a MP3 file</summary>
        public Mp3FileReader2(string mp3FileName)
            : base(File.OpenRead(mp3FileName), CreateAcmFrameDecompressor, true)
        {
        }

        /// <summary>
        /// Opens MP3 from a stream rather than a file
        /// Will not dispose of this stream itself
        /// </summary>
        /// <param name="inputStream">The incoming stream containing MP3 data</param>
        public Mp3FileReader2(Stream inputStream)
            : base(inputStream, CreateAcmFrameDecompressor, false)
        {

        }

        /// <summary>
        /// Creates an ACM MP3 Frame decompressor. This is the default with NAudio
        /// </summary>
        /// <param name="mp3Format">A WaveFormat object based </param>
        /// <returns></returns>
        public static IMp3FrameDecompressor CreateAcmFrameDecompressor(WaveFormat mp3Format) => new Dummy(mp3Format);

		private class Dummy : IMp3FrameDecompressor
		{
            public Dummy(WaveFormat outputFormat)
			{
				OutputFormat = new WaveFormat(outputFormat.SampleRate, 16, outputFormat.Channels);
			}

			public WaveFormat OutputFormat { get; }

			public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset) => throw new NotImplementedException();
			public void Dispose() { }
            public void Reset() { }
		}
	}
}
