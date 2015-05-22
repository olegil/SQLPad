using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SqlPad
{
	public class HtmlDataExporter : IDataExporter
	{
		private const string QuoteCharacter = "\"";
		private const string EscapedQuote = "&quot;";

		public string FileNameFilter
		{
			get { return "HTML files (*.html)|*.html|All files (*.*)|*"; }
		}

		public void ExportToClipboard(DataGrid dataGrid, IDataExportConverter dataExportConverter)
		{
			ExportToFile(null, dataGrid, dataExportConverter);
		}

		public void ExportToFile(string fileName, DataGrid dataGrid, IDataExportConverter dataExportConverter)
		{
			ExportToFileAsync(fileName, dataGrid, dataExportConverter, CancellationToken.None).Wait();
		}

		public Task ExportToClipboardAsync(DataGrid dataGrid, IDataExportConverter dataExportConverter, CancellationToken cancellationToken)
		{
			return ExportToFileAsync(null, dataGrid, dataExportConverter, cancellationToken);
		}

		public Task ExportToFileAsync(string fileName, DataGrid dataGrid, IDataExportConverter dataExportConverter, CancellationToken cancellationToken)
		{
			var orderedColumns = dataGrid.Columns
					.OrderBy(c => c.DisplayIndex)
					.ToArray();

			var columnHeaders = orderedColumns
				.Select(c => c.Header.ToString().Replace("__", "_").Replace(QuoteCharacter, EscapedQuote));

			var headerLine = BuildlTableRowTemplate(columnHeaders.Select(h => String.Format("<th>{0}</th>", h)));
			var htmlTableRowTemplate = BuildlTableRowTemplate(Enumerable.Range(0, orderedColumns.Length).Select(i => String.Format("<td>{{{0}}}</td>", i)));

			var rows = dataGrid.Items;

			return DataExportHelper.RunExportActionAsync(fileName, w => ExportInternal(headerLine, htmlTableRowTemplate, rows, w, cancellationToken));
		}

		private static string BuildlTableRowTemplate(IEnumerable<string> columnValues)
		{
			var htmlTableRowTemplateBuilder = new StringBuilder();
			htmlTableRowTemplateBuilder.Append("<tr>");
			htmlTableRowTemplateBuilder.Append(String.Concat(columnValues));
			htmlTableRowTemplateBuilder.Append("<tr>");
			return htmlTableRowTemplateBuilder.ToString();
		}

		private void ExportInternal(string headerLine, string htmlTemplate, ICollection rows, TextWriter writer, CancellationToken cancellationToken)
		{
			writer.Write("<!DOCTYPE html><html><head><title></title></head><body><table border=\"1\" style=\"border-collapse:collapse\">");
			writer.Write(headerLine);

			foreach (object[] rowValues in rows)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var values = rowValues.Select((t, i) => (object)FormatHtmlValue(t)).ToArray();
				writer.Write(htmlTemplate, values);
			}

			writer.Write("<table>");
		}

		private static string FormatHtmlValue(object value)
		{
			return DataExportHelper.IsNull(value) ? "NULL" : value.ToString().Replace(">", "&gt;").Replace("<", "&lt;");
		}
	}
}
