using UnityEngine;
using System.Collections;

public class SpheroConnectionView : MonoBehaviour {
	
	// Controls the look and feel of the Connection Scene
	public GUISkin m_SpheroConnectionSkin;	
	
	// Loading image
	public Texture2D m_Spinner;
	public Vector2 m_SpinnerSize = new Vector2(128, 128);
	public float m_SpinnerAngle = 0;
	Vector2 m_SpinnerPosition = new Vector2(0, 0);
	Vector2 m_SpinnerPivotPos = new Vector2(0, 0);
	Rect m_SpinnerRect;
	
	// UI Padding Variables
	int m_ViewPadding = 20;
	int m_ElementPadding = 10;
	
	// Button Size Variables
	int m_ButtonWidth = 200;
	int m_ButtonHeight = 55;
	
	// Title Variables
	int m_TitleWidth = 120;
	int m_TitleHeight = 40;
	string m_Title = "Connect to a Sphero";
	public GUIStyle m_TitleStyle = new GUIStyle();
	
	// Sphero Name Label Variable
	int m_SpheroLabelWidth = 350;
	int m_SpheroLabelHeight = 40;
	int m_SpheroLabelSelected = -1;

	// Paired Sphero Info
	int m_PairedRobotCount;
	string[] m_RobotNames;
	int m_RobotConnectingIndex = -1;
	
	// Custom ScrollView Variables
	public GUISkin optionsSkin;
    public GUIStyle rowSelectedStyle;
	
    // Internal variables for managing touches and drags
	private int selected = -1;
	private float scrollVelocity = 0f;
	private float timeTouchPhaseEnded = 0f;
	private const float inertiaDuration = 0.5f;
	
    Vector2 scrollPosition;

	// size of the window and scrollable list
    public Vector2 windowMargin;
    public Vector2 listMargin;
    private Rect windowRect;
	
	// Use this for initialization
	void Start () {		
		#if UNITY_ANDROID
			setupAndroid();
		#elif UNITY_IPHONE
			setupIOS();
		#else
			// Pop-up message that Sphero doesn't work with this platform?
		#endif
	}
	
	/*
	 * Called if the OS is iOS to immediately try to connect to the robot
	 */
	void setupIOS() {
		SpheroBridge.SetupRobotConnection();
		m_RobotConnectingIndex = 1;
		m_RobotNames = new string[0];
	}

// Needed for compiling on iOS
#if UNITY_ANDROID
	/*
	 * Called if the OS is Android to show the Connection Scene
	 */
	void setupAndroid() {

		// The SDK uses alot of handlers that need a valid Looper in the thread, so set that up here
        using (AndroidJavaClass jc = new AndroidJavaClass("android.os.Looper"))
        {
        	jc.CallStatic("prepare");
        }
		
		// Search for paired robots
		SpheroProvider.getSharedProvider().findRobots();
		m_RobotNames = SpheroProvider.getSharedProvider().getRobotNames();
		
		// Sign up for connection notifications
		using (AndroidJavaClass jc = new AndroidJavaClass("orbotix.unity.UnityConnectionMessageDispatcher"))
        {
			AndroidJavaObject jo = jc.CallStatic<AndroidJavaObject>("getDefaultDispatcher");
			jo.Call("addListener", "GUI", "javaMessage");
		}	
		
		// For debugging UI
//		m_RobotNames = new string[6];
//		m_RobotNames[0] = "Hello-0";
//		m_RobotNames[1] = "Hello-1";
//		m_RobotNames[2] = "Hello-2";
//		m_RobotNames[3] = "Hello-3";
//		m_RobotNames[4] = "Hello-4";
//		m_RobotNames[5] = "Hello-5";
	}
	
	public void javaMessage(string message) {
		if( message.Equals("failed") ) {
			m_Title = "Connection Failed";	
			// No longer connecting to a robot
			m_RobotConnectingIndex = -1;
		}
		else if( message.Equals("success") ) {
			m_Title = "Connection Success";
			
			// Tell Sphero Provider we have a newly connected robot
			Sphero[] spheros = new Sphero[1];
			spheros[0] = new Sphero(SpheroProvider.getSharedProvider().getConnectingRobot());
			SpheroProvider.getSharedProvider().setConnectedSpheros(spheros);
				
			// No longer connecting to a robot
			m_RobotConnectingIndex = -1;
			
			// Remove connection listener
			using (AndroidJavaClass jc = new AndroidJavaClass("orbotix.unity.UnityConnectionMessageDispatcher"))
	        {
				AndroidJavaObject jo = jc.CallStatic<AndroidJavaObject>("getDefaultDispatcher");
				jo.Call("removeListener", "GUI");
			}	
				
			// Go to HelloWorld Scene
			Application.LoadLevel ("HelloWorldScene"); 
		}
	}
#endif	
	
	void OnApplicationPause() {
		SpheroProvider.getSharedProvider().disconnectSpheros();
	}
	
