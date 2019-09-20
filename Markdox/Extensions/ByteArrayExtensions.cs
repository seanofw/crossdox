using System;
using System.Security.Cryptography;
using System.Text;

namespace Markdox.Extensions
{
	public static class ByteArrayExtensions
	{
		private static readonly char[] _lowercaseHexChars = "0123456789abcdef".ToCharArray();
		private static readonly char[] _uppercaseHexChars = "0123456789abcdef".ToCharArray();

		public static string ToHexChars(this byte[] buffer, bool uppercase = true)
			=> ToHexChars(buffer, 0, buffer.Length, uppercase);

		public static string ToHexChars(this byte[] buffer, int start, int length, bool uppercase = true)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentException(nameof(start));
			if (length < 0 || start > buffer.Length - length)
				throw new ArgumentException(nameof(length));

			if (length == 0)
				return string.Empty;

			char[] chars = new char[length << 1];

			char[] hexChars = uppercase ? _uppercaseHexChars : _lowercaseHexChars;

			for (int i = 0; i < length; i++)
			{
				byte b = buffer[i];
				chars[(i << 1)    ] = hexChars[b >> 4];
				chars[(i << 1) + 1] = hexChars[b & 0xF];
			}

			return new string(chars);
		}

		public static byte[] SHA256(this byte[] bytes)
			=> new SHA256Managed().ComputeHash(bytes);
	}
}
