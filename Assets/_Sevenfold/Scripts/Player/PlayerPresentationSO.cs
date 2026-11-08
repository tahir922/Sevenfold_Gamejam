using UnityEngine;

namespace Sevenfold.Player
{
    [CreateAssetMenu(fileName = "PlayerPresentation", menuName = "Sevenfold/Player/Player Presentation")]
    public class PlayerPresentationSO : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private float _moveDuration = 0.22f;
        [SerializeField] private float _hopHeight = 0.12f;

        [Header("Landing Squash")]
        [SerializeField] private float _landingSquash = 0.1f;
        [SerializeField] private float _landingDuration = 0.1f;

        [Header("Idle Bob")]
        [SerializeField] private float _idleBobAmount = 0.03f;
        [SerializeField] private float _idleBobSpeed = 1.5f;

        public float MoveDuration
        {
            get => _moveDuration;
            set => _moveDuration = value;
        }

        public float HopHeight
        {
            get => _hopHeight;
            set => _hopHeight = value;
        }

        public float LandingSquash
        {
            get => _landingSquash;
            set => _landingSquash = value;
        }

        public float LandingDuration
        {
            get => _landingDuration;
            set => _landingDuration = value;
        }

        public float IdleBobAmount
        {
            get => _idleBobAmount;
            set => _idleBobAmount = value;
        }

        public float IdleBobSpeed
        {
            get => _idleBobSpeed;
            set => _idleBobSpeed = value;
        }
    }
}
