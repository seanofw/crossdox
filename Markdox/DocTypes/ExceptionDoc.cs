using System;

namespace Markdox.DocTypes
{
	public class ExceptionDoc : IEquatable<ExceptionDoc>
	{
		public NameInfo Cref { get; }
		public string Description { get; }

		public ExceptionDoc(NameInfo cref, string description = null)
		{
			Cref = cref ?? throw new ArgumentNullException(nameof(cref));
			Description = description;
		}

		public ExceptionDoc WithCref(NameInfo cref)
			=> new ExceptionDoc(cref, Description);
		public ExceptionDoc WithDescription(string description)
			=> new ExceptionDoc(Cref, description);

		public override bool Equals(object obj)
			=> Equals(obj as ExceptionDoc);
		public bool Equals(ExceptionDoc other)
			=> other != null
				&& Cref == other.Cref && Description == other.Description;

		public override int GetHashCode()
			=> Cref.GetHashCode();

		public static bool operator ==(ExceptionDoc a, ExceptionDoc b)
			=> ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
		public static bool operator !=(ExceptionDoc a, ExceptionDoc b)
			=> ReferenceEquals(a, null) ? !ReferenceEquals(b, null) : !a.Equals(b);

		public override string ToString()
			=> Cref.ToString();
	}
}