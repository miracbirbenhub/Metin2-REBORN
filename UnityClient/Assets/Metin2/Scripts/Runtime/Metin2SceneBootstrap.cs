using UnityEngine;

namespace Metin2Reborn
{
    public sealed class Metin2SceneBootstrap : MonoBehaviour
    {
        [SerializeField] private Metin2PlayerController player;
        [SerializeField] private Metin2FollowCamera followCamera;

        private void Start()
        {
            if (player == null) player = FindFirstObjectByType<Metin2PlayerController>();
            if (followCamera == null && Camera.main != null)
                followCamera = Camera.main.GetComponent<Metin2FollowCamera>();

            if (followCamera != null && player != null)
                followCamera.SetTarget(player.transform);

            if (player != null && Camera.main != null)
                player.SetCamera(Camera.main.transform);
        }
    }
}
