using UnityEngine;
using System;

public class FloraFaceManager : NVComponent {
    public static FloraFaceManager mgr => FindObjectOfType<FloraFaceManager>();
    SkinnedMeshRenderer smr => GetComponent<SkinnedMeshRenderer>();

    public void SetFace(FloraFace face){
        smr.materials = new Material[] {face.mouth, face.eyes};
    }
}