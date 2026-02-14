using UnityEngine;
using System;

public class TriggerRelay : MonoBehaviour
{
    public event Action<Collider2D> OnStay;
    public event Action<Collider2D> OnEnter;

    private void OnTriggerStay2D(Collider2D other) => OnStay?.Invoke(other);
    private void OnTriggerEnter2D(Collider2D other) => OnEnter?.Invoke(other);
}
