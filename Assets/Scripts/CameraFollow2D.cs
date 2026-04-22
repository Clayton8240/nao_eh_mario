// CameraFollow2D.cs
// -----------------------------------------------------------------------------
// Câmera lateral que segue o player. Uso Lerp pra suavizar o movimento
// (a câmera não "gruda" no player, ela "persegue" suavemente).
//
// Tem um clamp em X pra ela não passar do começo nem do fim do nível
// (senão a gente veria o "vazio" depois da meta).
//
// LateUpdate ao invés de Update: roda DEPOIS de todos os Updates do frame,
// então a posição do player já tá atualizada quando a câmera vai pegar.
// É uma boa pratica que aprendi nos tutoriais oficiais da Unity.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace NaoEMario
{
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public float smooth = 5f;                       // velocidade do lerp (maior = mais grudado)
        public Vector2 offset = new Vector2(2f, 1.5f);  // câmera fica um pouco à frente e acima
        public float minX = -5f;
        public float maxX = 200f;

        private void LateUpdate()
        {
            if (target == null) return;

            // Posição que QUEREMOS estar
            Vector3 desired = new Vector3(
                Mathf.Clamp(target.position.x + offset.x, minX, maxX),
                target.position.y + offset.y,
                transform.position.z); // mantém o Z (orthographic câm precisa de z negativo)

            // Lerp = interpolação linear: se aproxima do destino a cada frame
            transform.position = Vector3.Lerp(transform.position, desired,
                                              Time.deltaTime * smooth);
        }
    }
}
