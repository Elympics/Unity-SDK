using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Elympics
{
	public class NamedSyncStatusArrayWrapper
	{
		public IEnumerable<NamedSynchronizationStatus> Elements
		{
			get
			{
				LoadCache();
				return _nameList.Select(x => new NamedSynchronizationStatus { Name = x, Enabled = !_disabledNameSet.Contains(x) });
			}
		}

		private readonly SerializedProperty _arrayProperty;
		private readonly List<string> _nameList = new List<string>();

		// temporary
		private readonly HashSet<string> _disabledNameSet = new HashSet<string>();
		private readonly HashSet<string> _newNameSet      = new HashSet<string>();

		public NamedSyncStatusArrayWrapper(SerializedProperty arrayProperty)
		{
			_arrayProperty = arrayProperty;
		}

		public void UpdateList(IEnumerable<string> names)
		{
			_nameList.Clear();
			_newNameSet.Clear();
			foreach (var name in names)
			{
				_newNameSet.Add(name);
				_nameList.Add(name);
			}

			LoadCache();
			var previousCount = _disabledNameSet.Count;
			_disabledNameSet.RemoveWhere(x => !_newNameSet.Contains(x));
			if (_disabledNameSet.Count != previousCount)
				WriteBackCache();
		}

		public void SetEnabled(string name, bool isEnabled)
		{
			for (var i = 0; i < _arrayProperty.arraySize; i++)
			{
				if (_arrayProperty.GetArrayElementAtIndex(i).stringValue != name)
					continue;
				if (isEnabled)
					_arrayProperty.DeleteArrayElementAtIndex(i);
				return;
			}

			var newIndex = _arrayProperty.arraySize;
			_arrayProperty.InsertArrayElementAtIndex(newIndex);
			_arrayProperty.GetArrayElementAtIndex(newIndex).stringValue = name;
		}

		private void LoadCache()
		{
			_disabledNameSet.Clear();
			foreach (SerializedProperty element in _arrayProperty)
				_disabledNameSet.Add(element.stringValue);
		}

		private void WriteBackCache()
		{
			_arrayProperty.ClearArray();
			foreach (var name in _disabledNameSet)
			{
				var newIndex = _arrayProperty.arraySize;
				_arrayProperty.InsertArrayElementAtIndex(newIndex);
				_arrayProperty.GetArrayElementAtIndex(newIndex).stringValue = name;
			}
		}
	}
}