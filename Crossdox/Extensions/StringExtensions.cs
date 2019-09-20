using System.Text;

namespace Crossdox.Extensions
{
	public static class StringExtensions
	{
		public static byte[] SHA256(this string text)
			=> Encoding.UTF8.GetBytes(text).SHA256();

		public static string StripBom(this string text)
		{
			if (text.Length >= 3
				&& text[0] == 0xEF && text[1] == 0xBB && text[2] == 0xBF)
				return text.Substring(3);
			if (text.Length >= 1
				&& (text[0] == 0xFEFF || text[0] == 0xFFFE))
				return text.Substring(1);

			return text;
		}

		public static string AddCSlashes(this string text)
		{
			StringBuilder stringBuilder = new StringBuilder();
			text.AddCSlashesTo(stringBuilder);
			return stringBuilder.ToString();
		}

		public static void AddCSlashesTo(this string text, StringBuilder stringBuilder)
		{
			int i, start;

			for (i = 0, start = 0; i < text.Length; )
			{
				char ch = text[i++];
				switch (ch)
				{
					case '\x00': case '\x01': case '\x02': case '\x03':
					case '\x04': case '\x05': case '\x06': case '\x07':
					case '\x08': case '\x09': case '\x0A': case '\x0B':
					case '\x0C': case '\x0D': case '\x0E': case '\x0F':
					case '\x10': case '\x11': case '\x12': case '\x13':
					case '\x14': case '\x15': case '\x16': case '\x17':
					case '\x18': case '\x19': case '\x1A': case '\x1B':
					case '\x1C': case '\x1D': case '\x1E': case '\x1F':
					case '\"':
					case '\\':
					escape:
						if (i - 1 > start)
							stringBuilder.Append(text, start, i - 1 - start);
						switch (ch)
						{
							case '\x07': stringBuilder.Append("\\a"); break;
							case '\x08': stringBuilder.Append("\\b"); break;
							case '\x09': stringBuilder.Append("\\t"); break;
							case '\x0A': stringBuilder.Append("\\n"); break;
							case '\x0B': stringBuilder.Append("\\v"); break;
							case '\x0C': stringBuilder.Append("\\f"); break;
							case '\x0D': stringBuilder.Append("\\r"); break;
							case '\\': stringBuilder.Append("\\\\"); break;
							case '\"': stringBuilder.Append("\\\""); break;

							default:
								stringBuilder.AppendFormat("\\u{0:X4}", (int)ch);
								break;
						}
						start = i;
						break;

					default:
						if (ch >= 127) goto escape;
						break;
				}
			}

			if (i > start)
				stringBuilder.Append(text, start, i - start);
		}
	}
}
