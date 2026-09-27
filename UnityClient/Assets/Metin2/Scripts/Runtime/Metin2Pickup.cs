using UnityEngine;

namespace Metin2Reborn
{
    [RequireComponent(typeof(Collider))]
    public sealed class Metin2Pickup : MonoBehaviour
    {
        [SerializeField] private string itemId = "Yang";
        [SerializeField] private int amount = 1;
        [SerializeField] private float pickupRange = 1.8f;
        [SerializeField] private float rotateSpeed = 90f;

        private Transform player;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            if (player == null)
            {
                GameObject go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
                return;
            }

            if (Vector3.Distance(transform.position, player.position) <= pickupRange)
            {
                Metin2Inventory inventory = player.GetComponent<Metin2Inventory>();
                if (inventory != null) inventory.Add(itemId, amount);
                Destroy(gameObject);
            }
        }
    }
}
