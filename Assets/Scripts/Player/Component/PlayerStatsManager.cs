using UnityEngine;

namespace Player.Component
{
    public class PlayerStatsManager
    {
        private PlayerUIManager _playerUIManager;

        // 스탯 관련 설정값 (PlayerController에서 받아오거나 직접 설정)
        public int maxHealth = 100;
        public float regenerationCooldown = 5f;
        public int regenerationAmount = 1;
        public float invincibilityDuration = 0.5f;

        private int currentHealthInternal;
        public int CurrentHealth
        {
            get => currentHealthInternal;
            private set
            {
                int previousHealth = currentHealthInternal;
                currentHealthInternal = Mathf.Clamp(value, 0, maxHealth);

                if (_playerUIManager != null)
                    _playerUIManager.PlayerHpChanged(currentHealthInternal, previousHealth); // UI 업데이트

                if (currentHealthInternal <= 0)
                {
                    HandleDeath();
                }
            }
        }

        private float currentRegenerationTimer = 0f;
        private float currentInvincibilityTimer = 0f;

        public PlayerStatsManager(PlayerController pc, PlayerUIManager uiManager)
        {
            _playerUIManager = uiManager;

            // PlayerController에서 설정값 가져오기 (예시)
            maxHealth = pc.maxHealth;
            regenerationCooldown = pc.regenerationCooldown;
            regenerationAmount = pc.regenerationAmount;
            invincibilityDuration = pc.invincibilityDuration;

            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int damageAmount)
        {
            if (currentInvincibilityTimer > 0f || CurrentHealth <= 0) return;

            CurrentHealth -= damageAmount;
            currentInvincibilityTimer = invincibilityDuration;
            currentRegenerationTimer = regenerationCooldown; // 피격 시 재생 쿨타임 초기화

            PlayerController.Instance.PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Hit);
        }

        private void HandleDeath()
        {
            // PlayerController를 통해 게임 오버 로직 호출 또는 직접 처리
            _playerUIManager.EndGame(false);
        }

        public void UpdateTimers() // PlayerController.Update에서 호출
        {
            // 체력 재생 로직
            if (CurrentHealth < maxHealth && currentRegenerationTimer <= 0f)
            {
                CurrentHealth += regenerationAmount;
                currentRegenerationTimer = 0.5f;
            }
            else if (currentRegenerationTimer > 0f)
            {
                currentRegenerationTimer -= Time.deltaTime;
            }

            // 무적 시간 로직
            if (currentInvincibilityTimer > 0f)
            {
                currentInvincibilityTimer -= Time.deltaTime;
            }
        }

        // 외부에서 체력 변경 시 (예: 아이템 획득)
        public void ModifyHealth(int amount)
        {
            CurrentHealth += amount;
        }
    }
}