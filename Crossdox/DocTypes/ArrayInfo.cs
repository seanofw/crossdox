using System;

namespace Crossdox.DocTypes
{
	public class ArrayInfo : IEquatable<ArrayInfo>
	{
		public int Dimensions { get; }

		public ArrayInfo(int dimensions)
		{
			Dimensions = dimensions;
		}

		public ArrayInfo WithDimensions(int dimensions)
			=> new ArrayInfo(Dimensions);

		public override bool Equals(object obj)
			=> Equals(obj as ArrayInfo);
		public bool Equals(ArrayInfo other)
			=> other != null
				&& Dimensions == other.Dimensions;

		public override int GetHashCode()
			=> Dimensions.GetHashCode();

		public static bool operator ==(ArrayInfo a, ArrayInfo b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(ArrayInfo a, ArrayInfo b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> Dimensions > 0 ? '[' + new string(',', Dimensions - 1) + ']' : "[]";
	}
}