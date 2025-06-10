using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Component
{
    public class PlayerAttackController
    {
        private PlayerController _playerController; 
        private PlayerAnimator _playerAnimator;
        private PlayerUIManager _playerUIManager;
        private CharacterController _characterController; // 위치 계산용

        // 공격 관련 설정값
        public float attackRange; // PlayerController에서 설정값 가져오므로 public 유지 또는 private으로 변경 후 생성자에서만 할당
        public float attackPower;
        public float attackCooldown;
        public float attackMinChargeTime;
        public float attackMaxChargeTime;
        public float attackJumpPower; // 공격 시 위로 튕겨오르는 힘
        private float attackForwardPower; // 공격 시 앞으로 나아가는 힘 (PlayerController에서 가져옴)

        private float currentAttackChargeTime = 0f;
        private bool isAttackChargingInternal = false;
        private readonly Collider[] attackOverlapResults = new Collider[30];

        public PlayerAttackController(PlayerController pc, PlayerAnimator animator, PlayerUIManager uiManager, CharacterController characterCtrl)
        {
            _playerController = pc; // _playerController를 먼저 할당
            _playerAnimator = animator;
            _playerUIManager = uiManager;
            _characterController = characterCtrl;

            // PlayerController에서 설정값 가져오기
            attackRange = _playerController.attackRange;
            attackPower = _playerController.attackPower;
            attackCooldown = _playerController.attackCooldown;
            attackMinChargeTime = _playerController.attackMinChargeTime;
            attackMaxChargeTime = _playerController.attackMaxChargeTime;
            attackJumpPower = _playerController.attackJumpPower;
            attackForwardPower = _playerController.attackForwardPower; // 새로 추가된 설정값 가져오기
        }

        public void HandleAttackInput(InputAction.CallbackContext context)
        {
            if (_playerUIManager == null || _playerAnimator == null || _characterController == null || _playerController == null) return;

            if (_playerUIManager.isCooldownActive || (context.canceled && !isAttackChargingInternal))
            {
                return;
            }

            if (context.performed)
            {
                isAttackChargingInternal = true;
                currentAttackChargeTime = 0f;
                if (_playerController.attackOvercharge)
                {
                    _playerController.attackOvercharge = false;
                    currentAttackChargeTime = attackMaxChargeTime; // 과충전 상태면 바로 최대 차지 시간으로 설정
                }
            }
            else if (context.canceled)
            {
                isAttackChargingInternal = false;
                if (currentAttackChargeTime >= attackMinChargeTime)
                {
                    _playerAnimator.TriggerAttack();

                    Vector3 attackCenter = _characterController.transform.position + Vector3.up * (_characterController.height * 0.5f);
                    float currentChargeRatio = Mathf.Clamp01(currentAttackChargeTime / attackMaxChargeTime); // 0~1 사이 값 보장
                    float currentAttackRange = attackRange * (1 + currentChargeRatio * 0.5f); // 차지 시간에 따라 범위 약간 증가
                    float currentAttackPower = attackPower * (1 + currentChargeRatio); // 차지 시간에 따라 파워 증가


                    var hitCount = Physics.OverlapSphereNonAlloc(attackCenter, currentAttackRange, attackOverlapResults, LayerMask.GetMask("Enemy"));

                    for (int i = 0; i < hitCount; i++)
                    {
                        Collider hitCollider = attackOverlapResults[i];
                        if (hitCollider.TryGetComponent(out EnemyController enemyController))
                        {
                            Vector3 direction = (hitCollider.transform.position - _characterController.transform.position).normalized;
                            direction.y += 0.5f; // 약간 위로 밀도록

                            if (enemyController.overchargeTime <= 0)
                                _playerController.attackOvercharge = true; // 적이 공격을 받았을 때 과충전 상태로 전환 (1초 쿨타임)

                            enemyController.Impact(currentAttackPower * direction); // 힘의 크기는 차지 시간에 비례
                        }
                    }

                    // 공격 후 위로 튕겨오르기
                    if (_playerController.VerticalVelocity < 0)
                    {
                        _playerController.VerticalVelocity = 0f; // 하강 중이었다면 초기화
                    }
                    _playerController.VerticalVelocity = attackJumpPower * (1 + currentChargeRatio);

                    // 공격 후 바라보는 방향으로 힘 적용
                    Vector3 forwardDirection = _playerController.transform.forward; // 플레이어(카메라)의 정면 방향
                    forwardDirection.y = 0; // 수평 방향만 사용

                    if (forwardDirection.sqrMagnitude > 0.001f) // 유효한 수평 방향일 경우
                    {
                        forwardDirection.Normalize();
                        _playerController.lastHorizontalMoveDirection = forwardDirection;

                        // 최고 속도보다 낮으면 속도 증가
                        if (_playerController.currentHorizontalSpeed < _playerController.moveSpeed)
                            _playerController.currentHorizontalSpeed += attackForwardPower * (1 + currentChargeRatio);
                    }
                    // else: 수평 방향이 거의 없으면 (정면이 수직에 가까움) 수평 힘은 적용하지 않음.

                    _playerController.PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Attack);
                }

                _playerUIManager.SetSkillBarCooldown(attackCooldown);
                currentAttackChargeTime = 0f;
                // _playerUIManager.SetSkillBarCharge(0);
            }
        }

        public void UpdateAttackCharge() // PlayerController.Update에서 호출
        {
            if (isAttackChargingInternal && currentAttackChargeTime < attackMaxChargeTime)
            {
                currentAttackChargeTime += Time.deltaTime;
                _playerUIManager.SetSkillBarCharge(currentAttackChargeTime / attackMaxChargeTime);
            }
        }
    }
}