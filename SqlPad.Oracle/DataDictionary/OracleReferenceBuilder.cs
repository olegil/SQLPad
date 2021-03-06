﻿using System;
using System.Collections.Generic;
using System.Linq;
using SqlPad.Oracle.DatabaseConnection;
using SqlPad.Oracle.SemanticModel;
using NonTerminals = SqlPad.Oracle.OracleGrammarDescription.NonTerminals;
using Terminals = SqlPad.Oracle.OracleGrammarDescription.Terminals;
using TerminalValues = SqlPad.Oracle.OracleGrammarDescription.TerminalValues;

namespace SqlPad.Oracle.DataDictionary
{
	internal class OracleReferenceBuilder
	{
		public static bool TryCreatePlSqlDataTypeReference(OraclePlSqlProgram program, StatementGrammarNode dataTypeNode, out OracleDataTypeReference dataTypeReference)
		{
			StatementGrammarNode ownerNode = null;
			StatementGrammarNode typeIdentifier = null;

			var firstChild = dataTypeNode[0];
			switch (firstChild?.Id)
			{
				case NonTerminals.AssignmentStatementTarget:
					var plSqlAssignmentTarget = firstChild[NonTerminals.PlSqlAssignmentTarget];
					var percentCharacterTypeOrRowTypeNotFound = dataTypeNode.ChildNodes.Count == 1;
					if (percentCharacterTypeOrRowTypeNotFound && plSqlAssignmentTarget != null)
					{
						var chainedIdentifiers = GatherChainedIdentifiers(plSqlAssignmentTarget).ToList();
						if (chainedIdentifiers.Count <= 2)
						{
							typeIdentifier = chainedIdentifiers.LastOrDefault();
							ownerNode = chainedIdentifiers.FirstOrDefault(i => i != typeIdentifier);
						}
					}

					break;

				case NonTerminals.BuiltInDataType:
					typeIdentifier = firstChild.FirstTerminalNode;
					break;

				default:
					if (String.Equals(dataTypeNode.Id, NonTerminals.AssociativeArrayIndexType))
					{
						typeIdentifier = dataTypeNode.FirstTerminalNode;
					}

					break;
			}

			dataTypeReference = null;

			if (typeIdentifier == null)
			{
				return false;
			}

			dataTypeReference =
				new OracleDataTypeReference
				{
					RootNode = dataTypeNode,
					Container = program,
					OwnerNode = ownerNode,
					ObjectNode = typeIdentifier
				};

			ResolveTypeMetadata(dataTypeReference);

			program.DataTypeReferences.Add(dataTypeReference);
			return true;
		}

		private static IEnumerable<StatementGrammarNode> GatherChainedIdentifiers(StatementGrammarNode plSqlAssignmentTargetNode)
		{
			var sourceNode = plSqlAssignmentTargetNode;

			while (sourceNode != null)
			{
				var identifier = sourceNode[Terminals.PlSqlIdentifier];
				if (identifier == null)
				{
					break;
				}

				yield return identifier;

				sourceNode = sourceNode[NonTerminals.DotRecordAttributeChained];
			}
		}

		public static OracleDataTypeReference CreateDataTypeReference(OracleQueryBlock queryBlock, OracleSelectListColumn selectListColumn, StatementPlacement placement, StatementGrammarNode typeIdentifier)
		{
			var dataTypeNode = typeIdentifier.ParentNode.ParentNode;
			var dataTypeReference =
				new OracleDataTypeReference
				{
					RootNode = dataTypeNode,
					Owner = queryBlock,
					Container = queryBlock,
					OwnerNode = dataTypeNode[NonTerminals.SchemaDatatype, NonTerminals.SchemaPrefix, Terminals.SchemaIdentifier],
					ObjectNode = typeIdentifier,
					DatabaseLinkNode = String.Equals(typeIdentifier.Id, Terminals.DataTypeIdentifier) ? GetDatabaseLinkFromIdentifier(typeIdentifier) : null,
					Placement = placement,
					SelectListColumn = selectListColumn
				};

			ResolveTypeMetadata(dataTypeReference);

			ResolveSchemaType(dataTypeReference);

			return dataTypeReference;
		}

		public static void ResolveSchemaType(OracleDataTypeReference dataTypeReference)
		{
			var semanticModel = dataTypeReference.Container.SemanticModel;
			if (dataTypeReference.DatabaseLinkNode == null && semanticModel.HasDatabaseModel)
			{
				dataTypeReference.SchemaObject = semanticModel.DatabaseModel.GetFirstSchemaObject<OracleTypeBase>(semanticModel.DatabaseModel.GetPotentialSchemaObjectIdentifiers(dataTypeReference.FullyQualifiedObjectName));
			}
		}

