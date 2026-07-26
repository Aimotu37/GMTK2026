using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeakerZone : MonoBehaviour
{
    private SpeakerZoneFeedback feedback;

    public Transform speakerZoneTransform;   // 发声槽的位置（用于磁吸）
    public Collider2D speakerZoneCollider;   // 发声槽的碰撞体（用于检测）
    public LayerMask speakerZoneLayerMask;   // 或者用 Layer 检测

    private void OnEnable()
    {
        if (speakerZoneTransform == null)
            speakerZoneTransform = GetComponent<Transform>();
        if (speakerZoneCollider == null)
            speakerZoneCollider = GetComponent<Collider2D>();
        feedback = GetComponent<SpeakerZoneFeedback>();
    }

    public void PlayAcceptedDropFeedback()
    {
        feedback?.Play();
    }
}
