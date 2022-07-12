using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
public enum CAMDOM { PTAG, PROBE }
public class NVCam : MonoBehaviour
{
    static NVCam _nvcam;
    public static NVCam nvcam {get{if(!_nvcam) _nvcam = FindObjectOfType<NVCam>(); return _nvcam;}}

    public void SetDomain(CAMDOM lookat, CAMDOM follow){
        movewith = follow == CAMDOM.PROBE ? LookProbe : protag;
        lookprobe = lookat == CAMDOM.PROBE ? LookProbe : protag;
    }

    Transform movewith; //The camera will move relative to this object
    Transform lookprobe; //The camera will always look at this object.
    Transform protag;
    Transform LookProbe;
    Transform id;
    public float[] DistanceToggles;
    int DistanceCurr = 0;
    public float MoveSpeed;
    public float TurnSpeed;
    float cts, gts;
    public float PitchSpeed, minPitch, maxPitch;
    float cps, gps;
    public float idealheight;
    public float speedlimit;
    public float minDist, ignoredist;
    public bool protecting;
    float OriginDist;
    float CurrDist;
    float Velocity = 30.0f;
    float ClipMoveTime = 0.01f;
    float OSRad = 1, SCRad = 0.2f;
    float lookAngle;
    float pitchAngle;
    int side = 1;
    Transform _tform;
    Transform tform { get { if (!_tform) _tform = transform;  return _tform; } }
    public int GroupsInScene;
    int mode = 0;
    Vector3 pos, targetpos, lizpos;
    Vector3 CameraForward;
    bool targeting, hardtargeting;
    RaycastHit[] rs;
    bool pauseinput = false;
    public float deadzoneSize = 0.6f;
    void Awake(){
        protag = FindObjectOfType<CameraProbe>().transform;
        LookProbe = protag;
        lookprobe = protag;
        movewith = protag;
    }
    public void Pause()
    {
        pauseinput = true;
    }
    public void Unpause()
    {
        pauseinput = false;
    }

    public Vector3 Forward { get { return tform.forward; } }
    //Static method for printing messages to console
    //call this from classes which do not derive from MonoBehaviour
    public static void DBM(string s, string from)
    {
        print(from + ": " + s);
    }


    public float dspd;

    float rsx=0,rsy=0;

    public void SetLookInput(Vector2 input){
        rsx=input.x;
        rsy=input.y;
    }


    void Update()
    {
        Vector3 origin = tform.position;
        CameraForward = tform.TransformDirection(Vector3.forward);
        Vector3 PlanarForward = new Vector3(CameraForward.x, 0, CameraForward.y);
        float currentDistance = Vector3.Distance(tform.position, lookprobe.position);
        //Resolve Camera Motion
        bool imove = pauseinput ;
        gts = rsx * TurnSpeed;
        gps = rsy * PitchSpeed;
        bool moveTurn = Math.Abs(rsx) > deadzoneSize;
        bool movePitch = Math.Abs(rsy) > deadzoneSize;
        cts = moveTurn ? 0 : cts;
        cps = movePitch ? 0 : cps;
        if (moveTurn && cts != gts)
        {
            bool stop = false;
            bool lt = cts < gts;
            if (lt)
            {
                cts = cts == 0 ? 20 : cts;
                cts += TurnSpeed * Time.deltaTime;
                stop = cts > gts;
            }
            else
            {
                cts = cts == 0 ? -20 : cts;
                cts -= TurnSpeed * Time.deltaTime;
                stop = cts < gts;
            }
            if (stop)
                cts = gts;
        }
        if (movePitch && cps != gps)
        {
            bool stop = false;
            bool lt = cps < gps;
            if (lt)
            { 
                cps = cps == 0 ? 20 : cps;
                cps += PitchSpeed * Time.deltaTime;
                stop = cps > gps;
            }
            else
            {
                cps = cps == 0 ? -20 : cps;
                cps -= PitchSpeed * Time.deltaTime;
                stop = cps < gps;
            }
            if (stop)
                cps = gps;
        }
        bool ctf = false;
        Collider[] cs = Physics.OverlapSphere(origin, OSRad);
        for (int i = 0; i < cs.Length; ++i)
        {
            Transform q = cs[i].transform;
            if (q.GetComponent<OcclusionObject>() && Vector3.Angle(PlanarForward, PlanarDirection(q.position, tform.position)) < 75) {
                ctf = true;
                break;
            }
        }
        bool block;
        bool reset = false;
        Vector3 point = Vector3.zero;
        float targd = 0;
        Vector3 rcorigin = LookProbe.position;
        float dtog = DistanceToggles[DistanceCurr];
        int iter = 0;
        do
        {
            iter++;
            if(iter > 5)
                break;
            reset = false;
            RaycastHit[] rhs = Physics.RaycastAll(new Ray(rcorigin, -CameraForward), dtog);
            

            if (rhs.Length < 1 || rhs[0].Equals(default(RaycastHit)))
            {
                targd = dtog;

                break;
            }
            if (rhs[0].distance < ignoredist)
            {
                rcorigin = rhs[0].point + rhs[0].normal;
                reset = true;
            }
            else
            {
                targd = rhs[0].distance - 1.7f;
            }
        } while (reset);

        if (moveTurn)
        {
            lookAngle += cts;
        }
        if (movePitch)
        {
            pitchAngle -= cps;
            pitchAngle = Mathf.Clamp(pitchAngle, minPitch, maxPitch);
        }
        float v = 30.0f;
        currentDistance = Math.Abs(targd - currentDistance) > 0.2 ? Mathf.SmoothDamp(currentDistance, targd, ref v, currentDistance > targd ? ClipMoveTime : Time.deltaTime) : targd;
            point = Quaternion.Euler(pitchAngle,lookAngle, 0) * (Vector3.forward * currentDistance);
        
        Vector3 gpoint = movewith.position - point;
        float len = Vector3.Distance(gpoint, tform.position);
        if(len>0){
            float deltaTime = Time.deltaTime;
        float dist = deltaTime * MoveSpeed;
        float frac = dist / len;
        tform.position = NVMath.NVLerp(tform.position, gpoint,MoveSpeed,deltaTime);
        }
        tform.LookAt(lookprobe);
    }


    Vector3 PlanarDirection(Vector3 from, Vector3 to)
    {
        Vector3 t = to - from;
        t.y = 0;
        return t.normalized;
    }

    public Vector3 GetAim()
    {
        return transform.forward;
    }

}