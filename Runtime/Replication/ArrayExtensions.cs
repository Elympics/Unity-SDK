namespace Elympics.Replication
{
    internal static class ArrayExtensions
    {
        internal static T[] Resized<T>(this T[] array, int newCapacity, int copyCount)
        {
            if (copyCount > newCapacity)
                throw new System.ArgumentOutOfRangeException(nameof(copyCount),
                    $"copyCount ({copyCount}) exceeds newCapacity ({newCapacity}).");
            var newArray = new T[newCapacity];
            if (copyCount > 0)
                System.Array.Copy(array, newArray, copyCount);
            return newArray;
        }
    }
}
