using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static OverdriveDownloader.Program;

namespace OverdriveDownloader.OdmFile
{
	public record OverDriveMedia
	{
		const string ClientID = "6D51D9B1-69FA-4442-BD47-6FF51B66FCAD";
		const string OMC = "3.6.0.0";
		const string OS = "6.2.2.9200";
		const string EXTRA = "ELOSNOC*AIDEM*EVIRDREVO";

		public static async Task<OverDriveMedia?> LoadAsync(string? odmFile)
		{
			if (odmFile is null || !File.Exists(odmFile)) return null;
			var odmDocument = XDocument.Load(odmFile);

			var license = odmDocument.Root?.Element("License");

			if (license is null) return null;

			var odm = new OverDriveMedia(odmDocument);

			if (license.Element("AcquisitionUrl")?.Value is string url)
			{
				LogInfo("Acquiring license and saving it to ODM file");

				var lic = await odm.GetLicenseAsync(url);

				if (lic is null) return null;

				license.RemoveAll();
				license.Add(new XCData(lic));

				using var fs = File.OpenWrite(odmFile);
				await odmDocument.SaveAsync(fs, SaveOptions.None, default);
			}

			odm.Client.DefaultRequestHeaders.Add("ClientID", ClientID);
			odm.Client.DefaultRequestHeaders.Add("License", license.Value);

			return odm;
		}

		private OverDriveMedia(XDocument document)
		{
			MediaID = document.Root?.Attribute("id")?.Value;
			EarlyReturnURL = document.Root?.Element(nameof(EarlyReturnURL))?.Value;
			DownloadSuccessURL = document.Root?.Element(nameof(DownloadSuccessURL))?.Value;
			Metadata = Metadata.Parse(document.Root?.Nodes().OfType<XCData>().FirstOrDefault()?.Value is string str ? XElement.Parse(str) : null);

			Formats = document.Root?.Element(nameof(Formats))
				?.Elements()
				.Select(Format.Parse)
				.OfType<Format>().ToArray() ?? Array.Empty<Format>();

			Client.DefaultRequestHeaders.Add("User-Agent", "OverDrive Media Console");
		}

		public async Task<bool> DownloadPartAsync(Part part, string filename)
		{
			var response = await Client.GetAsync(part.FileUrl, HttpCompletionOption.ResponseHeadersRead);

			if (!response.IsSuccessStatusCode)
			{
				LogError($"Server Response {response.StatusCode} for file {part.FileUrl}");
				return false;
			}

			using var fs = File.OpenWrite(filename);

			var stream = await response.Content.ReadAsStreamAsync();
			await stream.CopyToAsync(fs);

			return true;
		}

		public async Task<Stream> GetPartStreamAsync(Part part)
		{
			var response = await Client.GetAsync(part.FileUrl, HttpCompletionOption.ResponseHeadersRead);

			return await response.Content.ReadAsStreamAsync();
		}

		private async Task<string?> GetLicenseAsync(string licUrl)
		{
			var hash = Convert.ToBase64String(SHA1.HashData(Encoding.Unicode.GetBytes($"{ClientID}|{OMC}|{OS}|{EXTRA}")));
			var queryString = $"MediaID={MediaID}&ClientID={ClientID}&OMC={OMC}&OS={OS}&Hash={hash}";

			var resp = await Client.GetAsync($"{licUrl}?{queryString}");

			var license = await resp.Content.ReadAsStringAsync();

			if (!resp.IsSuccessStatusCode)
			{
				LogError($"Error acquiring license with status code '{resp.StatusCode}'. Response:\r\n{license}");
				return null;
			}

			return license;
		}

		private readonly HttpClient Client = new();

		public string? MediaID { get; }
		public string? EarlyReturnURL { get; }
		public string? DownloadSuccessURL { get; }
		public Metadata? Metadata { get; }
		public Format[] Formats { get; }
	}
}
