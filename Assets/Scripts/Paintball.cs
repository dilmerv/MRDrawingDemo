using Meta.XR.ImmersiveDebugger;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Unity.Netcode.NetworkObject))]
public class Paintball : NetworkBehaviour
{
    private const string SceneMeshTag = "SceneMesh";
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    public static int InstanceCount { get; private set; }

    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0.0f, Max = 10.0f)]
    private static float _forceThreshold = 2.0f;
    private static float _saturationMin = 0.7f;
    private static float _saturationMax = 0.9f;
    private static float _valueMin = 0.7f;
    private static float _valueMax = 0.9f;

    [SerializeField] private float collisionCooldownInS = 2.0f;
    [SerializeField] private GameObject splatterGameObject;

    private readonly NetworkVariable<Color> _color = new();
    private double _spawnTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server decides properties
            _color.Value = Random.ColorHSV(0.0f, 1.0f, _saturationMin, _saturationMax, _valueMin, _valueMax, 1.0f, 1.0f);
            _spawnTime = NetworkManager.ServerTime.Time;
        }

        // Setup ball material
        var material = GetComponent<Renderer>().material; // This instantiates a new material
        material.SetColor(ColorProperty, _color.Value);

        // Setup fake light material
        var projector = GetComponentInChildren<DecalProjector>();
        projector.material = new Material(projector.material)
        {
            color = _color.Value
        };

        // Update the static count of balls
        InstanceCount++;
    }

    public override void OnNetworkDespawn()
    {
        // Update the static count of balls
        InstanceCount--;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner)
        {
            // Ignore collisions if not owner of the ball
            return;
        }

        var elapsedTime = NetworkManager.ServerTime.Time - _spawnTime;
        if (elapsedTime < collisionCooldownInS)
        {
            // Ignore collisions during an initial cooldown
            return;
        }

        if (!collision.collider.CompareTag(SceneMeshTag))
        {
            // Filter out non Scene Mesh collisions
            return;
        }

        var force = collision.impulse.magnitude;
        if (force < _forceThreshold)
        {
            // weak collision won't cause the paintball to explode
            return;
        }

        // Hide ball on collision, TODO: add explosion FX
        var rendererComponent = GetComponent<Renderer>();
        rendererComponent.enabled = false;

        // Generate Splatter and Explode, as RPC
        var contact = collision.GetContact(0);
        ExplodeServerRpc(contact.point, contact.normal, force);
    }

    [ServerRpc]
    private void ExplodeServerRpc(Vector3 position, Vector3 normal, float force)
    {
        // Spawn a splatter
        var splatterObject = Instantiate(splatterGameObject, position, Quaternion.LookRotation(-normal));

        // Initialize the splatter properties
        var splatter = splatterObject.GetComponent<Splatter>();
        splatter.InitialColor = _color.Value;
        splatter.InitialForce = force;
        splatterObject.GetComponent<NetworkObject>().Spawn();

        // Destroy the paintball
        NetworkObject.Despawn();
    }
}
