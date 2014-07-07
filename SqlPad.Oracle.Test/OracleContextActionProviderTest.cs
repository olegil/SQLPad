﻿using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using SqlPad.Oracle.Commands;

namespace SqlPad.Oracle.Test
{
	[TestFixture]
	public class OracleContextActionProviderTest
	{
		private readonly OracleContextActionProvider _actionProvider = new OracleContextActionProvider();

		[Test(Description = @""), STAThread]
		public void TestSuggestingAmbiguousColumnReferenceResolutionAtTheNameBeginning()
		{
			const string query1 = @"SELECT DUMMY FROM (SELECT DUMMY FROM DUAL) t2, Dual";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 7).Where(a => a.Name.StartsWith("Resolve as")).ToArray();
			actions.Length.ShouldBe(2);
			actions[0].Name.ShouldBe("Resolve as t2.DUMMY");
			actions[1].Name.ShouldBe("Resolve as Dual.DUMMY");
		}

		[Test(Description = @""), STAThread]
		public void TestSuggestingAmbiguousColumnReferenceResolutionAtTheNameEnd()
		{
			const string query1 = @"SELECT DUMMY FROM (SELECT DUMMY FROM DUAL) t2, Dual";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 12).Where(a => a.Name.StartsWith("Resolve as")).ToArray();
			actions.Length.ShouldBe(2);
			actions[0].Name.ShouldBe("Resolve as t2.DUMMY");
			actions[1].Name.ShouldBe("Resolve as Dual.DUMMY");
		}

		[Test(Description = @""), STAThread]
		public void TestSuggestingAmbiguousColumnReferenceResolutionInWhereClause()
		{
			const string query1 = @"SELECT * FROM SELECTION, PROJECT WHERE NAME = 'Name'";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 39).Where(a => a.Name.StartsWith("Resolve as")).ToArray();
			actions.Length.ShouldBe(2);
			actions[0].Name.ShouldBe("Resolve as SELECTION.NAME");
			actions[1].Name.ShouldBe("Resolve as PROJECT.NAME");
		}

		[Test(Description = @""), STAThread]
		public void TestSuggestingAmbiguousColumnReferenceResolutionWithFullyQualifiedName()
		{
			const string query1 = @"SELECT DUAL.DUMMY FROM SYS.DUAL, ""PUBLIC"".DUAL";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 12).Where(a => a.Name.StartsWith("Resolve as")).ToArray();
			actions.Length.ShouldBe(2);
			actions[0].Name.ShouldBe("Resolve as SYS.DUAL.DUMMY");
			actions[1].Name.ShouldBe("Resolve as \"PUBLIC\".DUAL.DUMMY");
		}

		[Test(Description = @""), STAThread]
		public void TestSuggestingAddTableAlias()
		{
			const string query1 = @"SELECT DUMMY FROM (SELECT DUMMY FROM DUAL) t2, Dual";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 49).SingleOrDefault(a => a.Name == AddAliasCommand.Title);
			action.ShouldNotBe(null);
			action.Name.ShouldBe("Add Alias");
		}

		[Test(Description = @""), STAThread]
		public void TestAliasNotSuggestedAtNestedTableAlias()
		{
			const string query1 = @"SELECT DUMMY FROM (SELECT DUMMY FROM DUAL) t2, Dual";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 44).ToArray();
			actions.Length.ShouldBe(0);
		}

		[Test(Description = @""), STAThread]
		public void TestResolveColumnIsNotsuggestedWhenTableIsNotAliased()
		{
			const string query1 = @"SELECT DUMMY FROM (SELECT 1 DUMMY FROM DUAL), SYS.DUAL";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 7).Where(a => a.Name.StartsWith("Resolve as")).ToArray();
			actions.Length.ShouldBe(1);
			actions[0].Name.ShouldBe("Resolve as SYS.DUAL.DUMMY");
		}

		[Test(Description = @""), STAThread]
		public void TestResolveColumnIsNotsuggestedWhenTableAliasIsSameAsPhysicalTableName()
		{
			const string query1 = @"SELECT DUAL.DUMMY FROM (SELECT 1 DUMMY FROM DUAL) DUAL, SYS.DUAL";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 14).Where(a => a.Name.StartsWith("Resolve as")).ToArray();
			actions.Length.ShouldBe(1);
			actions[0].Name.ShouldBe("Resolve as SYS.DUAL.DUMMY");
		}

		[Test(Description = @""), STAThread]
		public void TestWrapCommandsNotAvailableWhenQueryBlockHasNoNamedColumn()
		{
			const string query1 = @"SELECT NULL FROM DUAL";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 1)
				.Count(a => a.Name.In(WrapAsCommonTableExpressionCommand.Title, WrapAsInlineViewCommand.Title));
			
			actions.ShouldBe(0);
		}

		[Test(Description = @""), STAThread]
		public void TestUnnestCommandSuggestion()
		{
			const string query1 = @"SELECT IV.TEST_COLUMN || ' ADDED' FROM (SELECT SELECTION.NAME || ' FROM CTE ' TEST_COLUMN FROM SELECTION) IV";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 41).SingleOrDefault(a => a.Name == UnnestInlineViewCommand.Title);
			action.ShouldNotBe(null);
			action.Name.ShouldBe("Unnest");
		}

		[Test(Description = @""), STAThread]
		public void TestUnnestCommandIsNotSuggestedWhenInlineViewContainsGroupByClause()
		{
			const string query1 = @"SELECT * FROM (SELECT NAME FROM SELECTION GROUP BY NAME)";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 18).Count(a => a.Name == UnnestInlineViewCommand.Title);
			actions.ShouldBe(0);
		}
		
		[Test(Description = @""), STAThread]
		public void TestUnnestCommandIsNotSuggestedWhenInlineViewContainsDistinctClause()
		{
			const string query1 = @"SELECT * FROM (SELECT DISTINCT NAME FROM SELECTION)";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 18).Count(a => a.Name == UnnestInlineViewCommand.Title);
			actions.ShouldBe(0);
		}

		[Test(Description = @""), STAThread]
		public void TestUnnestCommandNotAvailableAtObjectAliasWhichIsNotCommonTableExpressionAlias()
		{
			const string query1 = @"SELECT 1 FROM DUAL ALIAS";

			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 20).ToArray();
			actions.Length.ShouldBe(0);
		}

		[Test(Description = @""), STAThread]
		public void TestExpandAsteriskCommandNotAvailableWithSourceWithoutNamedColumn()
		{
			const string query1 = "SELECT * FROM (SELECT 1 FROM SELECTION)";
			var actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 7).ToArray();
			actions.Length.ShouldBe(0);

			const string query2 = "SELECT S.* FROM (SELECT 1 FROM SELECTION) S";
			actions = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query2, 9).ToArray();
			actions.Length.ShouldBe(0);
		}

		[Test(Description = @""), STAThread]
		public void TestToggleFullyQualifiedReferencesSuggested()
		{
			const string query1 = @"SELECT DUMMY FROM DUAL";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 0).SingleOrDefault(a => a.Name == ToggleFullyQualifiedReferencesCommand.Title);
			action.ShouldNotBe(null);
			action.Name.ShouldBe("Toggle fully qualified references");
		}

		[Test(Description = @""), STAThread]
		public void TestAddMissingColumnCommandSuggestion()
		{
			const string query1 = @"SELECT NOT_EXISTING_COLUMN FROM SELECTION";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 7).SingleOrDefault(a => a.Name == AddMissingColumnCommand.Title);
			action.ShouldNotBe(null);
			action.Name.ShouldBe("Add missing column");
		}

		[Test(Description = @""), STAThread]
		public void TestAddMissingColumnCommandNotSuggestedWhenAlreadyExists()
		{
			const string query1 = @"SELECT DUMMY FROM DUAL";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 7).SingleOrDefault(a => a.Name == AddMissingColumnCommand.Title);
			action.ShouldBe(null);
		}

		[Test(Description = @""), STAThread]
		public void TestToggleQuotedNotationNotSuggestedWhenNotConvertibleIdentifierOrAliasAvailable()
		{
			const string query1 = @"SELECT ""Balance"" FROM ""Accounts""";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 0).SingleOrDefault(a => a.Name == ToggleQuotedNotationCommand.Title);
			action.ShouldBe(null);
		}

		[Test(Description = @""), STAThread]
		public void TestAddColumnAliasSuggestion()
		{
			const string query1 = @"SELECT DUMMY FROM DUAL";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 10).SingleOrDefault(a => a.Name == AddAliasCommand.Title);
			action.ShouldNotBe(null);
		}

		[Test(Description = @""), STAThread]
		public void TestAddColumnAliasNotSuggestedWhenAliasExists()
		{
			const string query1 = @"SELECT DUMMY NOT_DUMMY FROM DUAL";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 10).SingleOrDefault(a => a.Name == AddAliasCommand.Title);
			action.ShouldBe(null);
		}

		[Test(Description = @""), STAThread]
		public void TestAddColumnAliasNotSuggestedWhenNotDirectReference()
		{
			const string query1 = @"SELECT DUMMY + 1 FROM DUAL";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 10).SingleOrDefault(a => a.Name == AddAliasCommand.Title);
			action.ShouldBe(null);
		}

		[Test(Description = @""), STAThread]
		public void TestCreateScriptSuggestion()
		{
			const string query1 = @"SELECT S.* FROM SELECTION S";

			var action = _actionProvider.GetContextActions(TestFixture.DatabaseModel, query1, 7).SingleOrDefault(a => a.Name == CreateScriptCommand.Title);
			action.ShouldNotBe(null);
		}
	}
}
