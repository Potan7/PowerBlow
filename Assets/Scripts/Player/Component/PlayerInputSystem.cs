using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Player.Component
{
    public class PlayerInputSystem : MonoBehaviour
    {
        public MyInputActions PlayerInput { get; private set; }
        private PlayerController _playerController;
        private PlayerCameraController _playerCameraController;
        private PlayerAttackController _playerAttackController; // 공격 컨트롤러 참조

        // 이벤트 정의
        public event Action<Vector2> MoveEvent;
        public event Action<Vector2> LookEvent;
        public event Action JumpEvent;
        public event Action CrouchEvent;
        public event Action<InputAction.CallbackContext> AttackEvent;
        public event Action MenuEvent;

        private void Awake()
        {
            PlayerInput = new MyInputActions();
        }

        public void Init(PlayerController controller, PlayerCameraController cameraController, PlayerAttackController attackController)
        {
            _playerController = controller;
            _playerCameraController = cameraController;
            _playerAttackController = attackController;

            SubscribeInputActions();
        }

        private void SubscribeInputActions()
        {
            PlayerInput.Player.Move.performed += ctx => MoveEvent?.Invoke(ctx.ReadValue<Vector2>());
            PlayerInput.Player.Move.canceled += ctx => MoveEvent?.Invoke(ctx.ReadValue<Vector2>());

            PlayerInput.Player.Look.performed += ctx => LookEvent?.Invoke(ctx.ReadValue<Vector2>());

            PlayerInput.Player.Jump.performed += ctx => JumpEvent?.Invoke();
            PlayerInput.Player.Crouch.performed += ctx => CrouchEvent?.Invoke();

            PlayerInput.Player.Attack.performed += ctx => AttackEvent?.Invoke(ctx);
            PlayerInput.Player.Attack.canceled += ctx => AttackEvent?.Invoke(ctx);

            PlayerInput.Player.Menu.performed += ctx => MenuEvent?.Invoke();
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            PlayerInput?.Enable();
        }

        private void OnDisable()
        {
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PlayerInput?.Disable();
        }

        private void OnDestroy()
        {
            PlayerInput?.Dispose();
        }

        // PlayerController 등에서 직접 호출하여 입력 활성화/비활성화
        public void EnablePlayerActions()
        {
            PlayerInput.Player.Enable();
        }

        public void DisablePlayerActions()
        {
            PlayerInput.Player.Disable();
        }
    }
}