using System.Collections.Generic;

namespace SqlPad
{
	public class BindVariableModel : ModelBase
	{
		public BindVariableModel(BindVariableConfiguration bindVariable)
		{
			BindVariable = bindVariable;
		}

		public BindVariableConfiguration BindVariable { get; }

		public string Name => BindVariable.Name;

		public ICollection<string> DataTypes => BindVariable.DataTypes.Keys;

		public object Value
		{
			get { return BindVariable.Value; }
			set
			{
				if (BindVariable.Value == value)
					return;

				BindVariable.Value = value;
				RaisePropertyChanged(nameof(Value));
			}
		}

		public string InputType => BindVariable.DataTypes[BindVariable.DataType].Name;

		public string DataType
		{
			get { return BindVariable.DataType; }
			set
			{
				if (BindVariable.DataType == value)
					return;

				var previousInputType = BindVariable.DataTypes[BindVariable.DataType].Name;
				BindVariable.DataType = value;

				if (previousInputType != InputType)
				{
					Value = null;
					RaisePropertyChanged(nameof(InputType));
				}

				RaisePropertyChanged(nameof(DataType));
			}
		}
	}
}
