using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class KmsMonsterLegSwing : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer legRenderer;
        [SerializeField] private SpriteRenderer leg2Renderer;

        private Rigidbody2D body;
        private Vector3 legRestPosition;
        private Vector3 leg2RestPosition;
        private float swingAmplitude = 0.08f;
        private float swingSpeed = 8f;
        private float returnSpeed = 10f;
        private float swingPhase;
        private bool isConfigured;

        public bool IsSwinging { get; private set; }
        public float CurrentWorldOffset { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            CaptureRestPositions();
        }

        private void OnDisable()
        {
            ResetImmediate();
        }

        private void Update()
        {
            if (!isConfigured || body == null)
            {
                return;
            }

            bool isMoving = body.linearVelocity.sqrMagnitude > 0.0001f;
            if (isMoving)
            {
                swingPhase += Time.deltaTime * swingSpeed;
                CurrentWorldOffset = Mathf.Sin(swingPhase) * swingAmplitude;
                float visualScale = visualRoot != null
                    ? Mathf.Abs(visualRoot.lossyScale.x)
                    : 1f;
                float localOffset = CurrentWorldOffset / Mathf.Max(0.0001f, visualScale);

                legRenderer.transform.localPosition =
                    legRestPosition + new Vector3(localOffset, 0f, 0f);
                leg2Renderer.transform.localPosition =
                    leg2RestPosition + new Vector3(-localOffset, 0f, 0f);
                IsSwinging = true;
                return;
            }

            swingPhase = 0f;
            CurrentWorldOffset = 0f;
            float interpolation = Mathf.Clamp01(Time.deltaTime * returnSpeed);
            legRenderer.transform.localPosition = Vector3.Lerp(
                legRenderer.transform.localPosition,
                legRestPosition,
                interpolation);
            leg2Renderer.transform.localPosition = Vector3.Lerp(
                leg2Renderer.transform.localPosition,
                leg2RestPosition,
                interpolation);
            IsSwinging = false;
        }

        public void Configure(KmsMonsterData data)
        {
            if (data == null || legRenderer == null || leg2Renderer == null)
            {
                DisableSeparatedLegs();
                return;
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            CaptureRestPositions();
            legRenderer.sprite = data.LegSprite;
            leg2Renderer.sprite = data.Leg2Sprite;
            legRenderer.color = data.Color;
            leg2Renderer.color = data.Color;
            legRenderer.enabled = data.LegSprite != null;
            leg2Renderer.enabled = data.Leg2Sprite != null;
            swingAmplitude = data.LegSwingAmplitude;
            swingSpeed = data.LegSwingSpeed;
            returnSpeed = data.LegReturnSpeed;
            isConfigured = data.UsesSeparatedLegs;
            ResetImmediate();
        }

        public void SetColor(Color color)
        {
            if (legRenderer != null)
            {
                legRenderer.color = color;
            }

            if (leg2Renderer != null)
            {
                leg2Renderer.color = color;
            }
        }

        public void ResetForPool(Color fallbackColor)
        {
            ResetImmediate();
            isConfigured = false;
            if (legRenderer != null)
            {
                legRenderer.sprite = null;
                legRenderer.color = fallbackColor;
                legRenderer.enabled = false;
            }

            if (leg2Renderer != null)
            {
                leg2Renderer.sprite = null;
                leg2Renderer.color = fallbackColor;
                leg2Renderer.enabled = false;
            }
        }

        public void ResetImmediate()
        {
            swingPhase = 0f;
            CurrentWorldOffset = 0f;
            IsSwinging = false;
            if (legRenderer != null)
            {
                legRenderer.transform.localPosition = legRestPosition;
            }

            if (leg2Renderer != null)
            {
                leg2Renderer.transform.localPosition = leg2RestPosition;
            }
        }

        private void CaptureRestPositions()
        {
            if (legRenderer != null)
            {
                legRestPosition = legRenderer.transform.localPosition;
            }

            if (leg2Renderer != null)
            {
                leg2RestPosition = leg2Renderer.transform.localPosition;
            }
        }

        private void DisableSeparatedLegs()
        {
            ResetForPool(Color.white);
        }
    }
}
