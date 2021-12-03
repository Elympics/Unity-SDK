using System;
using UnityEngine;

namespace Elympics
{
	[AttributeUsage(AttributeTargets.Field)]
	public class ConfigForVarAttribute : PropertyAttribute
	{
		public readonly string ElympicsVarPropertyName;
		public readonly string DisplayName = null;

		public ConfigForVarAttribute(string elympicsVarPropertyName, string displayName = null)
		{
			ElympicsVarPropertyName = elympicsVarPropertyName;
			DisplayName = displayName;
		}
	}
}
