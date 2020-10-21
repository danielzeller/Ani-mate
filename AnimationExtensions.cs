using UnityEngine;

public static class AnimationExtensions { 
    public static ValueAnimator Animate(this GameObject trans) {
        ValueAnimator animator = trans.AddComponent<ValueAnimator>();
        return animator;
    }
}