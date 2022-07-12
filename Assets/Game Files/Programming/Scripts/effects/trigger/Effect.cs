using UnityEngine;
using System;

public abstract class Effect : NVComponent{
    public string label;
    public abstract void Trigger(Vector3 position, Vector3 direction);
}