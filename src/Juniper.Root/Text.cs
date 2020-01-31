using System.Linq;

namespace Juniper
{
    public partial class MediaType
    {
        public sealed partial class Text : MediaType
        {
            private Text(string value, string[] extensions) : base("text/" + value, extensions) { }

            private Text(string value) : this(value, null) { }

            public static readonly Text AnyText = new Text("*");

            public override bool Matches(string fileName)
            {
                if (ReferenceEquals(this, AnyText))
                {
                    return Values.Any(x => x.Matches(fileName));
                }
                else
                {
                    return base.Matches(fileName);
                }
            }
        }
    }
}
