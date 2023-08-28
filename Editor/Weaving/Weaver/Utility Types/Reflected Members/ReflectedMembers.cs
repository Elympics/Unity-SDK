using UnityEditor;

public static class ReflectedMembers
{
    public static ReflectedField<T> FindField<T>(this SerializedObject serializedObject, string methodName)
    {
        return new ReflectedField<T>(serializedObject, methodName);
    }

    public static int GetArrayIndexFromPropertyPath(string propertyPath)
    {
        var pathLength = propertyPath.Length - 2;
        var i = pathLength;

        while (i >= 0)
        {
            i--;
            if (!char.IsDigit(propertyPath[i]))
            {
                break;
            }
        }
        var length = pathLength - i;
        var startIndex = propertyPath.Length - (propertyPath.Length - i) + 1;
        var digits = propertyPath.Substring(startIndex, length);
        return int.Parse(digits);
    }
}
