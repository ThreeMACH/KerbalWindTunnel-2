using System;

namespace Graphing.IO
{
    public interface ISpreadsheetWriter
    {
        void Write(string path, string sheet, object data, SpreadsheetOptions? options = null);
    }
    [Serializable]
    public readonly struct SpreadsheetOptions : IEquatable<SpreadsheetOptions>
    {
        public readonly bool headers, filter;
        public readonly int freezeRowCount, freezeColumnCount;
#if OUTSIDE_UNITY
        public SpreadsheetOptions() : this(true, true, 1, 0) { }
#endif
        public SpreadsheetOptions(bool headers = true, bool filter = true, int freezeRowCount = 1, int freezeColumnCount = 0)
        {
            this.headers = headers;
            this.filter = filter;
            this.freezeRowCount = freezeRowCount;
            this.freezeColumnCount = freezeColumnCount;
        }

        public bool Equals(SpreadsheetOptions other)
            => filter == other.filter && freezeRowCount == other.freezeRowCount && freezeColumnCount == other.freezeColumnCount;
    }
}
