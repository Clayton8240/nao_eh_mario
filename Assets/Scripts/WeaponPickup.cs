// WeaponPickup.cs
// -----------------------------------------------------------------------------
// Coletável da arma. Ao tocar, o player troca para o estado "com arma".
// A arma funciona como escudo: o próximo dano remove a arma em vez de matar.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    [RequireComponent(typeof(Collider2D))]
    public class WeaponPickup : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController2D>();
            if (player == null) return;

            player.GiveWeapon();
            GameManager.Instance?.AddScore(25, transform.position);
            SfxPlayer.Instance?.PlayCoin();
            Destroy(gameObject);
        }
    }
}
