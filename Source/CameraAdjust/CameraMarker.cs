using System;
using System.IO;
using UnityEngine;


namespace OLDD_camera.CameraAdjust
{
    public class CameraMarker : MonoBehaviour
    {
        private Modules.DockingCameraModule _dcm;

        public void LinkPart(Modules.DockingCameraModule newDCM)
        {
            print("Linking part");
            _dcm = newDCM;
            transform.position = _dcm._camera._moduleDockingNodeGameObject.transform.position;
        }

        internal void DoUpdate(Vector3 v3)
        {
            if (_dcm == null) return;

            transform.position = _dcm._camera._moduleDockingNodeGameObject.transform.position;

            transform.rotation = _dcm.part.transform.rotation * Quaternion.LookRotation(_dcm.cameraForward, _dcm.cameraUp);
            transform.position += _dcm._camera._moduleDockingNodeGameObject.transform.rotation * v3;

            _dcm._camera._moduleDockingNodeGameObject.transform.position = transform.position;
            _dcm._camera._moduleDockingNodeGameObject.transform.rotation = transform.rotation;

            DrawTools.DrawArrow(transform.position, transform.forward﻿, Color.red);
        }
    }
}
