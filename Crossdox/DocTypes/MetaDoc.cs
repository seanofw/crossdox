using System;

namespace Crossdox.DocTypes
{
	public class MetaDoc : IEquatable<MetaDoc>
	{
		public string Summary { get; }
		public string Remarks { get; }
		public string Example { get; }
		public string See { get; }
		public string SeeAlso { get; }

		public MetaDoc(string summary = null, string remarks = null, string example = null,
			string see = null, string seeAlso = null)
		{
			Summary = summary;
			Remarks = remarks;
			Example = example;
			See = see;
			SeeAlso = seeAlso;
		}

		public MetaDoc WithSummary(string summary)
			=> new MetaDoc(summary: summary, remarks: Remarks, example: Example, see: See, seeAlso: SeeAlso);
		public MetaDoc WithRemarks(string remarks)
			=> new MetaDoc(summary: Summary, remarks: remarks, example: Example, see: See, seeAlso: SeeAlso);
		public MetaDoc WithExample(string example)
			=> new MetaDoc(summary: Summary, remarks: Remarks, example: example, see: See, seeAlso: SeeAlso);
		public MetaDoc WithSee(string see)
			=> new MetaDoc(summary: Summary, remarks: Remarks, example: Example, see: see, seeAlso: SeeAlso);
		public MetaDoc WithSeeAlso(string seeAlso)
			=> new MetaDoc(summary: Summary, remarks: Remarks, example: Example, see: See, seeAlso: seeAlso);

		public override bool Equals(object obj)
			=> Equals(obj as MetaDoc);

		public virtual bool Equals(MetaDoc other)
			=> other != null
				&& Summary == other.Summary
				&& Remarks == other.Remarks
				&& Example == other.Example
				&& See == other.See
				&& SeeAlso == other.SeeAlso;

		public override int GetHashCode()
		{
			int hashCode = 0;
			hashCode = (hashCode * 29) + (Summary ?? "").GetHashCode();
			hashCode = (hashCode * 29) + (Remarks ?? "").GetHashCode();
			hashCode = (hashCode * 29) + (Example ?? "").GetHashCode();
			hashCode = (hashCode * 29) + (See ?? "").GetHashCode();
			hashCode = (hashCode * 29) + (SeeAlso ?? "").GetHashCode();
			return hashCode;
		}

		public static bool operator ==(MetaDoc a, MetaDoc b)
			=> ReferenceEquals(a, null)
				? ReferenceEquals(b, null)
				: a.Equals(b);

		public static bool operator !=(MetaDoc a, MetaDoc b)
			=> ReferenceEquals(a, null)
				? !ReferenceEquals(b, null)
				: !a.Equals(b);
	}
}
