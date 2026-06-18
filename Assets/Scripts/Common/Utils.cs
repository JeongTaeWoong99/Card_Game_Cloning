using System.Collections.Generic;
using UnityEngine;

// 위치·회전·스케일을 한 묶음으로 전달하기 위한 데이터 홀더
[System.Serializable]
public class PRS
{
    public Vector3 pos;
    public Quaternion rot;
    public Vector3 scale;

    public PRS(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        this.pos = pos;
        this.rot = rot;
        this.scale = scale;
    }
}

public static class Utils
{
    public static Quaternion QI => Quaternion.identity;

    // Camera.main 은 내부적으로 태그 검색을 수행하므로 캐싱한다 (드래그 중 매 프레임 호출됨)
    private static Camera _mainCamera;
    private static Camera MainCamera
    {
        get
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            return _mainCamera;
        }
    }

    public static Vector3 MousePos
    {
        get
        {
            Vector3 result = MainCamera.ScreenToWorldPoint(Input.mousePosition);
            result.z = -10f;
            return result;
        }
    }

    // Fisher-Yates 셔플 — 리스트를 제자리에서 무작위로 섞는다 O(n)
    public static void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
