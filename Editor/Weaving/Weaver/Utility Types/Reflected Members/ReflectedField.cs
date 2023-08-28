using System.Collections;
using System.Reflection;
using UnityEditor;

public class ReflectedField<T>
{
    private T m_Value;
    private object m_TargetInstance;
    private FieldInfo m_FieldInfo;

    public T value
    {
        get => m_Value;
        set
        {
            if (!value.Equals(m_Value))
            {
                m_Value = (T)m_FieldInfo.GetValue(m_TargetInstance);
            }
        }
    }

    public ReflectedField(SerializedObject serializedObject, string propertyPath)
    {
        FindTarget(serializedObject, propertyPath);
    }

    private void FindTarget(SerializedObject serializedObject, string propertyPath)
    {
        m_TargetInstance = null;

        for (var i = 0; i < serializedObject.targetObjects.Length; i++)
        {
            m_TargetInstance = serializedObject.targetObjects[i];
            var members = propertyPath.Split('.');

            for (var memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                var memberName = members[memberIndex];
                var instanceType = m_TargetInstance.GetType();

                if (string.CompareOrdinal("Array", memberName) == 0)
                {
                    // Skip to the next index
                    memberIndex++;
                    // Array.data[0] // Example of what we are trying to parse
                    var arrayPath = members[memberIndex];
                    // grab our index
                    var arrayIndex = ReflectedMembers.GetArrayIndexFromPropertyPath(arrayPath);
                    // Cast our instance as a IList
                    var asList = (IList)m_TargetInstance;
                    // Grab the element
                    m_TargetInstance = asList[arrayIndex];
                }
                else if (memberIndex == members.Length - 1)
                {
                    m_FieldInfo = instanceType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                }
                else
                {
                    var fieldInfo = instanceType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    m_TargetInstance = fieldInfo.GetValue(m_TargetInstance);
                }
            }

            if (m_FieldInfo.IsStatic)
            {
                m_TargetInstance = null;
                // Only invoke static methods once.
                break;
            }
        }

        m_Value = (T)m_FieldInfo.GetValue(m_TargetInstance);
    }
}
