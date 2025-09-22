namespace Database.Util;

public static class AsciiUtils {
    public static string[] ToAsciiTable(this List<List<string>> table, string name, params string[] columns) {
        string[] result = new string[table.Count + 4]; // 3 x separator, 1 x header
        int cols = table.Max(r => r.Count);
        int[] colWidths = new int[cols];

        for (int ch = 0; ch < columns.Length; ch++) colWidths[ch] = columns[ch].Length;
        for (int c = 0; c < cols; c++) colWidths[c] = table.Max(r => r.Count > c ? r[c].Length : 0);

        string separator = "+" + string.Join("+", colWidths.Select(w => new string('-', w + 2))) + "+";

        result[0] = separator;
        result[2] = separator;

        for (int r = 0; r < table.Count; r++) {
            List<string> row = table[r];
            string str = "|";
            for (int c = 0; c < cols; c++) {
                string cell = c < row.Count ? row[c] : string.Empty;
                str += $" {cell} |";
            }

            result[r + 3] = str;
        }

        result[^1] = separator;

        return result;
    }
}