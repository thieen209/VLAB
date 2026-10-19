using System;
using UnityEngine;

public sealed class VlabAirBlower : MonoBehaviour
{
    [SerializeField] private Transform rocker;
    [SerializeField] private Transform outletSocket;
    [SerializeField] private Transform outletBase;
    private Quaternion restRotation;
    public bool IsOn { get; private set; }
    public Transform OutletSocket => outletSocket;
    public Vector3 OutletAxis => outletSocket != null && outletBase != null
        ? (outletSocket.position - outletBase.position).normalized : Vector3.zero;
    public event Action<bool> PowerChanged;

    private void Awake()
    {
        if (rocker == null) { enabled = false; Debug.LogError("Air blower rocker is not assigned.",this); return; }
        restRotation = rocker.localRotation;
        SetPower(false);
    }

    public void Toggle() => SetPower(!IsOn);

    public void SetPower(bool on)
    {
        if (rocker == null) return;
        bool changed = IsOn != on;
        IsOn = on;
        rocker.localRotation = restRotation * Quaternion.AngleAxis(on ? -9 : 9,Vector3.right);
        if (changed) PowerChanged?.Invoke(on);
    }

    private void OnDisable() => SetPower(false);
}