	// Update is called once per frame
 	void Update()
    {
#if UNITY_ANDRDOID
		if (Input.touchCount != 1)
		{
			selected = -1;

			if ( scrollVelocity != 0.0f )
			{
				// slow down over time
				float t = (Time.time - timeTouchPhaseEnded) / inertiaDuration;
				float frameVelocity = Mathf.Lerp(scrollVelocity, 0, t);
				scrollPosition.y += frameVelocity * Time.deltaTime;
				
				// after N seconds, we've stopped
				if (t >= inertiaDuration) scrollVelocity = 0.0f;
			}
			return;
		}
		
		Touch touch = Input.touches[0];
		if (touch.phase == TouchPhase.Began)
		{
			selected = TouchToRowIndex(touch.position);
			scrollVelocity = 0.0f;
		}
		else if (touch.phase == TouchPhase.Canceled)
		{
			selected = -1;
		}
		else if (touch.phase == TouchPhase.Moved)
		{
			// dragging
			selected = -1;
			scrollPosition.y += touch.deltaPosition.y;
		}
		else if (touch.phase == TouchPhase.Ended)
		{
            // Was it a tap, or a drag-release?
            if ( selected > -1 )
            {
	            Debug.Log("Player selected row " + selected);
            }
			else
			{
				// impart momentum, using last delta as the starting velocity
				// ignore delta < 10; precision issues can cause ultra-high velocity
				if (Mathf.Abs(touch.deltaPosition.y) >= 10) 
					scrollVelocity = (int)(touch.deltaPosition.y / touch.deltaTime);
				timeTouchPhaseEnded = Time.time;
			}
		}
#elif UNITY_IPHONE
		if( SpheroBridge.IsRobotConnected() ) {
			m_Title = "Connection Success";
			
			// Tell Sphero Provider we have a newly connected robot
			Sphero[] spheros = new Sphero[1];
			spheros[0] = new Sphero();
			SpheroProvider.getSharedProvider().setConnectedSpheros(spheros);
				
			// No longer connecting to a robot
			m_RobotConnectingIndex = -1;
				
			// Go to HelloWorld Scene
			Application.LoadLevel ("HelloWorldScene"); 
		}
#endif
	}
	
	// Called when the GUI should update
	void OnGUI() {
		
		GUI.skin = m_SpheroConnectionSkin;
		
		// Draw a title lable
		GUI.Label(new Rect(m_ViewPadding,m_ViewPadding,Screen.width-(m_ViewPadding*2),m_TitleHeight), m_Title, "label");
		
		// Set up the scroll view that holds all the Sphero names
		int scrollY = m_ViewPadding + m_TitleHeight + m_ElementPadding*2;
        windowMargin = new Vector2(m_ViewPadding,scrollY);
        windowRect = new Rect(windowMargin.x, windowMargin.y-15, 
        				 Screen.width - (2*windowMargin.x), Screen.height - (2*windowMargin.y));
        GUI.Window(0, windowRect, (GUI.WindowFunction)DoWindow, "");
		
		// Set up the Connect Button
		int connectBtnX = (Screen.width/2)-(m_ButtonWidth/2);
		int connectBtnY = Screen.height-m_ViewPadding-m_ButtonHeight;
		if( GUI.Button(new Rect(connectBtnX,connectBtnY,m_ButtonWidth,m_ButtonHeight), "Connect" )) {
		
			// Check if we have a Sphero connected
			if( m_SpheroLabelSelected >= 0 ) {
				SpheroProvider.getSharedProvider().connect(m_SpheroLabelSelected);
				// Adjust title info
				m_RobotConnectingIndex = m_SpheroLabelSelected;
				m_Title = "Connecting to " + m_RobotNames[m_SpheroLabelSelected];		
			}
		}			
		
		// Only show the connection dialog if we are connecting to a robot
		if( m_RobotConnectingIndex >= 0 ) {
			GUI.Box(m_SpinnerRect,"");
			
			m_SpinnerPosition.x = Screen.width/2;
			m_SpinnerPosition.y = Screen.height/2;
			// Rotate the object
			m_SpinnerRect = new Rect(m_SpinnerPosition.x - m_SpinnerSize.x * 0.5f, m_SpinnerPosition.y - m_SpinnerSize.y * 0.5f, m_SpinnerSize.x, m_SpinnerSize.y);
        	m_SpinnerPivotPos = new Vector2(m_SpinnerRect.xMin + m_SpinnerRect.width * 0.5f, m_SpinnerRect.yMin + m_SpinnerRect.height * 0.5f);
			
			// Draw the new image
	        Matrix4x4 matrixBackup = GUI.matrix;
	        GUIUtility.RotateAroundPivot(m_SpinnerAngle, m_SpinnerPivotPos);
	        GUI.DrawTexture(m_SpinnerRect, m_Spinner);
	        GUI.matrix = matrixBackup;
			m_SpinnerAngle+=3;
		}
	}

	void DoWindow (int windowID) 
	{
		Vector2 listSize = new Vector2(windowRect.width - 2*listMargin.x,
									   windowRect.height - 2*listMargin.y);

		Rect rScrollFrame = new Rect(listMargin.x, listMargin.y, listSize.x, listSize.y);
		Rect rList = new Rect(0, 0, m_SpheroLabelWidth, m_RobotNames.Length*m_SpheroLabelHeight);
		
        scrollPosition = GUI.BeginScrollView (rScrollFrame, scrollPosition, rList, false, false);
            
		// Show a grid of Spheros to connect to
 		m_SpheroLabelSelected = GUI.SelectionGrid(new Rect(0,0,m_SpheroLabelWidth,m_SpheroLabelHeight*m_RobotNames.Length),m_SpheroLabelSelected,m_RobotNames,1,"toggle");
		
        GUI.EndScrollView();
	}

    private int TouchToRowIndex(Vector2 touchPos)
    {
		float y = Screen.height - touchPos.y;  // invert coordinates
		y += scrollPosition.y;  // adjust for scroll position
		y -= windowMargin.y;    // adjust for window y offset
		y -= listMargin.y;      // adjust for scrolling list offset within the window
		int irow = (int)(y / m_SpheroLabelHeight);
		
		irow = Mathf.Min(irow, m_RobotNames.Length);  // they might have touched beyond last row
		return irow;
    }
}