		public static StatementGrammarNode GetDatabaseLinkFromIdentifier(StatementGrammarNode identifier)
		{
			return GetDatabaseLinkFromNode(identifier.ParentNode);
		}

		public static StatementGrammarNode GetDatabaseLinkFromNode(StatementGrammarNode node)
		{
			return node[NonTerminals.DatabaseLink, NonTerminals.DatabaseLinkName];
		}

		public static OracleDataType ResolveDataTypeFromNode(StatementGrammarNode dataType)
		{
			var dataTypeReference = new OracleDataTypeReference { RootNode = dataType };
			ResolveTypeMetadata(dataTypeReference);
			return dataTypeReference.ResolvedDataType;
		}

		public static OracleDataType ResolveDataTypeFromJsonDataTypeNode(StatementGrammarNode jsonDataTypeNode)
		{
			if (jsonDataTypeNode == null)
			{
				return OracleDataType.Empty;
			}

			if (!String.Equals(jsonDataTypeNode.Id, NonTerminals.JsonDataType))
			{
				throw new ArgumentException($"Node ID must be '{NonTerminals.JsonDataType}'. ", nameof(jsonDataTypeNode));
			}

			var dataTypeNode = jsonDataTypeNode[NonTerminals.DataType];
			if (dataTypeNode != null)
			{
				return ResolveDataTypeFromNode(dataTypeNode);
			}

			var typeTerminal = jsonDataTypeNode[0];
			return
				typeTerminal == null
					? OracleDataType.Empty
					: new OracleDataType { FullyQualifiedName = OracleObjectIdentifier.Create(null, ((OracleToken)typeTerminal.Token).UpperInvariantValue) };
		}

		private static void ResolveTypeMetadata(OracleDataTypeReference dataTypeReference)
		{
			var dataTypeNode = dataTypeReference.RootNode;

			var isSqlDataType = String.Equals(dataTypeNode.Id, NonTerminals.DataType);
			var isAssociativeArrayType = String.Equals(dataTypeNode.Id, NonTerminals.AssociativeArrayIndexType);
			if (!isSqlDataType &&
				!String.Equals(dataTypeNode.Id, NonTerminals.PlSqlDataType) &&
				!String.Equals(dataTypeNode.Id, NonTerminals.PlSqlDataTypeWithoutConstraint) &&
				!isAssociativeArrayType)
			{
				throw new ArgumentException($"RootNode ID must be '{NonTerminals.DataType}' or '{NonTerminals.PlSqlDataType}' but is {dataTypeNode.Id}. ", nameof(dataTypeReference));
			}

			var owner = String.Equals(dataTypeNode.FirstTerminalNode.Id, Terminals.SchemaIdentifier)
				? dataTypeNode.FirstTerminalNode.Token.Value
				: String.Empty;

			var dataType = dataTypeReference.ResolvedDataType = new OracleDataType();

			var builtInDataTypeNode = isAssociativeArrayType
				? dataTypeNode
				: dataTypeNode[NonTerminals.BuiltInDataType];

			string name;
			if (builtInDataTypeNode != null)
			{
				var isVarying = builtInDataTypeNode[Terminals.Varying] != null;

				switch (builtInDataTypeNode.FirstTerminalNode.Id)
				{
					case Terminals.Double:
						name = TerminalValues.BinaryDouble;
						break;
					case Terminals.Long:
						name = builtInDataTypeNode.ChildNodes.Count > 1 && String.Equals(builtInDataTypeNode.ChildNodes[1].Id, Terminals.Raw)
							? "LONG RAW"
							: TerminalValues.Long;
						break;
					case Terminals.Interval:
						var yearToMonthNode = builtInDataTypeNode[NonTerminals.YearToMonthOrDayToSecond, NonTerminals.IntervalYearToMonth];
						if (yearToMonthNode == null)
						{
							var dayToSecondNode = builtInDataTypeNode[NonTerminals.YearToMonthOrDayToSecond, NonTerminals.IntervalDayToSecond];
							name = dayToSecondNode == null ? String.Empty : OracleDatabaseModelBase.BuiltInDataTypeIntervalDayToSecond;
						}
						else
						{
							name = OracleDatabaseModelBase.BuiltInDataTypeIntervalYearToMonth;
						}

						break;
					case Terminals.National:
						name = isVarying ? TerminalValues.NVarchar2 : TerminalValues.NChar;
						break;
					case Terminals.Character:
						name = isVarying ? TerminalValues.Varchar2 : TerminalValues.Char;
						break;
					default:
						name = ((OracleToken)builtInDataTypeNode.FirstTerminalNode.Token).UpperInvariantValue;
						break;
				}

				StatementGrammarNode precisionNode;
				if (String.Equals(name, OracleDatabaseModelBase.BuiltInDataTypeIntervalDayToSecond) ||
				    String.Equals(name, OracleDatabaseModelBase.BuiltInDataTypeIntervalYearToMonth))
				{
					var intervalPrecisions = builtInDataTypeNode.GetDescendants(NonTerminals.DataTypeSimplePrecision).ToArray();
					if (intervalPrecisions.Length > 0)
					{
						dataType.Precision = GetSimplePrecisionValue(intervalPrecisions[0], out precisionNode);
						dataTypeReference.PrecisionNode = precisionNode;

						if (intervalPrecisions.Length == 2)
						{
							dataType.Scale = GetSimplePrecisionValue(intervalPrecisions[1], out precisionNode);
							dataTypeReference.ScaleNode = precisionNode;
						}
					}
				}
				else
				{
					var simplePrecisionNode = builtInDataTypeNode.GetSingleDescendant(NonTerminals.DataTypeSimplePrecision);
					var precisionValue = GetSimplePrecisionValue(simplePrecisionNode, out precisionNode);

					switch (name)
					{
						case TerminalValues.Float:
						case TerminalValues.Timestamp:
							dataType.Precision = precisionValue;
							dataTypeReference.PrecisionNode = precisionNode;
							break;
						default:
							dataType.Length = precisionValue;
							dataTypeReference.LengthNode = precisionNode;
							break;
					}

					TryResolveVarcharDetails(dataTypeReference, builtInDataTypeNode);

					TryResolveNumericPrecisionAndScale(dataTypeReference, builtInDataTypeNode);
				}
			}
			else if (!isSqlDataType)
			{
				name = ((OracleToken)dataTypeNode.LastTerminalNode.Token).UpperInvariantValue;
			}
			else
			{
				var identifier = dataTypeNode[NonTerminals.SchemaDatatype, Terminals.DataTypeIdentifier];
				name = identifier == null ? String.Empty : ((OracleToken)identifier.Token).UpperInvariantValue;
			}

			dataType.FullyQualifiedName = OracleObjectIdentifier.Create(owner, name);

			dataTypeReference.ResolvedDataType = dataType;
		}

