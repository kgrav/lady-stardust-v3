using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseController : MonoBehaviour
{
	public Vector2 Input;

    public int this[int key]
    {
        get
        {
            switch (key)
            {
                case 1:
                    return Button1Buffer;
                case 2:
                    return Button2Buffer;
                case 3:
                    return Button3Buffer;
                case 4:
                    return Button4Buffer;
            }
            return 0;
        }
        set
        {
            switch (key) 
            {
                case 1:
                    Button1Buffer = value;
                    break;
                case 2:
                    Button2Buffer = value;
                    break;
                case 3:
                    Button3Buffer = value;
                    break;
                case 4:
                    Button4Buffer = value;
                    break;
            }
        }
    }

	public int Button1Buffer;
	public int Button2Buffer;
	public int Button3Buffer;
	public int Button4Buffer;

	public int Button1ReleaseBuffer;
	public int Button2ReleaseBuffer;
	public int Button3ReleaseBuffer;
	public int Button4ReleaseBuffer;


	public bool Button1Hold;
	public bool Button2Hold;
	public bool Button3Hold;
	public bool Button4Hold;

	public virtual void BeforeObjectUpdate()
	{

	}

	public virtual void PollForTargets()
	{

	}
}