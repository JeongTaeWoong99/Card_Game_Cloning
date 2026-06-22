using System.Collections.Generic;
using UnityEngine;

// 손패 카드를 좌/우 기준점 사이에 부채꼴(곡선)로 배치하기 위한 PRS 목록을 계산한다.
// 위치 계산만 담당하는 순수 헬퍼이며, 실제 이동(MoveTransform)은 호출부에서 수행한다.
public static class CardLayout
{
    // 1~3장은 평평한 고정 간격, 4장 이상부터 height 곡률과 회전을 적용한다
    public static List<PRS> GetHandPRS(Transform leftTr, Transform rightTr, int objCount, float height, Vector3 scale)
    {
        float[]   objLerps = new float[objCount];
        List<PRS> results  = new List<PRS>(objCount);

        switch (objCount)
        {
            case 1: objLerps = new float[] { 0.5f };               break;
            case 2: objLerps = new float[] { 0.27f, 0.73f };       break;
            case 3: objLerps = new float[] { 0.1f, 0.5f, 0.9f };   break;
            default:
                float interval = 1f / (objCount - 1);
                for (int i = 0; i < objCount; i++)
                {
                    objLerps[i] = interval * i;
                }
                break;
        }

        for (int i = 0; i < objCount; i++)
        {
            var targetPos = Vector3.Lerp(leftTr.position, rightTr.position, objLerps[i]);
            var targetRot = Utils.QI;

            if (objCount >= 4)
            {
                // 원의 방정식으로 가운데가 볼록(또는 오목)한 곡선 y 오프셋을 만든다
                float curve = Mathf.Sqrt(Mathf.Pow(height, 2) - Mathf.Pow(objLerps[i] - 0.5f, 2));
                curve = height >= 0 ? curve : -curve;
                targetPos.y += curve;
                targetRot = Quaternion.Slerp(leftTr.rotation, rightTr.rotation, objLerps[i]);
            }

            results.Add(new PRS(targetPos, targetRot, scale));
        }

        return results;
    }
}
