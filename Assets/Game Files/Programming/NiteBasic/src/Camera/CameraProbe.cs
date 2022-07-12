using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KinematicCharacterController;
public class CameraProbe : MonoBehaviour
{

    bool on = false;
    bool RETURNING = false;
    Transform whos;
    Transform tform=>transform;
    public Vector3 position;

    public float fwdOffset,upOffset;
    public Vector3 goal;
    public Vector3 direction;
    public SmartObject watch;
    new NVCam camera;
    public float hspeed,vspeed;
    public float accel;
    float currSpd = 0;
    void Start()
    {
        whos = watch.tform;
        position = transform.position - whos.position;
        goal = position;
        camera = NVCam.nvcam;
        //hspeed = watch.GetComponent<FloraController>().MaxStableMoveSpeed;
        direction = whos.transform.forward * (-1);
    }
    bool freeze;
    public void SetFreeze(bool f)
    {
        freeze = f;
    }
    void Update()
    {

        if (freeze)
            return;
            Vector3 fvel = watch.RBody.velocity;
            fvel.y=0;
            if (watch.RBody.velocity.magnitude > 0)
            { 
                goal = whos.position + fwdOffset * fvel.normalized + Vector3.up * upOffset;
            }
            else
            {
                goal = whos.position + Vector3.up * upOffset;
            }
        Vector3 goalPlanar = goal, posPlanar=tform.position;
        goalPlanar.y = 0;
        posPlanar.y=0;
        
        Vector3 newPos = NVMath.NVLerp(posPlanar,goalPlanar,hspeed,Time.deltaTime);
        newPos.y = Mathf.Lerp(tform.position.y,goal.y,vspeed*Time.deltaTime/Mathf.Abs(tform.position.y-goal.y));
        tform.position = newPos;
        /*Vector3 fvel = watch.rbody.velocity;
        Vector3 pvel = new Vector3(0,fvel.y,0);
        fvel.y = 0;
        float sgoal = fvel.magnitude;
        float smn = speed;
        tform.position = tform.position + (direction * Mathf.Min(smn*Time.deltaTime, dista));*/
    }
    public void SetActivity(bool b)
    {
        on = b;
    }
}