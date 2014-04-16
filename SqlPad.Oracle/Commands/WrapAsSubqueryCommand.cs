﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminals = SqlPad.Oracle.OracleGrammarDescription.Terminals;

namespace SqlPad.Oracle.Commands
{
	public class WrapAsSubqueryCommand : OracleConfigurableCommandBase
	{
		public WrapAsSubqueryCommand(OracleStatementSemanticModel semanticModel, StatementDescriptionNode currentTerminal, ICommandSettingsProvider settingsProvider = null)
			: base(semanticModel, currentTerminal, settingsProvider)
		{
		}

		public override bool CanExecute(object parameter)
		{
			if (CurrentTerminal.Id != Terminals.Select)
				return false;

			var queryBlock = SemanticModel.GetQueryBlock(CurrentTerminal);
			if (queryBlock == null)
				return false;

			//TODO: Check column aliases
			//var tables = .Columns.All(c => c.);
			return true;
		}

		protected override void ExecuteInternal(string statementText, ICollection<TextSegment> segmentsToReplace)
		{
			if (!SettingsProvider.GetSettings())
				return;

			var tableAlias = SettingsProvider.Settings.Value;

			var queryBlock = SemanticModel.GetQueryBlock(CurrentTerminal);

			var builder = new StringBuilder("SELECT ");
			var columnList = String.Join(", ", queryBlock.Columns.Where(c => !c.IsAsterisk && !String.IsNullOrEmpty(c.NormalizedName)).Select(c => c.NormalizedName.ToSimpleIdentifier()));
			builder.Append(columnList);
			builder.Append(" FROM (");

			segmentsToReplace.Add(new TextSegment
			{
				IndextStart = queryBlock.RootNode.SourcePosition.IndexStart,
				Length = 0,
				Text = builder.ToString()
			});

			segmentsToReplace.Add(new TextSegment
			{
				IndextStart = queryBlock.RootNode.SourcePosition.IndexEnd + 1,
				Length = 0,
				Text = ") " + tableAlias
			});
		}
	}
}