﻿using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using SqlPad.Oracle.ToolTips;
using SqlPad.Test;

namespace SqlPad.Oracle.Database.Test
{
	[TestFixture]
	public class OracleDatabaseModelTest : TemporaryDirectoryTestFixture
	{
		private readonly ConnectionStringSettings _connectionString = new ConnectionStringSettings("TestConnection", "DATA SOURCE=HQ_PDB_TCP;PASSWORD=oracle;USER ID=HUSQVIK");

		[Test]
		public void TestModelInitialization()
		{
			using (var databaseModel = OracleDatabaseModel.GetDatabaseModel(_connectionString))
			{
				databaseModel.Schemas.Count.ShouldBe(0);
				databaseModel.AllObjects.Count.ShouldBe(0);
				databaseModel.DatabaseLinks.Count.ShouldBe(0);
				databaseModel.CharacterSets.Count.ShouldBe(0);
				databaseModel.StatisticsKeys.Count.ShouldBe(0);
				databaseModel.SystemParameters.Count.ShouldBe(0);

				databaseModel.IsInitialized.ShouldBe(false);
				var resetEvent = new ManualResetEvent(false);
				databaseModel.RefreshFinished += (sender, args) => resetEvent.Set();
				databaseModel.Initialize();

				using (var modelClone = OracleDatabaseModel.GetDatabaseModel(_connectionString))
				{
					var cloneRefreshTask = modelClone.Refresh();
					cloneRefreshTask.Wait();

					resetEvent.WaitOne();
					databaseModel.IsInitialized.ShouldBe(true);
					databaseModel.Schemas.Count.ShouldBeGreaterThan(0);

					Trace.WriteLine("Assert original database model");
					AssertDatabaseModel(databaseModel);

					Trace.WriteLine("Assert cloned database model");
					AssertDatabaseModel(modelClone);
				}
			}
		}

		private void AssertDatabaseModel(OracleDatabaseModelBase databaseModel)
		{
			databaseModel.AllObjects.Count.ShouldBeGreaterThan(0);
			Trace.WriteLine(String.Format("All object dictionary has {0} members. ", databaseModel.AllObjects.Count));
			
			databaseModel.DatabaseLinks.Count.ShouldBeGreaterThan(0);
			Trace.WriteLine(String.Format("Database link dictionary has {0} members. ", databaseModel.DatabaseLinks.Count));

			databaseModel.CharacterSets.Count.ShouldBeGreaterThan(0);
			Trace.WriteLine(String.Format("Character set collection has {0} members. ", databaseModel.CharacterSets.Count));

			databaseModel.StatisticsKeys.Count.ShouldBeGreaterThan(0);
			Trace.WriteLine(String.Format("Statistics key dictionary has {0} members. ", databaseModel.StatisticsKeys.Count));

			databaseModel.SystemParameters.Count.ShouldBeGreaterThan(0);
			Trace.WriteLine(String.Format("System parameters dictionary has {0} members. ", databaseModel.SystemParameters.Count));

			var objectForScriptCreation = databaseModel.GetFirstSchemaObject<OracleSchemaObject>(databaseModel.GetPotentialSchemaObjectIdentifiers("SYS", "OBJ$"));
			objectForScriptCreation.ShouldNotBe(null);
			var scriptTask = databaseModel.GetObjectScriptAsync(objectForScriptCreation, CancellationToken.None);
			scriptTask.Wait();

			Trace.WriteLine("Object script output: " + Environment.NewLine + scriptTask.Result + Environment.NewLine);

			scriptTask.Result.ShouldNotBe(null);
			scriptTask.Result.Length.ShouldBeGreaterThan(100);

			var executionModel =
				new StatementExecutionModel
				{
					StatementText = "SELECT /*+ gather_plan_statistics */ * FROM DUAL WHERE DUMMY = :1",
					BindVariables = new[] { new BindVariableModel(new BindVariableConfiguration { Name = "1", Value = "X" }) },
					GatherExecutionStatistics = true
				};
			
			var result = databaseModel.ExecuteStatement(executionModel);
			result.ExecutedSuccessfully.ShouldBe(true);
			result.AffectedRowCount.ShouldBe(-1);
			
			databaseModel.CanFetch.ShouldBe(false);

			var columnHeaders = result.ColumnHeaders.ToArray();
			columnHeaders.Length.ShouldBe(1);
			columnHeaders[0].DataType.ShouldBe(typeof(string));
			columnHeaders[0].DatabaseDataType.ShouldBe("Varchar2");
			columnHeaders[0].Name.ShouldBe("DUMMY");
			columnHeaders[0].ValueConverter.ShouldNotBe(null);

			var rows = result.InitialResultSet.ToArray();
			rows.Length.ShouldBe(1);
			rows[0].Length.ShouldBe(1);
			rows[0][0].ShouldBe("X");

			databaseModel.FetchRecords(1).Any().ShouldBe(false);

			var displayCursorTask = databaseModel.GetActualExecutionPlanAsync(CancellationToken.None);
			displayCursorTask.Wait();

			displayCursorTask.Result.ShouldNotBe(null);

			Trace.WriteLine("Display cursor output: " + Environment.NewLine + displayCursorTask.Result + Environment.NewLine);

			displayCursorTask.Result.Length.ShouldBeGreaterThan(100);

			var task = databaseModel.GetExecutionStatisticsAsync(CancellationToken.None);
			task.Wait();

			var statisticsRecords = task.Result.Where(r => r.Value != 0).ToArray();
			statisticsRecords.Length.ShouldBeGreaterThan(0);

			var statistics = String.Join(Environment.NewLine, statisticsRecords.Select(r => String.Format("{0}: {1}", r.Name.PadRight(40), r.Value)));
			Trace.WriteLine("Execution statistics output: " + Environment.NewLine + statistics + Environment.NewLine);
		}

