namespace Database.Util;

public static class AsciiUtils {
    public static string[] ToAsciiTable(this List<List<string>> table, string name = "", params string[] columns) {
        string[] result = new string[table.Count + 4]; // 3 x separator, 1 x header
        int cols = table.Max(r => r.Count);
        int[] colWidths = new int[cols];

        for (int ch = 0; ch < columns.Length; ch++)
            colWidths[ch] = columns[ch].Length + 2;

        for (int c = 0; c < cols; c++) {
            int colWidth = table.Max(r => r.Count + 2 > c + 2 ? r[c].Length + 2 : 0);
            if (colWidth > colWidths[c]) colWidths[c] = colWidth;
        }

        string separator = "+" + string.Join("+", colWidths.Select(w => new string('-', w + 2))) + "+";

        result[0] = separator;

        string headerStr = "|";
        for (int i = 0; i < columns.Length; i++) headerStr += $" {columns[i].ToFixedLength(colWidths[i])} |";
        result[1] = headerStr;

        result[2] = separator;

        for (int r = 0; r < table.Count; r++) {
            List<string> row = table[r];
            string str = "|";
            for (int c = 0; c < row.Count; c++) {
                string cell = row[c];
                str += $" {cell.ToFixedLength(colWidths[c])} |";
            }

            result[r + 3] = str;
        }

        result[^1] = separator;

        if (name != string.Empty)
            return result
                .Prepend($"| {"".ToFixedLength((separator.Length - name.Length) / 2 - 2)} {name} {"".ToFixedLength((separator.Length - name.Length) / 2 - 3)} |")
                .Prepend("+" + new string('-', separator.Length - 2) + "+")
                .ToArray();

        return result;
    }
}