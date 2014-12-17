using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SqlPad.Oracle
{
	[DebuggerDisplay("OracleQueryBlock (Alias={Alias}; Type={Type}; RootNode={RootNode}; Columns={Columns.Count})")]
	public class OracleQueryBlock : OracleReferenceContainer
	{
		private OracleDataObjectReference _selfObjectReference;
		private bool? _hasRemoteAsteriskReferences;
		private readonly List<OracleSelectListColumn> _columns = new List<OracleSelectListColumn>();
		private readonly List<OracleSelectListColumn> _asteriskColumns = new List<OracleSelectListColumn>();

		public OracleQueryBlock(OracleStatementSemanticModel semanticModel) : base(semanticModel)
		{
			AccessibleQueryBlocks = new List<OracleQueryBlock>();
		}

		public OracleDataObjectReference SelfObjectReference
		{
			get { return _selfObjectReference ?? BuildSelfObjectReference(); }
		}

		public bool HasRemoteAsteriskReferences
		{
			get { return _hasRemoteAsteriskReferences ?? (_hasRemoteAsteriskReferences = HasRemoteAsteriskReferencesInternal(this)).Value; }
		}

		private OracleDataObjectReference BuildSelfObjectReference()
		{
			_selfObjectReference = new OracleDataObjectReference(ReferenceType.InlineView)
			                       {
									   AliasNode = AliasNode,
									   Owner = this
			                       };

			_selfObjectReference.QueryBlocks.Add(this);

			return _selfObjectReference;
		}

		public string Alias { get { return AliasNode == null ? null : AliasNode.Token.Value; } }

		public string NormalizedAlias { get { return Alias.ToQuotedIdentifier(); } }

		public StatementGrammarNode AliasNode { get; set; }

		public QueryBlockType Type { get; set; }
		
		public StatementGrammarNode RootNode { get; set; }

		public StatementGrammarNode WhereClause { get; set; }

		public StatementGrammarNode SelectList { get; set; }

		public bool HasDistinctResultSet { get; set; }

		public StatementGrammarNode FromClause { get; set; }

		public StatementGrammarNode GroupByClause { get; set; }

		public StatementGrammarNode HavingClause { get; set; }
		
		public StatementGrammarNode OrderByClause { get; set; }
		
		public StatementGrammarNode HierarchicalQueryClause { get; set; }

		public StatementGrammarNode ExplicitColumnNameList { get; set; }

		public OracleStatement Statement { get; set; }

		public IReadOnlyList<OracleSelectListColumn> Columns { get { return _columns; } }
		
		public IReadOnlyCollection<OracleSelectListColumn> AsteriskColumns { get { return _asteriskColumns; } }

		public OracleSqlModelReference ModelReference { get; set; }

		public IEnumerable<OracleReferenceContainer> ChildContainers
		{
			get
			{
				var containers = (IEnumerable<OracleReferenceContainer>)Columns;
				if (ModelReference != null)
				{
					containers = containers.Concat(ModelReference.ChildContainers);
				}

				return containers;
			}
		}

		public IEnumerable<OracleProgramReference> AllProgramReferences { get { return Columns.SelectMany(c => c.ProgramReferences).Concat(ProgramReferences); } }

		public IEnumerable<OracleColumnReference> AllColumnReferences { get { return Columns.SelectMany(c => c.ColumnReferences).Concat(ColumnReferences); } }

		public IEnumerable<OracleTypeReference> AllTypeReferences { get { return Columns.SelectMany(c => c.TypeReferences).Concat(TypeReferences); } }

		public IEnumerable<OracleSequenceReference> AllSequenceReferences { get { return Columns.SelectMany(c => c.SequenceReferences).Concat(SequenceReferences); } }

		public ICollection<OracleQueryBlock> AccessibleQueryBlocks { get; private set; }

		public OracleQueryBlock FollowingConcatenatedQueryBlock { get; set; }
		
		public OracleQueryBlock PrecedingConcatenatedQueryBlock { get; set; }

		public OracleQueryBlock ParentCorrelatedQueryBlock { get; set; }

		public IEnumerable<OracleQueryBlock> AllPrecedingConcatenatedQueryBlocks
		{
			get { return GetAllConcatenatedQueryBlocks(qb => qb.PrecedingConcatenatedQueryBlock); }
		}
		
		public IEnumerable<OracleQueryBlock> AllFollowingConcatenatedQueryBlocks
		{
			get { return GetAllConcatenatedQueryBlocks(qb => qb.FollowingConcatenatedQueryBlock); }
		}

		private IEnumerable<OracleQueryBlock> GetAllConcatenatedQueryBlocks(Func<OracleQueryBlock, OracleQueryBlock> getConcatenatedQueryBlockFunction)
		{
			var concatenatedQueryBlock = getConcatenatedQueryBlockFunction(this);
			while (concatenatedQueryBlock != null)
			{
				yield return concatenatedQueryBlock;

				concatenatedQueryBlock = getConcatenatedQueryBlockFunction(concatenatedQueryBlock);
			}
		}

		public IEnumerable<OracleReference> DatabaseLinkReferences
		{
			get
			{
				var programReferences = (IEnumerable<OracleReference>)AllProgramReferences.Where(p => p.DatabaseLinkNode != null);
				var sequenceReferences = AllSequenceReferences.Where(p => p.DatabaseLinkNode != null);
				var columnReferences = AllColumnReferences.Where(p => p.DatabaseLinkNode != null);
				var objectReferences = ObjectReferences.Where(p => p.DatabaseLinkNode != null);
				return programReferences.Concat(sequenceReferences).Concat(columnReferences).Concat(objectReferences);
			}
		}

		public void AddSelectListColumn(OracleSelectListColumn column, int? index = null)
		{
			if (index.HasValue)
			{
				_columns.Insert(index.Value, column);
			}
			else
			{
				_columns.Add(column);
			}

			if (column.IsAsterisk)
			{
				_asteriskColumns.Add(column);
			}
		}

		public int IndexOf(OracleSelectListColumn column)
		{
			return _columns.IndexOf(column);
		}

		private static bool HasRemoteAsteriskReferencesInternal(OracleQueryBlock queryBlock)
		{
			var hasRemoteAsteriskReferences = queryBlock._asteriskColumns.Any(c => (c.RootNode.TerminalCount == 1 && queryBlock.ObjectReferences.Any(r => r.DatabaseLinkNode != null)) || (c.ColumnReferences.Any(r => r.ValidObjectReference != null && r.ValidObjectReference.DatabaseLinkNode != null)));

			OracleQueryBlock childQueryBlock;
			return hasRemoteAsteriskReferences ||
				   queryBlock.ObjectReferences.Any(o => o.QueryBlocks.Count == 1 && (childQueryBlock = o.QueryBlocks.First()) != queryBlock && HasRemoteAsteriskReferencesInternal(childQueryBlock));
		}
	}

	public struct OracleLiteral
	{
		public LiteralType Type;
		
		public StatementGrammarNode Terminal;
	}

	public enum LiteralType
	{
		Date,
		Timestamp
	}
}
