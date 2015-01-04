using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SqlPad
{
	public abstract class StatementBase
	{
		private IReadOnlyList<StatementGrammarNode> _allTerminals;
		private ICollection<StatementGrammarNode> _invalidGrammarNodes;

		public ParseStatus ParseStatus { get; set; }

		public StatementGrammarNode RootNode { get; set; }

		public StatementGrammarNode TerminatorNode { get; set; }

		public IEnumerable<StatementCommentNode> Comments
		{
			get
			{
				if (RootNode == null)
				{
					return Enumerable.Empty<StatementCommentNode>();
				}

				return RootNode.AllChildNodes
					.Where(n => n.Type == NodeType.NonTerminal)
					.SelectMany(n => n.Comments);
			}
		}

		public SourcePosition SourcePosition { get; set; }

		public abstract ICollection<BindVariableConfiguration> BindVariables { get; }

		public ICollection<StatementGrammarNode> InvalidGrammarNodes
		{
			get { return _invalidGrammarNodes ?? (_invalidGrammarNodes = BuildInvalidGrammarNodeCollection()); }
		}

		private ICollection<StatementGrammarNode> BuildInvalidGrammarNodeCollection()
		{
			return RootNode == null
				? new StatementGrammarNode[0]
				: GetInvalidGrammerNodes(RootNode).ToArray();
		}

		private static IEnumerable<StatementGrammarNode> GetInvalidGrammerNodes(StatementGrammarNode node)
		{
			foreach (var childNode in node.ChildNodes)
			{
				if (childNode.IsGrammarValid)
				{
					var nestedNodes = GetInvalidGrammerNodes(childNode).Where(n => n.LastTerminalNode != null);
					foreach (var nestedChildMode in nestedNodes)
					{
						yield return nestedChildMode.LastTerminalNode.ParentNode;
					}
				}
				else if (childNode.LastTerminalNode != null)
				{
					yield return childNode.LastTerminalNode.ParentNode;
				}
			}
		}

		public IReadOnlyList<StatementGrammarNode> AllTerminals
		{
			get { return _allTerminals ?? (_allTerminals = BuildTerminalCollection()); }
		}
		public StatementGrammarNode LastTerminalNode
		{
			get { return RootNode == null ? null : (TerminatorNode ?? RootNode.LastTerminalNode); }
		}

		private IReadOnlyList<StatementGrammarNode> BuildTerminalCollection()
		{
			var terminals = RootNode == null ? Enumerable.Empty<StatementGrammarNode>() : RootNode.Terminals;
			return terminals.ToArray();
		}

		public StatementGrammarNode GetNodeAtPosition(int position, Func<StatementGrammarNode, bool> filter = null)
		{
			return RootNode == null ? null : RootNode.GetNodeAtPosition(position, filter);
		}

		public StatementGrammarNode GetTerminalAtPosition(int position, Func<StatementGrammarNode, bool> filter = null)
		{
			var node = GetNodeAtPosition(position, filter);
			return node == null || node.Type == NodeType.NonTerminal ? null : node;
		}

		public StatementGrammarNode GetNearestTerminalToPosition(int position, Func<StatementGrammarNode, bool> filter = null)
		{
			return RootNode == null ? null : RootNode.GetNearestTerminalToPosition(position, filter);
		}
	}

	[DebuggerDisplay("FoldingSection (Placeholder={Placeholder}; Range={FoldingStart + \"-\" + FoldingEnd})")]
	public class FoldingSection
	{
		public int FoldingStart { get; set; }

		public int FoldingEnd { get; set; }
		
		public string Placeholder { get; set; }
		
		public bool IsNested { get; set; }
	}
}
