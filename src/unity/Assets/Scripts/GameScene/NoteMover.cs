using Assets.Scripts;
using UnityEngine;

public class NoteMover : MonoBehaviour
{
    public float spawnTime;   // 相对歌曲时间
    public float hitTime;
    public Vector3 startPos;
    public Vector3 endPos;

    private float startDspTime;
    private bool isStarted;



    public void GameStart()
    {
        startDspTime = (float)AudioSettings.dspTime;
        isStarted = true;
    }

    void Awake()
    {
        float noteDuration = hitTime - spawnTime;
    }

    void Update()
    {
        if (!isStarted) return;

        float songTime = (float)AudioSettings.dspTime - startDspTime;

        if (songTime < spawnTime)
        {
            transform.position = startPos;
            return;
        }

        //float progress = (songTime - spawnTime) / (hitTime - spawnTime);

        //transform.position = startPos - new Vector3((startPos.x-endPos.x)*progress,0,0);

        float t = Mathf.InverseLerp(spawnTime, hitTime, songTime);
        transform.position = Vector3.Lerp(startPos, endPos, t);

        if (songTime > hitTime)
        {
            // Miss or auto hit
            Destroy(gameObject);
        }
    }
}