		private static int? GetSimplePrecisionValue(StatementGrammarNode simplePrecisionNode, out StatementGrammarNode node)
		{
			if (simplePrecisionNode == null)
			{
				node = null;
				return null;
			}

			node = simplePrecisionNode[Terminals.IntegerLiteral];
			return node == null
				? (int?)null
				: Convert.ToInt32(node.Token.Value);
		}

		private static void TryResolveNumericPrecisionAndScale(OracleDataTypeReference dataTypeReference, StatementGrammarNode definitionNode)
		{
			var numericPrecisionScaleNode = definitionNode[NonTerminals.DataTypeNumericPrecisionAndScale];
		    var precisionValueTerminal = numericPrecisionScaleNode?[NonTerminals.IntegerOrAsterisk, Terminals.IntegerLiteral];
			if (precisionValueTerminal == null)
			{
				return;
			}

			dataTypeReference.ResolvedDataType.Precision = Convert.ToInt32(precisionValueTerminal.Token.Value);
			dataTypeReference.PrecisionNode = precisionValueTerminal;

			var negativeIntegerNonTerminal = numericPrecisionScaleNode[NonTerminals.Scale, NonTerminals.NegativeInteger];
			if (negativeIntegerNonTerminal == null)
			{
				return;
			}

			dataTypeReference.ResolvedDataType.Scale = Convert.ToInt32(negativeIntegerNonTerminal.LastTerminalNode.Token.Value);
			dataTypeReference.ScaleNode = negativeIntegerNonTerminal;

			if (String.Equals(negativeIntegerNonTerminal.FirstTerminalNode.Id, Terminals.MathMinus))
			{
				dataTypeReference.ResolvedDataType.Scale = -dataTypeReference.ResolvedDataType.Scale;
			}
		}

		private static void TryResolveVarcharDetails(OracleDataTypeReference dataTypeReference, StatementGrammarNode definitionNode)
		{
			var varyingCharacterSimplePrecisionNode = definitionNode.GetSingleDescendant(NonTerminals.DataTypeVarcharSimplePrecision);
		    var valueTerminal = varyingCharacterSimplePrecisionNode?[Terminals.IntegerLiteral];
			if (valueTerminal == null)
			{
				return;
			}

			dataTypeReference.ResolvedDataType.Length = Convert.ToInt32(valueTerminal.Token.Value);
			dataTypeReference.LengthNode = valueTerminal;

			var byteOrCharNode = varyingCharacterSimplePrecisionNode[NonTerminals.ByteOrChar];
			if (byteOrCharNode != null)
			{
				dataTypeReference.ResolvedDataType.Unit = byteOrCharNode.FirstTerminalNode.Id == Terminals.Byte ? DataUnit.Byte : DataUnit.Character;
			}
		}
	}
}
