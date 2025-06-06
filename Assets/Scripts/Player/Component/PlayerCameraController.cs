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

        // 설정값 (PlayerController에서 값을 받아오거나 직접 설정)
        private float pitchMin;
        private float pitchMax;
        private float idleFOV;
        // private float movingFOV;
        private float fovChangeSpeed;

        private float currentPitch = 0.0f;
        private float currentTargetFOVInternal; // 내부에서 사용할 currentTargetFOV

        // 마우스 감도는 MenuManager에서 가져옴
        private float MouseSensitivity => MenuManager.mouseSensitivity;

        private float previousPlayerRotationY; // 이전 프레임의 플레이어 Y축 회전값
        public bool IsRapidTurn { get; private set; } // 급회전 감지 플래그
        public float rapidTurnThreshold = 25f; // 급회전으로 간주할 각도 (프레임당)

        public PlayerCameraController(PlayerController pc)
        {
            playerTransform = pc.transform;

            // 설정값도 PlayerController에서 받아옴
            pitchMin = pc.pitchMin;
            pitchMax = pc.pitchMax;
            idleFOV = pc.idleFOV;
            // movingFOV = pc.movingFOV;
            fovChangeSpeed = pc.fovChangeSpeed;

            headTransform = pc.head;
            cameraRigTransform = pc.cameraTransform;
            cinemachineCamera = pc.cinemachineCamera; 
            // speedParticle = pc.speedParticle;

            currentTargetFOVInternal = idleFOV;
            cinemachineCamera.Lens.FieldOfView = currentTargetFOVInternal;
            previousPlayerRotationY = playerTransform.eulerAngles.y; // 초기 회전값 설정
        }

        public void HandleLookInput(Vector2 mouseDelta)
        {
            float mouseX = mouseDelta.x * MouseSensitivity;
            float mouseY = mouseDelta.y * MouseSensitivity;

            // 현재 프레임의 Y축 회전 변화량 계산
            float currentRotationY = playerTransform.eulerAngles.y;
            float deltaRotationY = Mathf.DeltaAngle(previousPlayerRotationY, currentRotationY + mouseX); // mouseX를 더한 후의 예상 회전 변화

            // 플레이어 Y축 회전 적용
            playerTransform.Rotate(Vector3.up * mouseX);

            // 급회전 감지
            if (Mathf.Abs(deltaRotationY) > rapidTurnThreshold)
            {
                IsRapidTurn = true;
            }
            else
            {
                IsRapidTurn = false;
            }
            previousPlayerRotationY = playerTransform.eulerAngles.y; // 다음 프레임을 위해 현재 회전값 저장


            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);

            headTransform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
            cameraRigTransform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
        }

        public void SetCameraFOV(float targetFOV, bool immediate = false)
        {
            currentTargetFOVInternal = targetFOV;

            if (immediate)
            {
                cinemachineCamera.Lens.FieldOfView = targetFOV;
            }

            // speedParticle.SetActive(targetFOV == movingFOV);
        }

        public void UpdateFOV()
        {
            if (Mathf.Abs(cinemachineCamera.Lens.FieldOfView - currentTargetFOVInternal) > 0.01f)
            {
                cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, currentTargetFOVInternal, Time.deltaTime * fovChangeSpeed);
            }
        }

        public void ChangeCameraFollowTarget(bool followHead)
        {
            if (followHead)
            {
                cinemachineCamera.Follow = headTransform;
            }
            else if (!followHead)
            {
                cinemachineCamera.Follow = cameraRigTransform;
            }
        }
    }
}