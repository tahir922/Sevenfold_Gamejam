using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sevenfold.Dice
{
    /// <summary>
    /// Presentation component for a 3D physical graybox die.
    /// Animates rolling (lift, tumble spin, and landing bounce) and ensures that
    /// the final resting rotation aligns the rolled face exactly facing UP (+Y).
    /// </summary>
    [SelectionBase]
    public class DiceView : MonoBehaviour
    {
        [Header("Positioning & State")]
        [SerializeField] private Vector3 _restPosition;
        [SerializeField] private Vector3 _spinAxis = new Vector3(1.3f, 2.7f, 0.9f);
        [SerializeField] private int _currentFace = 1;

        private Coroutine _animCoroutine;
        private bool _isAnimating;

        public bool IsAnimating => _isAnimating;
        public int CurrentFace => _currentFace;
        public Vector3 SpinAxis
        {
            get => _spinAxis;
            set => _spinAxis = value;
        }
        public Vector3 RestPosition
        {
            get => _restPosition;
            set => _restPosition = value;
        }

        // Standard die face normal in local space
        private static readonly Dictionary<int, Vector3> FaceNormals = new Dictionary<int, Vector3>
        {
            { 1, Vector3.up },
            { 2, Vector3.forward },
            { 3, Vector3.right },
            { 4, Vector3.left },
            { 5, Vector3.back },
            { 6, Vector3.down }
        };

        // Static rotation table: mapping face value to the rotation that aligns that face with +Y (Vector3.up)
        private static readonly Dictionary<int, Quaternion> FaceRotations = new Dictionary<int, Quaternion>
        {
            { 1, Quaternion.identity },
            { 2, Quaternion.Euler(-90f, 0f, 0f) },
            { 3, Quaternion.Euler(0f, 0f, 90f) },
            { 4, Quaternion.Euler(0f, 0f, -90f) },
            { 5, Quaternion.Euler(90f, 0f, 0f) },
            { 6, Quaternion.Euler(180f, 0f, 0f) }
        };

        public static Quaternion GetFaceUpRotation(int value)
        {
            if (FaceRotations.TryGetValue(value, out var rot))
            {
                return rot;
            }
            return Quaternion.identity;
        }

        public static Vector3 GetFaceNormal(int value)
        {
            if (FaceNormals.TryGetValue(value, out var normal))
            {
                return normal;
            }
            return Vector3.up;
        }

        private void Awake()
        {
            if (_restPosition == Vector3.zero && transform.localPosition != Vector3.zero)
            {
                _restPosition = transform.localPosition;
            }
        }

        private void OnDisable()
        {
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }
            _isAnimating = false;
            transform.localPosition = _restPosition;
            transform.localRotation = GetFaceUpRotation(_currentFace);
        }

        /// <summary>
        /// Immediately snaps the die to show the specified face pointing up.
        /// </summary>
        public void SetFaceImmediate(int value)
        {
            if (value < 1 || value > 6)
            {
                value = Mathf.Clamp(value, 1, 6);
            }

            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }

            _isAnimating = false;
            _currentFace = value;
            transform.localPosition = _restPosition;
            transform.localRotation = GetFaceUpRotation(value);
        }

        /// <summary>
        /// Starts the roll animation on this MonoBehaviour and tracks its active Coroutine.
        /// </summary>
        public Coroutine AnimateToFace(int value, float duration)
        {
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }
            _animCoroutine = StartCoroutine(AnimateTo(value, duration));
            return _animCoroutine;
        }

        /// <summary>
        /// Animates the die rolling: lifts up, tumbles vigorously, lands with a bounce,
        /// and comes to rest with the face for 'value' pointing UP (+Y).
        /// </summary>
        public IEnumerator AnimateTo(int value, float duration)
        {
            if (value < 1 || value > 6)
            {
                Debug.LogWarning($"[DiceView] Invalid die value {value}, clamping to [1, 6].");
                value = Mathf.Clamp(value, 1, 6);
            }

            if (_restPosition == Vector3.zero && transform.localPosition != Vector3.zero)
            {
                _restPosition = transform.localPosition;
            }

            _currentFace = value;
            Quaternion targetRotation = GetFaceUpRotation(value);
            Vector3 basePos = _restPosition;

            if (duration <= 0f || !Application.isPlaying)
            {
                transform.localPosition = basePos;
                transform.localRotation = targetRotation;
                _isAnimating = false;
                yield break;
            }

            _isAnimating = true;
            Quaternion startRot = transform.localRotation;
            Vector3 spinAxis = (_spinAxis.sqrMagnitude > 0.001f) ? _spinAxis.normalized : new Vector3(1.3f, 2.7f, 0.9f).normalized;
            float totalSpinDegrees = 720f;
            float peakLift = 1.0f;
            float elapsed = 0f;
            Quaternion tumbleEndRot = startRot * Quaternion.AngleAxis(totalSpinDegrees, spinAxis);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Parabolic bounce with secondary settle bounce
                float currentY;
                if (t < 0.72f)
                {
                    float subT = t / 0.72f;
                    currentY = basePos.y + 4f * peakLift * subT * (1f - subT);
                }
                else
                {
                    float subT = (t - 0.72f) / 0.28f;
                    currentY = basePos.y + 4f * (peakLift * 0.18f) * subT * (1f - subT);
                }

                transform.localPosition = new Vector3(basePos.x, currentY, basePos.z);

                // Rotation: tumble spin in initial phase, slerp to target orientation in descent
                if (t < 0.55f)
                {
                    float spinT = t / 0.55f;
                    transform.localRotation = startRot * Quaternion.AngleAxis(totalSpinDegrees * spinT, spinAxis);
                }
                else
                {
                    float blendT = (t - 0.55f) / 0.45f;
                    float smooth = blendT * blendT * (3f - 2f * blendT); // SmoothStep
                    transform.localRotation = Quaternion.Slerp(tumbleEndRot, targetRotation, smooth);
                }

                yield return null;
            }

            // Guaranteed exact resting state
            transform.localPosition = basePos;
            transform.localRotation = targetRotation;
            _isAnimating = false;
            _animCoroutine = null;
        }
    }
}
