using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LuaDotNet.DebuggingHelpers
{
    internal sealed class DebugTable
    {
        public DebugTable(params string[] headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (!headers.Any())
            {
                throw new ArgumentException("Headers cannot be empty.", nameof(headers));
            }

            ColumnHeaders = headers;
        }
        
        private IList<string> ColumnHeaders { get; }
        
        private IList<string[]> Entities { get; } = new List<string[]>();
        
        public void AddRow(params object[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length > ColumnHeaders.Count)
            {
                throw new ArgumentException("Too much data.", nameof(values));
            }

            Entities.Add(values.Select(v => v.ToString()).ToArray());
        }
        
        public string GetOutput()
        {
            var tableBuilder = new StringBuilder();
            tableBuilder.AppendLine(GetRowSeparatorString('='));

            var columnLengths = GetColumnLengths().ToArray();
            for (var i = 0; i < ColumnHeaders.Count; ++i)
            {
                var header = ColumnHeaders[i];
                tableBuilder.Append($"{header}{new string(' ', columnLengths[i] - header.Length)} |");
            }

            tableBuilder.AppendLine($"\n{GetRowSeparatorString('=')}");
            foreach (var entity in Entities)
            {
                for (var i = 0; i < entity.Length; ++i)
                {
                    var value = entity[i];
                    tableBuilder.Append($"{value}{new string(' ', columnLengths[i] - value.Length)} |");
                }

                tableBuilder.AppendLine($"\n{GetRowSeparatorString()}");
            }

            return tableBuilder.ToString();
        }
        
        private IEnumerable<int> GetColumnLengths()
        {
            return ColumnHeaders.Select((c, ix) =>
                Entities.Select(e => e[ix]).Union(new[] {ColumnHeaders[ix]}).Max(s => s.Length));
        }

        private string GetRowSeparatorString(char separator = '-') =>
            new string(separator, GetColumnLengths().Sum() + ColumnHeaders.Count * 2);
    }
}