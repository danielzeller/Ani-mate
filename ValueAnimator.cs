using System;
using UnityEngine;
using UnityEngine.Events;

public enum AnimationType {
    POSITION,
    ROTATION,
    SCALE,
    FLOAT_VALUE
};

public enum RepeatMode {
    DESTROY,
    LOOP,
    PING_PONG
};

public class ValueAnimator : MonoBehaviour {
    public float from;
    public float to;

    public Vector3 toVec;
    public Vector3 fromVec;

    public float duration = 1f;
    public float delay;

    public bool autoStart;
    public AnimationType animationType = AnimationType.FLOAT_VALUE;
    public RepeatMode repeatMode = RepeatMode.DESTROY;
    [SerializeField] public BezierPathEasingCurve easingCurve = new BezierPathEasingCurve(x1: 0.5f, y1: 0.0f, x2: 0.5f, y2: 0.9f);

    private UpdateEvent updateEvent = new UpdateEvent();
    private UnityEvent endEvent = new UnityEvent();

    private bool reverse;

    private void Start() {
        if (autoStart) startAnimation();
    }

    public ValueAnimator create(float from, float to, UnityAction<float> updateFunction) {
        animationType = AnimationType.FLOAT_VALUE;
        updateEvent.AddListener(updateFunction);
        this.from = from;
        this.to = to;
        return this;
    }

    public ValueAnimator create(Vector3 from, Vector3 to, AnimationType type) {
        animationType = type;
        fromVec = from;
        toVec = to;
        return this;
    }

    public ValueAnimator withDuration(float duration) {
        this.duration = duration;
        return this;
    }

    public ValueAnimator withEasing(BezierPathEasingCurve easingCurve) {
        this.easingCurve = easingCurve;
        return this;
    }

    public ValueAnimator withDelay(float delay) {
        this.delay = delay;
        return this;
    }

    public ValueAnimator withEndAction(UnityAction action) {
        endEvent.AddListener(action);
        return this;
    }

    private float startTimeStamp = -1f;

    void cancel() {
        Destroy(this);
    }

    void Update() {
        if (startTimeStamp >= 0) {
            var now = Time.time;
            float startTimeWithDelay = startTimeStamp + delay;

            if (startTimeWithDelay < now) {
                var time = now - startTimeWithDelay;
                float percent = time / duration;
                if (reverse) percent = 1f - percent;

                if (percent > 1f && repeatMode == RepeatMode.DESTROY) {
                    finish();
                }
                else if (percent > 1f && repeatMode == RepeatMode.LOOP) {
                    startAnimation();
                }
                else if ((percent > 1 || percent < 0) && repeatMode == RepeatMode.PING_PONG) {
                    reverse = !reverse;
                    startAnimation();
                }
                else {
                    updateValues(percent);
                }
            }
        }
    }

    private void updateValues(float percent) {
        var easedTime = easingCurve.evaluate(percent);
        if (animationType == AnimationType.FLOAT_VALUE) {
            var animatedValue = @from + (to - @from) * easedTime;
            updateEvent.Invoke(animatedValue);
        }
        else if (animationType == AnimationType.POSITION) {
            transform.position = fromVec + (toVec - fromVec) * easedTime;
        }
        else if (animationType == AnimationType.ROTATION) {
            transform.eulerAngles = fromVec + (toVec - fromVec) * easedTime;
        }
        else if (animationType == AnimationType.SCALE) {
            transform.localScale = fromVec + (toVec - fromVec) * easedTime;
        }
    }

    public void startAnimation() {
        startTimeStamp = Time.time;
    }

    private void finish() {
        updateEvent.Invoke(to);
        endEvent.Invoke();
        cancel();
    }
}

[Serializable]
public class BezierPathEasingCurve {
    [SerializeField, HideInInspector] public Vector2 handle1 = new Vector3(0.5f, 0.5f, 0);
    [SerializeField, HideInInspector] public Vector2 handle2 = new Vector3(0.5f, 0.5f, 0);
    [SerializeField, HideInInspector] public Vector2[] points;
    private const int NUMBER_OF_POINTS = 100;

    public BezierPathEasingCurve(double x1, double y1, double x2, double y2) {
        handle1.x = (float) x1;
        handle1.y = (float) y1;
        handle2.x = (float) x2;
        handle2.y = (float) y2;
        recreate();
    }

    public BezierPathEasingCurve(float x1, float y1, float x2, float y2) {
        handle1.x = x1;
        handle1.y = y1;
        handle2.x = x2;
        handle2.y = y2;
        recreate();
    }

    public void recreate() {
        points = new Vector2[NUMBER_OF_POINTS];
        for (int i = 0; i < NUMBER_OF_POINTS-1; i++) {
            points[i] = cubicCurvePoint(
                (float) i / 100.0f,
                p0: new Vector2(0, 0),
                c1: new Vector2(x: handle1.x, y: handle1.y),
                c2: new Vector2(x: handle2.x, y: handle2.y),
                p1: new Vector2(x: 1, y: 1)
            );
        }

        points[99] = Vector2.one;
    }

    private Vector2 cubicCurvePoint(float t, Vector2 p0, Vector2 c1, Vector2 c2, Vector2 p1) {
        var x = cubicCurveValue(t, p0.x, c1.x, c2.x, p1.x);
        var y = cubicCurveValue(t, p0.y, c1.y, c2.y, p1.y);
        return new Vector2(x, y);
    }

    float cubicCurveValue(float t, float p0, float c1, float c2, float p1) {
        float value = Mathf.Pow(1f - t, 3f) * p0;
        value += 3f * Mathf.Pow(1f - t, 2f) * t * c1;
        value += 3f * (1f - t) * Mathf.Pow(t, 2f) * c2;
        value += Mathf.Pow(t, 3) * p1;
        return value;
    }

    public float evaluate(float t) {
        for (int index = 0; index < points.Length; index++) {
            var item = points[index];
            if (item.x > t) {
                var rangeX = item.x - points[index - 1].x;
                var currentValue = t - points[index - 1].x;
                var percent = currentValue / rangeX;
                var rangeY = item.y - points[index - 1].y;
                return points[index - 1].y + rangeY * percent;
            }
        }

        return 1;
    }
}

[Serializable]
public class UpdateEvent : UnityEvent<float> { }