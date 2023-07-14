using System.Collections;
using UnityEngine;

public class TestLogs : MonoBehaviour
{
    private IEnumerator Start()
    {
        for (var i = 0; i < 3; i++)
        {
            Debug.Log(i);
            yield return new WaitForSeconds(1);
        }
    }
}
