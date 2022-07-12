using UnityEngine;
using System;

[CreateAssetMenu(menuName = "FloraAnimation")] 
public class FloraAnimation : ActorAnimation {
    public PercentFunction[] percentFunctions;
    public PercentFunction[] soundClips;
    [Serializable]
    public class PercentFunction{
        public float pct;
        public string method;
    }
    

    public AttackInfo attackData;
    public float exitTime;
    public bool callbackOnExit;
    public bool loopData;
}