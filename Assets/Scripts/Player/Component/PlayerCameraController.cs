using UnityEngine;
using Unity.Cinemachine;

namespace Player.Component
{
    public class PlayerCameraController
    {
        // PlayerController에서 직접 할당받을 변수들
        private Transform playerTransform;
        private Transform headTransform;
        private Transform cameraRigTransform;
        private CinemachineCamera cinemachineCamera;
        private GameObject speedParticle;

        // 설정값 (PlayerController에서 값을 받아오거나 직접 설정)
        private float mouseSensitivity;
        private float pitchMin;
        private float pitchMax;
        private float idleFOV;
        private float movingFOV;
        private float fovChangeSpeed;

        private float currentPitch = 0.0f;
        private float currentTargetFOVInternal; // 내부에서 사용할 currentTargetFOV

        public PlayerCameraController(PlayerController pc)
        {
            playerTransform = pc.transform;

            // 설정값도 PlayerController에서 받아옴
            mouseSensitivity = pc.mouseSensitivity;
            pitchMin = pc.pitchMin;
            pitchMax = pc.pitchMax;
            idleFOV = pc.idleFOV; // PlayerController에 public float idleFOV; 가 있다고 가정
            movingFOV = pc.movingFOV; // PlayerController에 public float movingFOV; 가 있다고 가정
            fovChangeSpeed = pc.fovChangeSpeed; // PlayerController에 public float fovChangeSpeed; 가 있다고 가정

            headTransform = pc.head;
            cameraRigTransform = pc.cameraTransform;
            cinemachineCamera = pc.cinemachineCamera; 
            speedParticle = pc.speedParticle;

            currentTargetFOVInternal = idleFOV;
            if (cinemachineCamera != null)
            {
                cinemachineCamera.Lens.FieldOfView = currentTargetFOVInternal;
            }
        }

        public void HandleLookInput(Vector2 mouseDelta)
        {
            if (playerTransform == null) return; // Null 체크

            float mouseX = mouseDelta.x * mouseSensitivity;
            float mouseY = mouseDelta.y * mouseSensitivity;

            playerTransform.Rotate(Vector3.up * mouseX);

            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);

            if (headTransform != null) headTransform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
        }

        public void SetCameraFOV(float targetFOV, bool immediate = false)
        {
            currentTargetFOVInternal = targetFOV;
            if (cinemachineCamera == null) return; // Null 체크

            if (immediate)
            {
                cinemachineCamera.Lens.FieldOfView = targetFOV;
            }

            if (speedParticle != null) // Null 체크 추가
            {
                speedParticle.SetActive(targetFOV == movingFOV);
            }
        }

        public void UpdateFOV()
        {
            if (cinemachineCamera == null) return; // Null 체크
            if (Mathf.Abs(cinemachineCamera.Lens.FieldOfView - currentTargetFOVInternal) > 0.01f)
            {
                cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, currentTargetFOVInternal, Time.deltaTime * fovChangeSpeed);
            }
        }

        public void ChangeCameraFollowTarget(bool followHead)
        {
            if (cinemachineCamera == null) return; // Null 체크

            if (followHead && headTransform != null)
            {
                cinemachineCamera.Follow = headTransform;
            }
            else if (!followHead && cameraRigTransform != null)
            {
                cinemachineCamera.Follow = cameraRigTransform;
            }
        }
    }
}