﻿/*
 *
 *  PictureWindowControl.cs	(c) Ryan Sullivan 2017
 *  url: http://smirkingcat.software/picturewindow
 *
 *  Component used to control the window.
 *  
 *  Forked by Teruaki Tsubokura
 */

using UnityEngine;


[RequireComponent(typeof(SteamVR_TrackedController))]
public class PictureWindowControl3D : MonoBehaviour
{

    [Tooltip("Whether to use a modified version of SteamVR_TrackedObject")]
    public bool _useCustomTrackedObject = true;

    [Tooltip("The amount of time to hold GRIP in order to assign that controller to the window")]
    public float _assignTime = 1;

    [Tooltip("The amount of time to hold APP MENU in order to reset the window")]
    public float _resetTime = 3;


    // Timer for holding down the button to assign a controller or reset the window
    private float _gripStartTime;

    // Represents the tip of the controller
    private GameObject _tip;

    // This is the distance from the default origin of the Vive wand to it's frontmost tip
    private static Vector3 _tipOffset = new Vector3(0, -0.075f, 0.04f);

	private SteamVR_TrackedController _controller;

    private GameObject _model;
    private GameObject pwCam;



    private void Start()
    {

        _controller = GetComponent<SteamVR_TrackedController>();

        // Assign grip event listeners
        _controller.Gripped += Controller_Gripped;
        _controller.Ungripped += Controller_Ungripped;

        SteamVR_RenderModel renderModel = GetComponentInChildren<SteamVR_RenderModel>();
        if (renderModel != null) _model = renderModel.gameObject;

    }


    private void Update()
    {

        // If there isn't a controller assigned and we've held the grip long enough,
        // assign this controller.
        if (PictureWindow3D.Instance.WindowController == null &&
            (_gripStartTime != 0) && (Time.time - _gripStartTime) > _assignTime )
        {

            _gripStartTime = 0;

            if (_useCustomTrackedObject)
            {

                SteamVR_TrackedObject steamVR_TrackedObj = GetComponent<SteamVR_TrackedObject>();
                steamVR_TrackedObj.enabled = false;

                SmirkingCat_TrackedObject smirkingCat_TrackedObj = gameObject.AddComponent<SmirkingCat_TrackedObject>();
                smirkingCat_TrackedObj.SetDeviceIndex((int) steamVR_TrackedObj.index);

            }

            // Create a child object at the tip of the controller to take measurements from
            _tip = new GameObject();
            _tip.name = "Tip";
            _tip.transform.SetParent(transform);
            _tip.transform.localPosition = _tipOffset;

            AssignController();

            PictureWindow3D.Instance.ShowInstruction(PictureWindow3D.INSTRUCTION.BottomLeft);

        }

        // If there is an assigned controller and we've held the grip long enough,
        // reset the window.
        if (PictureWindow3D.Instance.WindowController != null &&
            (_gripStartTime != 0) && (Time.time - _gripStartTime) > _resetTime )
        {

            _gripStartTime = 0;

            PictureWindow3D.Instance.ResetWindow();
                
        }

    }


    private void OnDestroy()
    {

        _controller.Gripped -= Controller_Gripped;
        _controller.Ungripped -= Controller_Ungripped;
        _controller.PadClicked -= Controller_PadClicked;
        _controller.TriggerClicked -= Controller_TriggerClicked;

    }


    #region [CameraRig] listeners
    private void Controller_Gripped(object sender, ClickedEventArgs e)
    {

        // Set start time
        _gripStartTime = Time.time;

    }


    private void Controller_Ungripped(object sender, ClickedEventArgs e)
    {

        _gripStartTime = 0;

    }


    private void Controller_PadClicked(object sender, ClickedEventArgs e)
    {

        // Toggle debug box boolean
        PictureWindow3D.Instance._useTargetBox= !PictureWindow3D.Instance._useTargetBox;

    }


    private void Controller_TriggerClicked(object sender, ClickedEventArgs e)
    {

        // Assign one of the corners
        PictureWindow3D.Instance.SetCorner(_tip.transform.position);

    }
    #endregion


    private void AssignController()
    {

        // Set the assigned window controller to this one
        PictureWindow3D.Instance.WindowController = this;

        // Go through and disable other window controls
        PictureWindowControl3D[] windowControls = FindObjectsOfType<PictureWindowControl3D>();

        foreach (PictureWindowControl3D windowControl in windowControls)
        {

            if (windowControl != this)
            {

                Destroy(windowControl.GetComponent<PictureWindowControl3D>());

            }

        }

        if (_controller != null)
        {
            // Assign touchpad and trigger event listeners
            _controller.PadClicked += Controller_PadClicked;
            _controller.TriggerClicked += Controller_TriggerClicked;
        }

    }


    public void CreateCamera(PictureWindow3D.STEREO_MODE _mode = PictureWindow3D.STEREO_MODE.NONE)
    {

        // Disable the controller model (it just gets in the way)
        if (_model != null) _model.SetActive(false);

        if (_mode == PictureWindow3D.STEREO_MODE.NONE)
        {
            //pwCam = new GameObject("PWCamera2D", typeof(Camera), typeof(PictureWindowCamera));
            pwCam = Instantiate(PictureWindow3D.Instance.CameraPrefab, transform);
            //pwCam.transform.SetParent(transform);
            pwCam.transform.localPosition = Vector3.zero;

            Camera cam = pwCam.GetComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
        }else
        {
            pwCam = new GameObject("PWCamera3D");
            pwCam.transform.SetParent(transform);
            pwCam.transform.localPosition = Vector3.zero;

            // left cam
            GameObject pwCamLeft = Instantiate(PictureWindow3D.Instance.CameraPrefab, pwCam.transform);
            pwCamLeft.transform.localPosition = Vector3.zero + new Vector3(-PictureWindow3D.Instance.IPD / 2, 0, 0);

            Camera camLeft = pwCamLeft.GetComponent<Camera>();
            camLeft.nearClipPlane = 0.01f;
            camLeft.stereoTargetEye = StereoTargetEyeMask.None;

            // right cam
            GameObject pwCamRight = Instantiate(PictureWindow3D.Instance.CameraPrefab, pwCam.transform);
            pwCamRight.transform.localPosition = Vector3.zero + new Vector3(PictureWindow3D.Instance.IPD / 2, 0, 0);

            Camera camRight = pwCamRight.GetComponent<Camera>();
            camRight.nearClipPlane = 0.01f;
            camRight.stereoTargetEye = StereoTargetEyeMask.None;

            if (_mode == PictureWindow3D.STEREO_MODE.TOP_AND_BOTTOM)
            {
                camLeft.rect = new Rect(0, 0, 1, 0.5f);
                camRight.rect = new Rect(0, 0.5f, 1, 0.5f);
            }
            else if (_mode == PictureWindow3D.STEREO_MODE.SIDE_BY_SIDE) {
                camLeft.rect = new Rect(0, 0, 0.5f, 1);
                camRight.rect = new Rect(0.5f, 0, 0.5f, 1);
            }
        }

        PictureWindow3D.Instance.WindowCamera = pwCam;

    }


    public void DestroyCamera()
    {

        // Re-enable the controller model
        if (_model != null) _model.SetActive(true);
 
        if(pwCam) Destroy(pwCam);

    }


}