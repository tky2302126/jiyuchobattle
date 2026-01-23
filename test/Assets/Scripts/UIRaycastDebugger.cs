using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIRaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (EventSystem.current == null) return;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        if (results.Count > 0)
        {
            Debug.Log("---- UI Hit ----");
            foreach (var r in results)
            {
                Debug.Log($"Hit: {r.gameObject.name}");
            }
        }
    }
}
