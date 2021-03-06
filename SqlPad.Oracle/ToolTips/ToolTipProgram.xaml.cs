﻿using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using SqlPad.Oracle.DataDictionary;

namespace SqlPad.Oracle.ToolTips
{
	public partial class ToolTipProgram
	{
		public ToolTipProgram(string title, string documentation, OracleProgramMetadata programMetadata)
		{
			InitializeComponent();

			LabelTitle.Text = title;
			LabelDocumentation.Text = documentation;

			IsExtractDdlVisible =
				programMetadata.Type != ProgramType.PackageProcedure &&
				programMetadata.Type != ProgramType.PackageFunction &&
				programMetadata.Owner != null &&
				String.IsNullOrEmpty(programMetadata.Identifier.Package);

			DataContext = programMetadata;
		}

		protected override Task<string> ExtractDdlAsync(CancellationToken cancellationToken)
		{
			return ScriptExtractor.ExtractSchemaObjectScriptAsync(((OracleProgramMetadata)DataContext).Owner, cancellationToken);
		}
	}

	public class AuthIdConverter : ValueConverterBase
	{
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null
				? ValueNotAvailable
				: (AuthId)value == AuthId.CurrentUser
					? "Current user"
					: "Definer";
		}
	}

	public class ProgramTypeConverter : ValueConverterBase
	{
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null
				? ValueNotAvailable
				: BuildTypeLabel((OracleProgramMetadata)value);
		}

		private static string BuildTypeLabel(OracleProgramMetadata metadata)
		{
			switch (metadata.Type)
			{
				case ProgramType.StatementFunction:
					return "Statement defined function";
				case ProgramType.PackageFunction:
					return "Package function";
				case ProgramType.PackageProcedure:
					return "Package procedure";
			}

			var label = String.IsNullOrEmpty(metadata.Identifier.Owner)
				? "SQL "
				: metadata.IsBuiltIn ? "Built-in " : null;

			if (!String.IsNullOrEmpty(metadata.Identifier.Package))
			{
				label = $"{label}{(label == null ? "Package " : "package ")}";
			}
			else if (!String.IsNullOrEmpty(metadata.Identifier.Owner))
			{
				label = "Schema ";
			}
			
			var programType = metadata.Type.ToString().ToLowerInvariant();
			return $"{label}{programType}";
		}
	}
}
