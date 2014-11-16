using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SqlPad.Oracle
{
	[DebuggerDisplay("OracleProgramReference (Owner={OwnerNode == null ? null : OwnerNode.Token.Value}; Object={ObjectNode == null ? null : ObjectNode.Token.Value}; Function={FunctionIdentifierNode.Token.Value})")]
	public class OracleProgramReference : OracleProgramReferenceBase
	{
		public override string Name { get { return FunctionIdentifierNode.Token.Value; } }

		public StatementGrammarNode FunctionIdentifierNode { get; set; }
		
		public StatementGrammarNode AnalyticClauseNode { get; set; }
		
		public override OracleFunctionMetadata Metadata { get; set; }
	}

	[DebuggerDisplay("OracleTypeReference (Owner={OwnerNode == null ? null : OwnerNode.Token.Value}; Type={ObjectNode.Token.Value})")]
	public class OracleTypeReference : OracleProgramReferenceBase
	{
		public override string Name { get { return ObjectNode.Token.Value; } }

		public override OracleFunctionMetadata Metadata
		{
			get { return ((OracleTypeBase)SchemaObject.GetTargetSchemaObject()).GetConstructorMetadata(); }
			set { throw new NotSupportedException("Metadata cannot be set. It is inferred from type attributes"); }
		}
	}

	[DebuggerDisplay("OracleSequenceReference (Owner={OwnerNode == null ? null : OwnerNode.Token.Value}; Sequence={ObjectNode.Token.Value})")]
	public class OracleSequenceReference : OracleObjectWithColumnsReference
	{
		public override string Name { get { return ObjectNode.Token.Value; } }

		public override ICollection<OracleColumn> Columns
		{
			get { return ((OracleSequence)SchemaObject).Columns; }
		}

		public override ReferenceType Type
		{
			get { return ReferenceType.SchemaObject; }
		}
	}

	[DebuggerDisplay("OracleTableCollectionReference (Owner={OwnerNode == null ? null : OwnerNode.Token.Value}; FunctionIdentifier={ObjectNode.Token.Value})")]
	public class OracleTableCollectionReference : OracleDataObjectReference
	{
		private List<OracleColumn> _columns;

		public OracleTableCollectionReference() : base(ReferenceType.TableCollection)
		{
		}

		public OracleFunctionMetadata FunctionMetadata { get; set; }

		public override string Name { get { return AliasNode == null ? null : AliasNode.Token.Value; } }

		public override OracleObjectIdentifier FullyQualifiedObjectName
		{
			get { return OracleObjectIdentifier.Empty; }
		}

		public override ICollection<OracleColumn> Columns
		{
			get { return _columns ?? BuildColumns(); }
		}

		private ICollection<OracleColumn> BuildColumns()
		{
			_columns = new List<OracleColumn>();

			var schemaObject = SchemaObject.GetTargetSchemaObject();
			var collectionType = schemaObject as OracleTypeCollection;
			if (collectionType != null)
			{
				var column =
					new OracleColumn
					{
						Name = "\"COLUMN_VALUE\"",
						DataType = collectionType.ElementDataType,
						Nullable = true
					};

				_columns.Add(column);
			}
			else if (FunctionMetadata != null)
			{
				//FunctionMetadata.Parameters
			}

			return _columns;
		}
	}

	public abstract class OracleProgramReferenceBase : OracleReference
	{
		public StatementGrammarNode ParameterListNode { get; set; }

		public IList<StatementGrammarNode> ParameterNodes { get; set; }

		public abstract OracleFunctionMetadata Metadata { get; set; }
	}
}
