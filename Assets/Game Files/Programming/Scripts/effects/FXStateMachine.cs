using UnityEngine;
using System;
using System.Collections.Generic;

public class FXStateMachine : MonoBehaviour
{
    public FXEntry[] LocalEffects;

    Dictionary<string, Effect> TrigFX;
    Dictionary<string, ToggleEffect> TogFX;
    Dictionary<string, AudioClip> sfx;

    public AudioList audioList;
    public AudioSource audioSource => GetComponent<AudioSource>();
    
    void Start()
    {
        TrigFX = new Dictionary<string, Effect>();
        TogFX = new Dictionary<string, ToggleEffect>();
        sfx = new Dictionary<string, AudioClip>();
        var alist = audioList.list;
        foreach(var a in alist){
            sfx.Add(a.label,a.clip);
        }
        foreach (FXEntry f in LocalEffects)
        {
            switch (f.type)
            {
                case FXType.Trigger:
                    TrigFX.Add(f.label, f.value.GetComponent<Effect>());
                    break;
                case FXType.Toggle:
                    TogFX.Add(f.label, f.value.GetComponent<ToggleEffect>());
                    break;
            }
        }


    }

    public void ResolveFXFrame(FXFrame fx){
        switch(fx.type){
				case FXType.Trigger:
					if(fx.local){
						SetTriggerEffect(fx.label,fx.position,fx.direction);
					}
					else{
						GlobalEffectsTable.TriggerEffect(fx.label,fx.position,fx.direction);
					}
				break;
				case FXType.Toggle:
					if(fx.local){
						SetToggleEffect(fx.label,fx.activitySetting);

					}
					else{
						GlobalEffectsTable.ToggleEffect(fx.label, fx.activitySetting);
					}
				break;
				case FXType.Sound:
					PlaySound(fx.label);
				break;
        }
    }

    public void PlaySound(string label){
        audioSource.PlayOneShot(sfx[label]);
    }

    public bool GetToggleState(string label){
        return TogFX[label].active;
    }

    public void SetToggleEffect(string label, bool activity){
        TogFX[label].SetActive(activity);
    }

    public void SetTriggerEffect(string label, Vector3 position, Vector3 direction){
        TrigFX[label].Trigger(position, direction);
    }
}