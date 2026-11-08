using Sevenfold.Core;
using UnityEngine;

namespace Sevenfold.Board
{
    /// <summary>
    /// Computes and adjusts the Camera's orthographicSize dynamically based on
    /// board dimensions, aspect ratio, and HUD margin margins.
    /// Ensures the board occupies approximately 70% of view width at 16:9 while safely
    /// fitting vertical extents across any display aspect ratio.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class CameraFramer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private BoardDefinitionSO _board;

        [Header("Framing Settings")]
        [Tooltip("Target fraction of camera view width occupied by board width at 16:9.")]
        [Range(0.4f, 0.95f)]
        [SerializeField] private float _hudMarginFactor = 0.70f;

        [Tooltip("Maximum fraction of camera view height allowed for board extents.")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float _verticalMaxCoverage = 0.85f;

        public Camera CameraReference
        {
            get => _camera;
            set => _camera = value;
        }

        public BoardDefinitionSO BoardDefinition
        {
            get => _board;
            set => _board = value;
        }

        public float HudMarginFactor
        {
            get => _hudMarginFactor;
            set => _hudMarginFactor = value;
        }

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }
        }

        private void Start()
        {
            RecalculateFraming();
        }

        private void OnValidate()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }
            RecalculateFraming();
        }

        /// <summary>
        /// Recalculates and applies the optimal orthographicSize to frame the board.
        /// </summary>
        public void RecalculateFraming()
        {
            if (_camera == null || _board == null) return;
            if (!_camera.orthographic) return;

            float halfBoardWidth = ((_board.Width - 1) * _board.CellSpacing + _board.TileTopSize) * 0.5f;
            float halfBoardHeight = ((_board.Height - 1) * _board.CellSpacing + _board.TileTopSize) * 0.5f;

            // Frame all 4 corners, base thickness, plus the tall gate arch
            Vector3[] boundingPoints = new[]
            {
                new Vector3(-halfBoardWidth, -0.3f, -halfBoardHeight),
                new Vector3(halfBoardWidth, -0.3f, -halfBoardHeight),
                new Vector3(halfBoardWidth, -0.3f, halfBoardHeight),
                new Vector3(-halfBoardWidth, -0.3f, halfBoardHeight),
                new Vector3(-halfBoardWidth, 0.0f, -halfBoardHeight),
                new Vector3(halfBoardWidth, 0.0f, -halfBoardHeight),
                new Vector3(halfBoardWidth, 0.0f, halfBoardHeight),
                new Vector3(-halfBoardWidth, 0.0f, halfBoardHeight),
                // Tall gate archway extents at upper-right cell
                new Vector3(halfBoardWidth, 1.5f, halfBoardHeight)
            };

            Quaternion camRot = _camera.transform.rotation;
            Quaternion invRot = Quaternion.Inverse(camRot);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < boundingPoints.Length; i++)
            {
                Vector3 localInCam = invRot * boundingPoints[i];
                if (localInCam.x < minX) minX = localInCam.x;
                if (localInCam.x > maxX) maxX = localInCam.x;
                if (localInCam.y < minY) minY = localInCam.y;
                if (localInCam.y > maxY) maxY = localInCam.y;
            }

            float extentX = maxX - minX;
            float extentY = maxY - minY;

            float aspect = _camera.aspect > 0.01f ? _camera.aspect : (16f / 9f);

            // Compute size needed to satisfy width HUD margin (~70% width)
            // Account for any camera-center asymmetry so neither side is clipped
            float halfWidth = Mathf.Max(extentX * 0.5f, Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX)));
            float requiredHalfWidth = halfWidth / _hudMarginFactor;
            float sizeFromWidth = requiredHalfWidth / aspect;

            // Compute size needed to satisfy height margin
            float halfHeight = Mathf.Max(extentY * 0.5f, Mathf.Max(Mathf.Abs(minY), Mathf.Abs(maxY)));
            float sizeFromHeight = halfHeight / _verticalMaxCoverage;

            _camera.orthographicSize = Mathf.Max(sizeFromWidth, sizeFromHeight);
        }
    }
}
