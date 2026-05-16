using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[ExecuteAlways]
public class ObjectCurveMover : MonoBehaviour
{
    [SerializeField] private GameObject _prefabObjectMove;
    [SerializeField] private RectTransform _startPoint;
    [SerializeField] private RectTransform _endPoint;
    [SerializeField] private ParticleSystem _effect;
    [SerializeField] private float amplitude, frequency, duration;
    [SerializeField] private AnimationCurve _moveTimeCurve;
    [SerializeField] private AnimationCurve _scaleCurve;
    [SerializeField] private float _moveDuration = 2f;
    [SerializeField] private float _newObjectDuration = 0.1f;
    [SerializeField] private bool _useCallFinishObjectRate;
    [SerializeField] private float _callFinishObjectRate = 1f;
    private List<RectTransform> _controlPoints = new List<RectTransform>();
     private void Awake()
    {
        LoadAllNote();
    }
    public void StartMove(int count, Action onMoveFinished = null, Action onComplete = null)
    {
        StartCoroutine(IESpawnObjectMove(count, onMoveFinished, onComplete));
    }
    private IEnumerator IESpawnObjectMove(int count, Action onMoveFinished, Action onComplete)
    {
        Coroutine move = null;
        for (int i = 0; i < count; i++)
        {
            GameObject objectToMove = Instantiate(_prefabObjectMove, _startPoint.transform.position, Quaternion.identity, transform);
            move = StartCoroutine(IEMoveAlongCurve(objectToMove.transform, onMoveFinished));
            yield return new WaitForSeconds(_newObjectDuration);
        }
        yield return move;
        onComplete?.Invoke();
    }
    private IEnumerator IEMoveAlongCurve(Transform objectToMove, Action onMoveFinished)
    {
        float time = 0f;
        var scale = transform.localScale;
        var isFinished = false;
        while (time < _moveDuration)
        {
            float t = time / _moveDuration;
            Vector3 position = GetBezierPoint(_moveTimeCurve.Evaluate(t));
            objectToMove.position = position;
            objectToMove.localScale = scale  * _scaleCurve.Evaluate(t);
            time += Time.deltaTime;
            if (t >= _callFinishObjectRate && !isFinished && _useCallFinishObjectRate)
            {
                onMoveFinished?.Invoke();
                isFinished = true;
            }
            yield return null;
        }
        objectToMove.position = _endPoint.position;
        if(!_useCallFinishObjectRate) onMoveFinished?.Invoke();
        Destroy(objectToMove.gameObject);
        Instantiate(_effect, _endPoint.position, Quaternion.identity, transform);
    }
    private Vector3 GetBezierPoint(float t)
    {
        List<Vector3> points = new List<Vector3> { _startPoint.position };
        foreach (var cp in _controlPoints)
            points.Add(cp.position);
        points.Add(_endPoint.position);
        while (points.Count > 1)
        {
            List<Vector3> nextPoints = new List<Vector3>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 pt = Vector3.Lerp(points[i], points[i + 1], t);
                nextPoints.Add(pt);
            }
            points = nextPoints;
        }
        return points[0];
    }
    private void LoadAllNote()
    {
        _controlPoints.Clear();
        int childCount = transform.GetChild(0).childCount;
        for (int i = 0; i < childCount; i++)
            _controlPoints.Add(transform.GetChild(0).GetChild(i).transform as RectTransform);
    }
    private void OnDrawGizmos()
    {
        if (_startPoint == null || _endPoint == null) return;
        Gizmos.color = Color.red;
        Vector3 prevPoint = _startPoint.position;
        for (int i = 1; i <= 20; i++)
        {
            float t = i / (float)20;
            Vector3 point = GetBezierPoint(t);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
#if UNITY_EDITOR
    private void LateUpdate()
    {
        LoadAllNote();
    }
#endif
}
