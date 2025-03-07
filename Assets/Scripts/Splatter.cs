using System;
using Meta.XR.ImmersiveDebugger;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Unity.Netcode.NetworkObject))]
public class Splatter : NetworkBehaviour
{
    private struct DecalConfig : INetworkSerializable, IEquatable<DecalConfig>
    {
        public Color Color;
        public float ScaleModifier;
        public float SplatterTime;
        public float SpawnTime;
        public float TargetDisturbance;
        public float TargetSize;
        public float Noise;
        public Vector4 NoiseSeed;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Color);
                reader.ReadValueSafe(out ScaleModifier);
                reader.ReadValueSafe(out SplatterTime);
                reader.ReadValueSafe(out SpawnTime);
                reader.ReadValueSafe(out TargetDisturbance);
                reader.ReadValueSafe(out TargetSize);
                reader.ReadValueSafe(out Noise);
                reader.ReadValueSafe(out NoiseSeed);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Color);
                writer.WriteValueSafe(ScaleModifier);
                writer.WriteValueSafe(SplatterTime);
                writer.WriteValueSafe(SpawnTime);
                writer.WriteValueSafe(TargetDisturbance);
                writer.WriteValueSafe(TargetSize);
                writer.WriteValueSafe(Noise);
                writer.WriteValueSafe(NoiseSeed);
            }
        }

        public bool Equals(DecalConfig other)
        {
            return Mathf.Approximately(SplatterTime, other.SplatterTime) &&
                   Mathf.Approximately(ScaleModifier, other.ScaleModifier) &&
                   Mathf.Approximately(SpawnTime, other.SpawnTime) &&
                   Mathf.Approximately(TargetDisturbance, other.TargetDisturbance) &&
                   Mathf.Approximately(TargetSize, other.TargetSize) &&
                   Mathf.Approximately(Noise, other.Noise) &&
                   Vector4.Distance(NoiseSeed, other.NoiseSeed) < 0.001f &&
                   Color == other.Color;
        }
    }

    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0.0f, Max = 600.0f)]
    private static float _lifetime = 200;

    public static float NoiseSeedOffsetMin = -100.0f;
    public static float NoiseSeedOffsetMax = 100.0f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 1.0f)] public static float SplatterTimeMin = 0.1f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 1.0f)] public static float SplatterTimeMax = 0.2f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 0.5f)] public static float SizeMin = 0.2f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 0.5f)] public static float SizeMax = 0.35f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 2.0f)] public static float ScaleMin = 0.8f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 2.0f)] public static float ScaleMax = 2.0f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 50.0f)] public static float DisturbanceMin = 0.2f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 50.0f)] public static float DisturbanceMax = 0.4f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 50.0f)] public static float NoiseMin = 5f;
    [DebugMember(Category = "Paintball", Tweakable = true, Min = 0f, Max = 50.0f)] public static float NoiseMax = 30f;

    private static readonly int SeedProperty = Shader.PropertyToID("_Seed");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int NoiseProperty = Shader.PropertyToID("_Noise");
    private static readonly int SizeProperty = Shader.PropertyToID("_Size");
    private static readonly int DisturbanceProperty = Shader.PropertyToID("_Disturbance");
    private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");

    public Color InitialColor { get; set; }
    public float InitialForce { get; set; }

    private Material _material;
    private readonly NetworkVariable<DecalConfig> _config = new();

    public override void OnNetworkSpawn()
    {
        // Assign a unique material
        var decal = GetComponent<DecalProjector>();
        _material = new Material(decal.material);
        decal.material = _material;

        if (IsServer)
        {
            // Authority prepares the decal properties
            _config.Value = new DecalConfig
            {
                Color = InitialColor,
                SpawnTime = NetworkManager.ServerTime.TimeAsFloat,
                SplatterTime = Random.Range(SplatterTimeMin, SplatterTimeMax),
                TargetDisturbance = Random.Range(DisturbanceMin, DisturbanceMax),
                TargetSize = Random.Range(SizeMin, SizeMax),
                Noise = Random.Range(NoiseMin, NoiseMax),
                ScaleModifier = InitialForce * Random.Range(ScaleMin, ScaleMax),
                NoiseSeed = new Vector4(
                    Random.Range(-NoiseSeedOffsetMin, NoiseSeedOffsetMax),
                    Random.Range(-NoiseSeedOffsetMin, NoiseSeedOffsetMax),
                    0.0f,
                    0.0f)
            };

            transform.localScale = _config.Value.ScaleModifier * Vector2.one;
        }

        // Apply initial values to the material
        _material.SetFloat(SizeProperty, 0.0f);
        _material.SetFloat(DisturbanceProperty, 0.0f);
        _material.SetFloat(OpacityProperty, 1.0f);
        _material.SetFloat(NoiseProperty, _config.Value.Noise);
        _material.SetVector(SeedProperty, _config.Value.NoiseSeed);
        _material.SetColor(ColorProperty, _config.Value.Color);
    }

    private void Update()
    {
        // Compute how long the splatter has been there
        var splatterTimer = NetworkManager.ServerTime.TimeAsFloat - _config.Value.SpawnTime;

        // Appearing Ratio, smoothed out, to increase the size and disturbance
        var appearingRatio = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(0.0f, _config.Value.SplatterTime, splatterTimer));
        _material.SetFloat(SizeProperty, Mathf.Lerp(0.0f, _config.Value.TargetSize, appearingRatio));
        _material.SetFloat(DisturbanceProperty, Mathf.Lerp(0.0f, _config.Value.TargetDisturbance, appearingRatio));

        // Disappearing Ratio, smoothed out, to make the splatter disappear
        var disappearingRatio = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(_lifetime - 10.0f, _lifetime, splatterTimer));
        _material.SetFloat(OpacityProperty, Mathf.Lerp(1.0f, 0.0f, disappearingRatio));

        // Despawn the splatter after a Lifetime (Only if authority)
        if (IsServer && splatterTimer > _lifetime)
        {
            NetworkObject.Despawn();
        }
    }
}
