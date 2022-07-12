using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "AudioList")]
public class AudioList : ScriptableObject{
    
    public AudioListNode[] list;



    public Dictionary<string, AudioClip> CreateDict(){
        Dictionary<string, AudioClip> r = new Dictionary<String, AudioClip>();
        foreach(AudioListNode aln in list){
            r.Add(aln.label, aln.clip);
        }
        return r;
    }
    [Serializable]
    public class AudioListNode{
        public string label;
        public AudioClip clip;
    }
}