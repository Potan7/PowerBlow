using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footAudioSource;
    public AudioSource headAudioSource;
    public AudioSource bodyAudioSource;
    public AudioSource groundSource;

    [Header("Audio Clips")]
    public AudioClip[] footstepSound;
    public AudioClip[] hitSound;
    public AudioClip[] jumpSound;
    public AudioClip[] jumpGroundSound;
    public AudioClip[] landSound;
    public AudioClip[] slideSound;
    public AudioClip[] attackSound;
    public AudioClip[] attackExplosionSound;

    public AudioClip[] vaultingSound;

    public enum PlayerAudioType
    {
        Footstep,
        Hit,
        Jump,
        Land,
        Slide,
        Attack,
        Vaulting
    }

    public void PlaySound(PlayerAudioType audioType)
    {
        switch (audioType)
        {
            case PlayerAudioType.Footstep:
                PlaySound(footstepSound, footAudioSource);
                break;
            case PlayerAudioType.Hit:
                PlaySound(hitSound, bodyAudioSource);
                break;
            case PlayerAudioType.Jump:
                PlaySound(jumpSound, headAudioSource);
                PlaySound(jumpGroundSound, footAudioSource);
                break;
            case PlayerAudioType.Land:
                PlaySound(landSound, groundSource);
                break;
            case PlayerAudioType.Slide:
                PlaySound(slideSound, footAudioSource);
                break;
            case PlayerAudioType.Attack:
                PlaySound(attackSound, headAudioSource);
                PlaySound(attackExplosionSound, footAudioSource);
                break;
            case PlayerAudioType.Vaulting:
                PlaySound(vaultingSound, headAudioSource);
                break;
        }
    }

    void PlaySound(AudioClip[] clips, AudioSource source)
    {
        if (clips != null && clips.Length > 0)
        {
            source.clip = clips[Random.Range(0, clips.Length)];
            source.Play();
        }
    }
    
}
