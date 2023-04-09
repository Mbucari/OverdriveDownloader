namespace OverdriveDownloader
{
	internal class Logging
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
	}
}
