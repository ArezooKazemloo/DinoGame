
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    // One interactable can have several child colliders. Track each collider so
    // the obstacle triggers once on contact and becomes available again only
    // after every collider has left the player.
    private readonly Dictionary<Interactable, HashSet<Collider2D>> activeContacts = new Dictionary<Interactable, HashSet<Collider2D>>();
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryInteract(other);
    }

    // Stay callbacks recover contacts missed during a low frame or physics tick.
    private void OnTriggerStay2D(Collider2D other) => TryInteract(other);

    private void OnTriggerExit2D(Collider2D other) => RemoveContact(other);

    private void OnCollisionEnter2D(Collision2D collision) => TryInteract(collision.collider);

    private void OnCollisionStay2D(Collision2D collision) => TryInteract(collision.collider);

    private void OnCollisionExit2D(Collision2D collision) => RemoveContact(collision.collider);

    private void TryInteract(Collider2D other)
    {
        if (other == null || playerHealth == null) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.RunState.Playing) return;

        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable == null) interactable = other.GetComponentInParent<Interactable>();
        if (interactable == null) interactable = other.GetComponentInChildren<Interactable>(true);
        if (interactable == null) return;

        if (!activeContacts.TryGetValue(interactable, out HashSet<Collider2D> colliders))
        {
            colliders = new HashSet<Collider2D>();
            activeContacts.Add(interactable, colliders);
        }
        if (!colliders.Add(other)) return;
        if (colliders.Count > 1) return;
        interactable.Interact(playerHealth);
    }

    private void RemoveContact(Collider2D other)
    {
        if (other == null) return;
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable == null) interactable = other.GetComponentInParent<Interactable>();
        if (interactable == null) interactable = other.GetComponentInChildren<Interactable>(true);
        if (interactable == null) return;
        if (!activeContacts.TryGetValue(interactable, out HashSet<Collider2D> colliders)) return;
        colliders.Remove(other);
        if (colliders.Count == 0) activeContacts.Remove(interactable);
    }

    private void OnDisable() => activeContacts.Clear();
}
