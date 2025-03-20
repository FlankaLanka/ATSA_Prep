using UnityEngine;
using System;

public class ReplaySystemTab : MonoBehaviour
{
    public event Action OnTargetDisabled;

    private void OnDisable()
    {
        OnTargetDisabled?.Invoke();
    }
}
