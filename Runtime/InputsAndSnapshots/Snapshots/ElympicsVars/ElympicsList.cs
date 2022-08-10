using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Elympics
{
	[Serializable]
	public class ElympicsList<T> : ElympicsVar, IEnumerable<T> where T : ElympicsVar
	{
		private Func<T> _factoryFunc;
		private List<T> _values;

		private T _listElementInstanceForDataCompare;

		public int Count => _values.Count;

		public bool IsReadOnly => false;

		public T this[int index] => _values[index];

		public ElympicsList(Func<T> factory, int elementsInListAtStart = 0, bool enableSynchronization = true) : base(enableSynchronization)
		{
			_factoryFunc = factory;
			_listElementInstanceForDataCompare = factory();

			_values = new List<T>();
			AddElementsToList(elementsInListAtStart);
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write(_values.Count);

			foreach (var elympicsVar in _values)
				elympicsVar.Serialize(bw);
		}

		public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
		{
			var elementsInList = br.ReadInt32();

			if (elementsInList > _values.Count)
				AddElementsToList(elementsInList - _values.Count);
			else if (elementsInList < _values.Count)
				RemoveElementsFromList(_values.Count - elementsInList);

			foreach (var elympicsVar in _values)
				elympicsVar.Deserialize(br, ignoreTolerance);
		}

		private void AddElementsToList(int elementsToAdd)
		{
			for (var i = 0; i < elementsToAdd; i++)
				Add();
		}

		private void RemoveElementsFromList(int elementsToRemove)
		{
			_values.RemoveRange(_values.Count - elementsToRemove, elementsToRemove);
		}

		public override bool Equals(BinaryReader br1, BinaryReader br2)
		{
			var list1Length = br1.ReadInt32();
			var list2Length = br2.ReadInt32();

			if (list1Length != list2Length)
				return false;

			for (var i = 0; i < list1Length; i++)
				if (!_listElementInstanceForDataCompare.Equals(br1, br2))
					return false;

			return true;
		}

		public int IndexOf(T item)
		{
			return _values.IndexOf(item);
		}

		public T Insert(int index)
		{
			var createdInstance = CreateAndInitialize();
			_values.Insert(index, createdInstance);

			return createdInstance;
		}

		public void RemoveAt(int index)
		{
			_values.RemoveAt(index);
		}

		public T Add()
		{
			var createdInstance = CreateAndInitialize();
			_values.Add(createdInstance);

			return createdInstance;
		}

		private T CreateAndInitialize()
		{
			var createdInstance = _factoryFunc();
			createdInstance.Initialize(Elympics);
			return createdInstance;
		}

		public void Clear()
		{
			_values.Clear();
		}

		public bool Contains(T item)
		{
			return _values.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_values.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			return _values.Remove(item);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _values.GetEnumerator();
		}

		internal override void Initialize(IElympics elympics)
		{
			base.Initialize(elympics);
			_listElementInstanceForDataCompare.Initialize(elympics);

			foreach (var element in _values)
				element.Initialize(elympics);
		}
	}
}
