﻿using System;

namespace SqlPad.Oracle
{
	internal class OracleFunctionMatcher
	{
		private readonly FunctionMatchElement _ownerMatch;
		private readonly FunctionMatchElement _packageMatch;
		private readonly FunctionMatchElement _identifierMatch;

		public OracleFunctionMatcher(FunctionMatchElement ownerMatch, FunctionMatchElement packageMatch, FunctionMatchElement identifierMatch)
		{
			_identifierMatch = identifierMatch;
			_packageMatch = packageMatch;
			_ownerMatch = ownerMatch;
		}

		public bool IsMatch(OracleFunctionMetadata functionMetadata, string currentSchema)
		{
			var result = _ownerMatch == null || (String.IsNullOrEmpty(_ownerMatch.Value) && functionMetadata.Identifier.Owner == currentSchema.ToQuotedIdentifier()) ||
			             _ownerMatch.IsMatch(functionMetadata.Identifier);
			
			result &= _packageMatch == null || _packageMatch.IsMatch(functionMetadata.Identifier);
			return result && (_identifierMatch == null || _identifierMatch.IsMatch(functionMetadata.Identifier));
		}
	}

	internal class FunctionMatchElement
	{
		private static readonly Func<OracleFunctionIdentifier, string> OwnerSelector = identifier => identifier.Owner;
		private static readonly Func<OracleFunctionIdentifier, string> PackageSelector = identifier => identifier.Package;
		private static readonly Func<OracleFunctionIdentifier, string> NameSelector = identifier => identifier.Name;

		public FunctionMatchElement(string value)
		{
			Value = value;
		}

		public FunctionMatchElement SelectOwner()
		{
			Selector = OwnerSelector;
			return this;
		}

		public FunctionMatchElement SelectPackage()
		{
			Selector = PackageSelector;
			return this;
		}

		public FunctionMatchElement SelectName()
		{
			Selector = NameSelector;
			return this;
		}

		public string Value { get; private set; }

		public Func<OracleFunctionIdentifier, string> Selector { get; private set; }

		public bool AllowPartialMatch { get; set; }

		public bool AllowStartWithMatch { get; set; }

		public bool DenyEqualMatch { get; set; }

		public bool IsMatch(OracleFunctionIdentifier functionIdentifier)
		{
			var elementValue = Selector(functionIdentifier);
			return (!DenyEqualMatch && elementValue == Value.ToQuotedIdentifier()) ||
				   (AllowStartWithMatch && elementValue.ToUpperInvariant().Replace("\"", null).StartsWith(Value.ToSimpleIdentifier().Replace("\"", null))) ||
			       (AllowPartialMatch && elementValue.ToUpperInvariant().Contains(Value.ToSimpleIdentifier().Replace("\"", null)));
		}
	}
}