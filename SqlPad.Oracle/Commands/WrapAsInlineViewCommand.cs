﻿using System;
using System.Linq;
using System.Text;
using SqlPad.Commands;
using Terminals = SqlPad.Oracle.OracleGrammarDescription.Terminals;

namespace SqlPad.Oracle.Commands
{
	internal class WrapAsInlineViewCommand : OracleCommandBase
	{
		public const string Title = "Wrap as inline view";

		public static CommandExecutionHandler ExecutionHandler = CreateStandardExecutionHandler<WrapAsInlineViewCommand>("WrapAsInlineView");

		private WrapAsInlineViewCommand(OracleCommandExecutionContext executionContext)
			: base(executionContext)
		{
		}

		protected override bool CanExecute()
		{
			return CurrentNode != null && CurrentQueryBlock != null &&
				   CurrentNode.Id == Terminals.Select &&
				   CurrentQueryBlock.Columns.Any(c => !String.IsNullOrEmpty(c.NormalizedName));
		}

		protected override void Execute()
		{
			ExecutionContext.EnsureSettingsProviderAvailable();

			var settingsModel = ExecutionContext.SettingsProvider.Settings;
			settingsModel.Title = Title;
			settingsModel.Heading = settingsModel.Title;
			settingsModel.Description = "Enter an alias for the inline view";

			if (!ExecutionContext.SettingsProvider.GetSettings())
				return;

			var tableAlias = settingsModel.Value;

			var queryBlock = SemanticModel.GetQueryBlock(CurrentNode);

			var builder = new StringBuilder("SELECT ");
			var columnList = String.Join(", ", queryBlock.Columns
				.Where(c => !c.IsAsterisk && !String.IsNullOrEmpty(c.NormalizedName))
				.Select(c => String.Format("{0}.{1}", tableAlias, c.NormalizedName.ToSimpleIdentifier()))
				);
			
			builder.Append(columnList);
			builder.Append(" FROM (");

			ExecutionContext.SegmentsToReplace.Add(new TextSegment
			{
				IndextStart = queryBlock.RootNode.SourcePosition.IndexStart,
				Length = 0,
				Text = builder.ToString()
			});

			ExecutionContext.SegmentsToReplace.Add(new TextSegment
			{
				IndextStart = queryBlock.RootNode.SourcePosition.IndexEnd + 1,
				Length = 0,
				Text = ") " + tableAlias
			});
		}
	}
}
