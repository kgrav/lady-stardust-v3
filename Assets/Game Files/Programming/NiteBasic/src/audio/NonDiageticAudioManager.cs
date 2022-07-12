using UnityEngine;
using System.Collections.Generic;

public class NonDiageticAudioManager : MonoBehaviour{

    static NonDiageticAudioManager _ndam;
    public static NonDiageticAudioManager ndam {get{if(!_ndam) _ndam = FindObjectOfType<NonDiageticAudioManager>(); return _ndam;}}

    public AudioList audioList;
    Dictionary<string, AudioClip> sfxDict;
    public int size;
    
    int current;
    
    AudioSource[] _audioSources;
    AudioSource[] audioSources{
        get{
            if(!sourcesInit){
                sourcesInit=true;
                _audioSources = new AudioSource[size];
                for(int i = 0; i < size; ++i){
                    _audioSources[i] = Instantiate(new GameObject()).AddComponent<AudioSource>();
                    _audioSources[i].transform.parent = transform;
                    //_audioSources[i].volume=0.1f;
                    //_audioSources[i].pitch=0.5f;
                }
            }
            return _audioSources;
        }
    }
    bool sourcesInit = false;

    public static void PlayClip(string clip){
        ndam.audioSources[ndam.current].PlayOneShot(ndam.sfxDict[clip]);
        ndam.current++;
        if(ndam.current >= ndam.size){
            ndam.current = 0;
        }
    }

}