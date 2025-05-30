using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Component
{
    public class PlayerAttackController
    {
        private PlayerAnimator _playerAnimator;
        private PlayerUIManager _playerUIManager;
        private CharacterController _characterController; // 위치 계산용

        // 공격 관련 설정값 (PlayerController에서 받아오거나 직접 설정)
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

            // PlayerController에서 설정값 가져오기 (예시)
            attackRange = pc.attackRange;
            attackPower = pc.attackPower;
            attackCooldown = pc.attackCooldown;
            attackMinChargeTime = pc.attackMinChargeTime;
            attackMaxChargeTime = pc.attackMaxChargeTime;
            attackJumpPower = pc.attackJumpPower;
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
                            enemyController.Impact(currentAttackPower * direction); // 힘의 크기는 차지 시간에 비례
                        }
                    }

                    // 공격 후 위로 튕겨오름
                    PlayerController.Instance.VerticalVelocity += attackJumpPower * (1 + currentChargeRatio);
                }

                _playerUIManager.SetSkillBarCooldown(attackCooldown);
                currentAttackChargeTime = 0f;
                _playerUIManager.SetSkillBarCharge(0);

                // 공격 후엔 위로 잠깐 상승함
            }
        }

        public void UpdateAttackCharge() // PlayerController.Update에서 호출
        {
            if (isAttackChargingInternal && currentAttackChargeTime < attackMaxChargeTime)
            {
                currentAttackChargeTime += Time.deltaTime;
                if (_playerUIManager != null)
                    _playerUIManager.SetSkillBarCharge(currentAttackChargeTime / attackMaxChargeTime);
            }
        }
    }
}