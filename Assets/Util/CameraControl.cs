using UnityEngine;
using UnityEngine.InputSystem;

namespace Util
{
    public class CameraControl : MonoBehaviour
    {
        float rotationX;
        float rotationY;

        void Update()
        {
            MoveCamera();
        }

        void MoveCamera()
        {
            if (Keyboard.current.wKey.isPressed)
            {
                transform.position +=
                    transform.forward.normalized * Time.deltaTime;
            }

            if (Keyboard.current.aKey.isPressed)
            {
                transform.position -=
                    transform.right.normalized * Time.deltaTime;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                transform.position -=
                    transform.forward.normalized * Time.deltaTime;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                transform.position +=
                    transform.right.normalized * Time.deltaTime;
            }

            if (Keyboard.current.qKey.isPressed)
            {
                transform.position += Vector3.down * Time.deltaTime;
            }

            if (Keyboard.current.eKey.isPressed)
            {
                transform.position += Vector3.up * Time.deltaTime;
            }
        }
    }
}