		[Test]
		public void TestDataTypesFetch()
		{
			var executionModel =
					new StatementExecutionModel
					{
						StatementText = "SELECT TO_BLOB(RAWTOHEX('BLOB')), TO_CLOB('CLOB DATA'), TO_NCLOB('NCLOB DATA'), DATA_DEFAULT, SYSTIMESTAMP, LOCALTIMESTAMP, 1.23, XMLTYPE('<root/>') FROM ALL_TAB_COLS WHERE OWNER = 'SYS' AND TABLE_NAME = 'DUAL'",
						BindVariables = new BindVariableModel[0],
						GatherExecutionStatistics = true
					};

			StatementExecutionResult result;
			using (var databaseModel = OracleDatabaseModel.GetDatabaseModel(_connectionString))
			{
				result = databaseModel.ExecuteStatement(executionModel);
			}

			result.ExecutedSuccessfully.ShouldBe(true);
			
			result.ColumnHeaders.Count.ShouldBe(8);
			result.ColumnHeaders[0].DatabaseDataType.ShouldBe("Blob");
			result.ColumnHeaders[1].DatabaseDataType.ShouldBe("Clob");
			result.ColumnHeaders[2].DatabaseDataType.ShouldBe("NClob");
			result.ColumnHeaders[3].DatabaseDataType.ShouldBe("Long");
			result.ColumnHeaders[4].DatabaseDataType.ShouldBe("TimeStampTZ");
			result.ColumnHeaders[5].DatabaseDataType.ShouldBe("TimeStamp");
			result.ColumnHeaders[6].DatabaseDataType.ShouldBe("Decimal");
			result.ColumnHeaders[7].DatabaseDataType.ShouldBe("XmlType");
			
			result.InitialResultSet.Count.ShouldBe(1);
			var firstRow = result.InitialResultSet[0];
			firstRow[0].ShouldBeTypeOf<OracleBlobValue>();
			firstRow[0].ToString().ShouldBe("(BLOB[4 B])");
			firstRow[1].ShouldBeTypeOf<OracleClobValue>();
			((OracleClobValue)firstRow[1]).DataTypeName.ShouldBe("CLOB");
			firstRow[2].ShouldBeTypeOf<OracleClobValue>();
			((OracleClobValue)firstRow[2]).DataTypeName.ShouldBe("NCLOB");
			firstRow[3].ShouldBeTypeOf<string>();
			firstRow[4].ShouldBeTypeOf<OracleTimestampWithTimeZone>();
			firstRow[5].ShouldBeTypeOf<OracleTimestamp>();
			firstRow[6].ShouldBeTypeOf<OracleNumber>();
			firstRow[7].ShouldBeTypeOf<OracleXmlValue>();
		}

