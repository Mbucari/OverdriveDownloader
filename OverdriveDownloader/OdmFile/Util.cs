namespace OverdriveDownloader.OdmFile
{
	internal class Util
	{
		public static TimeSpan ParseTimeSpan(string? timeSpan)
		{
			if (timeSpan is null) return default;
			var parts = timeSpan.Split(':');
			Array.Reverse(parts);

			var time = double.TryParse(parts[0], out var secs) ? TimeSpan.FromSeconds(secs) : TimeSpan.Zero;
			if (int.TryParse(parts[1], out var mins)) time += TimeSpan.FromMinutes(mins);
			if (parts.Length > 2 && int.TryParse(parts[2], out var hrs)) time += TimeSpan.FromHours(hrs);
			return time;
		}
	}
}
