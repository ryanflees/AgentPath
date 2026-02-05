using UnityEngine;
using System.Collections;

public class FpsTest : MonoBehaviour
{

    public float fpsMeasuringDelta = 2.0f;

    private float timePassed;
    private int mFrameCount = 0;
    private float mFPS = 0.0f;

    private void Start()
    {
        timePassed = 0.0f;
    }

    private void Update()
    {
        mFrameCount = mFrameCount + 1;
        timePassed = timePassed + Time.deltaTime;

        if (timePassed > fpsMeasuringDelta)
        {
            mFPS = mFrameCount / timePassed;

            timePassed = 0.0f;
            mFrameCount = 0;
        }
    }

    private void OnGUI()
    {
        GUIStyle bb = new GUIStyle();
        bb.normal.background = null; 
        bb.normal.textColor = new Color(1.0f, 0.5f, 0.0f); 
        bb.fontSize = 40;
        
        GUI.Label(new Rect(Screen.width - 200, 0, 200, 200), "FPS: " + mFPS, bb);
    }
}