		[Test]
		public void TestColumnDetailsModelUpdater()
		{
			var model = new ColumnDetailsModel();
			using (var databaseModel = OracleDatabaseModel.GetDatabaseModel(_connectionString))
			{
				databaseModel.UpdateColumnDetailsAsync(new OracleObjectIdentifier(OracleDatabaseModelBase.SchemaSys, "\"DUAL\""), "\"DUMMY\"", model, CancellationToken.None).Wait();
			}

			model.AverageValueSize.ShouldBe(2);
			model.DistinctValueCount.ShouldBe(1);
			model.HistogramBucketCount.ShouldBe(1);
			model.HistogramType.ShouldBe("None");
			model.LastAnalyzed.ShouldBeGreaterThan(DateTime.MinValue);
			model.NullValueCount.ShouldBe(0);
			model.SampleSize.ShouldBe(1);
		}

		[Test]
		public void TestTableDetailsModelUpdater()
		{
			var model = new TableDetailsModel();

			using (var databaseModel = OracleDatabaseModel.GetDatabaseModel(_connectionString))
			{
				databaseModel.UpdateTableDetailsAsync(new OracleObjectIdentifier(OracleDatabaseModelBase.SchemaSys, "\"DUAL\""), model, CancellationToken.None).Wait();
			}

			model.AverageRowSize.ShouldBe(2);
			model.BlockCount.ShouldBe(1);
			model.ClusterName.ShouldBe(null);
			model.Compression.ShouldBe("Disabled");
			model.IsPartitioned.ShouldBe(false);
			model.IsTemporary.ShouldBe(false);
			model.LastAnalyzed.ShouldBeGreaterThan(DateTime.MinValue);
			model.Organization.ShouldBe("Heap");
			model.ParallelDegree.ShouldBe("1");
			model.RowCount.ShouldBe(1);
		}

		[Test]
		public void TestTableSpaceAllocationModelUpdater()
		{
			var model = new TableDetailsModel();
			var tableSpaceAllocationUpdater = new TableSpaceAllocationModelUpdater(model, new OracleObjectIdentifier(OracleDatabaseModelBase.SchemaSys, "\"DUAL\""));

			ExecuteUpdater(tableSpaceAllocationUpdater);

			model.AllocatedBytes.ShouldBe(65536);
		}

		[Test]
		public void TestDisplayCursorUpdater()
		{
			var displayCursorUpdater = (DisplayCursorUpdater)DisplayCursorUpdater.CreateDisplayLastCursorUpdater();
			ExecuteUpdater(displayCursorUpdater);

			displayCursorUpdater.PlanText.ShouldNotBe(null);
			Trace.WriteLine(displayCursorUpdater.PlanText);
		}

		private void ExecuteUpdater(IDataModelUpdater columnDetailsUpdater)
		{
			using (var databaseModel = OracleDatabaseModel.GetDatabaseModel(_connectionString))
			{
				var task = (Task)typeof(OracleDatabaseModel).GetMethod("UpdateModelAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(databaseModel, new object[] { CancellationToken.None, false, new[] { columnDetailsUpdater } });
				task.Wait();
			}
		}
	}
}
