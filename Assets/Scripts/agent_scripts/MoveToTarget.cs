using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace agent_scripts
{
    public class MoveToTarget : Agent
    {
        public Transform targetTransform;
        public MeshRenderer floorRenderer;
        public Material winMaterial;
        public Material loseMaterial;
        public float moveSpeed = 5f;

        public override void OnEpisodeBegin()
        {
            // Agente: posição aleatória numa metade da arena
            transform.localPosition = new Vector3(
                Random.Range(-3.5f, 3.5f), 0.5f, Random.Range(-3.5f, -0.5f));

            // Alvo: posição aleatória na outra metade
            targetTransform.localPosition = new Vector3(
                Random.Range(-3.5f, 3.5f), 0.5f, Random.Range(0.5f, 3.5f));
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            var dir = (targetTransform.localPosition - transform.localPosition).normalized;
            var dist = Vector3.Distance(targetTransform.localPosition, transform.localPosition) / 10f;
            sensor.AddObservation(dir); // 3 floats
            sensor.AddObservation(dist); // 1 float
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0]; // entre -1 e +1
            float moveZ = actions.ContinuousActions[1]; // entre -1 e +1
            transform.position +=
                new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Target"))
            {
                SetReward(+1f); // sucesso!
                floorRenderer.material = winMaterial;
            }

            if (other.CompareTag("Wall"))
            {
                SetReward(-1f); // falha
                floorRenderer.material = loseMaterial;
            }

            EndEpisode(); // reset
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var ca = actionsOut.ContinuousActions;
            ca[0] = Input.GetAxis("Horizontal");
            ca[1] = Input.GetAxis("Vertical");
        }
    }
}