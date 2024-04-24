using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class NetEnergyOrb : MonoBehaviour
{
    /// <summary>
    /// 발사 초기 속도
    /// </summary>
    public float speed = 10.0f;

    /// <summary>
    /// 수명
    /// </summary>
    public float lifeTime = 20.0f;

    /// <summary>
    /// 폭발 범위
    /// </summary>
    public float explosionRadius = 5.0f;

    Rigidbody rigid;
    VisualEffect effect;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        effect = GetComponent<VisualEffect>();
    }

    private void Start()
    {
        transform.Rotate(-30.0f, 0.0f, 0.0f);
        rigid.velocity = speed * transform.forward;
        Destroy(this.gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider[] result = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Player"));

        if (result.Length > 0)
        {
            foreach (Collider col in result)
            {
                Debug.Log(col.gameObject.name);
            }
        }

        StartCoroutine(EffectFinishProcess());
    }

    IEnumerator EffectFinishProcess()
    {
        yield return null;
    }
}

/// 실습_240423
/// EffectFinishProcess() 구현하기
/// ----------------------------------
/// 0.5초 동안 explosionRadius까지 확대
/// 1초 동안 baseSize가 0이 될 때까지 축소
/// 파티클 생성 중지
/// 파티클 개수가 0이 되면 게임 오브젝트 제거
/// ----------------------------------
/// effect.SetFloat("BaseSize", radius);    // 변수 변경하기 (타입에 맞게 해야함)
/// effect.SendEvent("OnEffectFinish");     // 이벤트 날리기
/// effect.aliveParticleCount;              // 남아있는 파티클 개수

/*
float timer;
private void Update()
{
    timer = Time.deltaTime;
}

IEnumerator EffectFinishProcess()
{
    float radius = 0.0f;
    int baseSize = Shader.PropertyToID("BaseSize");

    timer = 0.0f;
    while (timer < 0.5f)
    {
        radius = explosionRadius * timer * 2;
        effect.SetFloat(baseSize, radius);
    }

    timer = 0.0f;
    while (timer < 1.0f)
    {
        radius -= explosionRadius * timer;
        effect.SetFloat(baseSize, radius);
    }

    effect.SendEvent("OnEffectFinish");
    if (effect.aliveParticleCount == 0)
    {
        Destroy(this.gameObject);
    }

    yield return null;
}
 */
