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
        public float attackRange = 1.5f;
        public float attackPower = 10f;
        public float attackCooldown = 1f;
        public float attackMinChargeTime = 0.5f;
        public float attackMaxChargeTime = 2f;

        public float attackJumpPower = 5f; // 공격 시 위로 튕겨오르는 힘

        private float currentAttackChargeTime = 0f;
        private bool isAttackChargingInternal = false;
        private readonly Collider[] attackOverlapResults = new Collider[30];

        public PlayerAttackController(PlayerController pc, PlayerAnimator animator, PlayerUIManager uiManager, CharacterController characterCtrl)
        {
            _playerAnimator = animator;
            _playerUIManager = uiManager;
            _characterController = characterCtrl;

            // PlayerController에서 설정값 가져오기
            attackRange = pc.attackRange;
            attackPower = pc.attackPower;
            attackCooldown = pc.attackCooldown;
            attackMinChargeTime = pc.attackMinChargeTime;
            attackMaxChargeTime = pc.attackMaxChargeTime;
            attackJumpPower = pc.attackJumpPower;

            _playerController = pc;
        }

        public void HandleAttackInput(InputAction.CallbackContext context)
        {
            if (_playerUIManager == null || _playerAnimator == null || _characterController == null) return;

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

                    // 공격 위치는 CharacterController의 현재 위치를 기준으로
                    Vector3 attackCenter = _characterController.transform.position + Vector3.up * (_characterController.height * 0.5f);
                    float currentChargeRatio = currentAttackChargeTime / attackMaxChargeTime; // 0~1 사이 값
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

                    // 공격 후 위로 튕겨오르며 이는 중력을 초기화함
                    if (PlayerController.Instance.VerticalVelocity < 0)
                    {
                        PlayerController.Instance.VerticalVelocity = 0f; // 하강 중이었다면 초기화
                    }
                    PlayerController.Instance.VerticalVelocity = attackJumpPower * (1 + currentChargeRatio);

                    PlayerController.Instance.PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Attack);
                }

                _playerUIManager.SetSkillBarCooldown(attackCooldown);
                currentAttackChargeTime = 0f;
                // _playerUIManager.SetSkillBarCharge(0);

                // 공격 후엔 위로 잠깐 상승함
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