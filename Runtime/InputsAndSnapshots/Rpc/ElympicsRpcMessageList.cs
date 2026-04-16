using System.Collections;
using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsRpcMessageList : IToServer, IFromServer, IList<ElympicsRpcMessage>
    {
        [Key(0)] public List<ElympicsRpcMessage> Messages { get; set; } = new();

        public ElympicsRpcMessageList()
        { }

        internal ElympicsRpcMessageList(ElympicsRpcMessage message) =>
            Messages.Add(message);

        [IgnoreMember] public int Count => Messages.Count;
        [IgnoreMember] public bool IsReadOnly => false;

        public IEnumerator<ElympicsRpcMessage> GetEnumerator() => Messages.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(ElympicsRpcMessage item) => Messages.Add(item);
        public void AddRange(IEnumerable<ElympicsRpcMessage> collection) => Messages.AddRange(collection);
        public void Clear() => Messages.Clear();
        public bool Contains(ElympicsRpcMessage item) => Messages.Contains(item);
        public void CopyTo(ElympicsRpcMessage[] array, int arrayIndex) => Messages.CopyTo(array, arrayIndex);
        public bool Remove(ElympicsRpcMessage item) => Messages.Remove(item);
        public int IndexOf(ElympicsRpcMessage item) => Messages.IndexOf(item);
        public void Insert(int index, ElympicsRpcMessage item) => Messages.Insert(index, item);
        public void RemoveAt(int index) => Messages.RemoveAt(index);
        public ElympicsRpcMessage this[int index]
        {
            get => Messages[index];
            set => Messages[index] = value;
        }
    }

}
