using System.Collections.Generic;
using UnityEngine;

public enum GuideIconType { Sunstone, LifeBlossom, AetherShield, Cactus, SpikeRock, Bat, StormCloud }

/// <summary>Spawns ground and airborne hazards plus three collectible types.</summary>
public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    [Min(.4f)] public float spawnInterval = 2f;
    [Min(.4f)] public float minimumInterval = 1.05f;
    [Min(1f)] public float initialSpeed = 5f;
    [Min(1f)] public float maximumSpeed = 9f;
    [Range(0f, 1f)] public float pickupChance = .22f;
    [Range(0f, 1f)] public float airObstacleChance = .34f;

    private float timer;
    private float runTime;
    private float nextInterval;
    private Transform ground;

    private void Awake()
    {
        nextInterval = spawnInterval;
        GameObject groundObject = GameObject.Find("Ground");
        if (groundObject != null) ground = groundObject.transform;
        if (obstaclePrefab == null) Debug.LogWarning("ObstacleSpawner needs a ground obstacle prefab.", this);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.RunState.Playing) return;
        float gameSpeed = PlayerDash.GameSpeedMultiplier;
        runTime += Time.deltaTime * gameSpeed;
        timer += Time.deltaTime * gameSpeed;
        if (timer < nextInterval) return;
        timer = 0f;
        nextInterval = Random.Range(Mathf.Max(minimumInterval, spawnInterval * .75f), spawnInterval * 1.25f);
        SpawnOne();
    }

    private void SpawnOne()
    {
        float speed = Mathf.Lerp(initialSpeed, maximumSpeed, Mathf.Clamp01(runTime / 100f));
        float x = SpawnX();
        if (Random.value < pickupChance)
        {
            SpawnPickup(x, speed);
            return;
        }
        if (Random.value < airObstacleChance)
        {
            SpawnAirObstacle(x, speed);
            return;
        }

        if (obstaclePrefab == null) return;
        const float obstacleScaleX = .82f;
        const float obstacleScaleY = .68f;
        float y = GroundTop() + obstacleScaleY * .5f;
        GameObject obstacle = Instantiate(obstaclePrefab, new Vector3(x, y, 0f), Quaternion.identity);
        bool useCactus = Random.value < .5f;
        obstacle.name = useCactus ? "Ground Cactus" : "Ground Spike Rock";
        obstacle.transform.localScale = new Vector3(obstacleScaleX, obstacleScaleY, 1f);
        SpriteRenderer renderer = obstacle.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = useCactus
                ? PixelIcon(new Color(.93f, .42f, .34f), IconShape.Cactus)
                : PixelIcon(new Color(.72f, .39f, .43f), IconShape.SpikeRock);
            renderer.color = Color.white;
            renderer.sortingOrder = 1;
        }
        BoxCollider2D obstacleCollider = obstacle.GetComponent<BoxCollider2D>();
        if (obstacleCollider != null)
        {
            // Keep the collision shape inside the visible hazard. Its lower
            // edge remains aligned with the ground after the object is scaled.
            obstacleCollider.size = new Vector2(.76f, .82f);
            obstacleCollider.offset = new Vector2(0f, -.09f);
            obstacleCollider.isTrigger = true;
        }
        ObstacleMovement movement = obstacle.GetComponent<ObstacleMovement>();
        if (movement == null) movement = obstacle.AddComponent<ObstacleMovement>();
        movement.moveSpeed = speed;
    }

    public void ResetForNewRun()
    {
        timer = 0f;
        runTime = 0f;
        nextInterval = spawnInterval;
    }

    private void SpawnAirObstacle(float x, float speed)
    {
        bool useBat = Random.value < .5f;
        GameObject go = new GameObject(useBat ? "Sky Bat Hazard" : "Storm Cloud Hazard", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(FlyingObstacle), typeof(ObstacleMovement));
        go.transform.position = new Vector3(x, GroundTop() + Random.Range(1.35f, 2.15f), 0f);
        go.transform.localScale = useBat ? new Vector3(1.2f, .95f, 1f) : new Vector3(1.1f, 1.05f, 1f);
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = useBat
            ? PixelIcon(new Color(.94f, .28f, .36f), IconShape.Bat)
            : PixelIcon(new Color(.42f, .46f, .72f), IconShape.StormCloud);
        renderer.sortingOrder = 1;
        BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = useBat ? new Vector2(.78f, .58f) : new Vector2(.78f, .66f);
        go.GetComponent<ObstacleMovement>().moveSpeed = speed;
    }

    private void SpawnPickup(float x, float speed)
    {
        int kind = Random.Range(0, 3);
        string name;
        Color color;
        GameObject go;
        if (kind == 0)
        {
            name = "Sunstone"; color = new Color(1f, .76f, .27f);
            go = NewPickup(name, x, GroundTop() + Random.Range(.65f, 1.6f), color, IconShape.Diamond);
            ScoreItem score = go.AddComponent<ScoreItem>(); score.scoreValue = 15;
        }
        else if (kind == 1)
        {
            name = "Life Blossom"; color = new Color(.35f, .91f, .58f);
            go = NewPickup(name, x, GroundTop() + .9f, color, IconShape.Flower);
            go.AddComponent<HealthPickup>();
        }
        else
        {
            name = "Aether Shield"; color = new Color(.38f, .78f, 1f);
            go = NewPickup(name, x, GroundTop() + 1.25f, color, IconShape.Shield);
            go.AddComponent<ShieldPickup>();
        }
        go.GetComponent<ObstacleMovement>().moveSpeed = speed;
    }

    private GameObject NewPickup(string name, float x, float y, Color color, IconShape shape)
    {
        GameObject go = new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(ObstacleMovement));
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(.68f, .68f, 1f);
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = PixelIcon(color, shape); renderer.sortingOrder = 2;
        CircleCollider2D collider = go.GetComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = .42f;
        return go;
    }

    private float GroundTop()
    {
        if (ground != null)
        {
            Collider2D collider = ground.GetComponent<Collider2D>();
            if (collider != null) return collider.bounds.max.y;
        }
        return -3f;
    }

    private float SpawnX()
    {
        Camera camera = Camera.main;
        if (camera == null) return transform.position.x;
        float halfWidth = camera.orthographicSize * camera.aspect;
        return camera.transform.position.x + halfWidth + 1.5f;
    }

    private enum IconShape { Diamond, Flower, Shield, Cactus, SpikeRock, Bat, StormCloud }

    public static Sprite GetGuideIcon(GuideIconType icon)
    {
        switch (icon)
        {
            case GuideIconType.Sunstone: return PixelIcon(new Color(1f, .76f, .27f), IconShape.Diamond);
            case GuideIconType.LifeBlossom: return PixelIcon(new Color(.35f, .91f, .58f), IconShape.Flower);
            case GuideIconType.AetherShield: return PixelIcon(new Color(.38f, .78f, 1f), IconShape.Shield);
            case GuideIconType.Cactus: return PixelIcon(new Color(.93f, .42f, .34f), IconShape.Cactus);
            case GuideIconType.SpikeRock: return PixelIcon(new Color(.72f, .39f, .43f), IconShape.SpikeRock);
            case GuideIconType.Bat: return PixelIcon(new Color(.94f, .28f, .36f), IconShape.Bat);
            case GuideIconType.StormCloud: return PixelIcon(new Color(.42f, .46f, .72f), IconShape.StormCloud);
            default: return null;
        }
    }

    private static readonly Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();
    private static readonly Vector2[] batLeftWing =
    {
        new Vector2(31, 37), new Vector2(25, 42), new Vector2(19, 46), new Vector2(14, 46),
        new Vector2(10, 42), new Vector2(6, 45), new Vector2(5, 38), new Vector2(12, 39),
        new Vector2(8, 32), new Vector2(17, 35), new Vector2(24, 35)
    };
    private static readonly Vector2[] batRightWing =
    {
        new Vector2(33, 37), new Vector2(39, 42), new Vector2(45, 46), new Vector2(50, 46),
        new Vector2(54, 42), new Vector2(58, 45), new Vector2(59, 38), new Vector2(52, 39),
        new Vector2(56, 32), new Vector2(47, 35), new Vector2(40, 35)
    };
    private static readonly Vector2[] batLeftEar = { new Vector2(25, 39), new Vector2(31, 42), new Vector2(29, 55) };
    private static readonly Vector2[] batRightEar = { new Vector2(39, 42), new Vector2(45, 39), new Vector2(35, 55) };

    private static bool PointInPolygon(float x, float y, Vector2[] points)
    {
        bool inside = false;
        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
        {
            Vector2 a = points[i], b = points[j];
            if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    private static Sprite PixelIcon(Color color, IconShape shape)
    {
        int key = (color.GetHashCode() * 397) ^ (int)shape;
        if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color clear = new Color(0, 0, 0, 0);
        Color[] pixels = new Color[size * size];
        Color outline = new Color(.1f, .14f, .22f, 1f);
        Color highlight = Color.Lerp(color, Color.white, .48f);
        bool[,] mask = new bool[size, size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = (x - 31.5f) / 31.5f, ny = (y - 31.5f) / 31.5f;
            switch (shape)
            {
                case IconShape.SpikeRock:
                    bool rockBody = ((x - 32f) / 24f) * ((x - 32f) / 24f) + ((y - 18f) / 13f) * ((y - 18f) / 13f) < 1f;
                    bool peakLeft = ((x - 19f) / 10f) * ((x - 19f) / 10f) + ((y - 31f) / 18f) * ((y - 31f) / 18f) < 1f;
                    bool peakCenter = ((x - 34f) / 11f) * ((x - 34f) / 11f) + ((y - 34f) / 24f) * ((y - 34f) / 24f) < 1f;
                    bool peakRight = ((x - 47f) / 9f) * ((x - 47f) / 9f) + ((y - 28f) / 15f) * ((y - 28f) / 15f) < 1f;
                    mask[x, y] = rockBody || peakLeft || peakCenter || peakRight;
                    break;
                case IconShape.Bat:
                    bool batBody = ((x - 32) * (x - 32)) / 58f + ((y - 32) * (y - 32)) / 165f < 1f;
                    bool batWings = PointInPolygon(x, y, batLeftWing) || PointInPolygon(x, y, batRightWing);
                    bool batEars = PointInPolygon(x, y, batLeftEar) || PointInPolygon(x, y, batRightEar);
                    mask[x, y] = batBody || batWings || batEars;
                    break;
                case IconShape.StormCloud:
                    bool cloudPuff = (x - 18) * (x - 18) + (y - 34) * (y - 34) < 150 ||
                                     (x - 32) * (x - 32) + (y - 40) * (y - 40) < 210 ||
                                     (x - 47) * (x - 47) + (y - 34) * (y - 34) < 150 ||
                                     (x >= 15 && x <= 50 && y >= 25 && y <= 36);
                    bool lightning = y >= 9 && y <= 32 &&
                                     ((x >= 30 && x <= 39 && y >= 24) ||
                                      (x >= 26 && x <= 35 && y >= 17 && y < 25) ||
                                      (x >= 31 && x <= 39 && y >= 9 && y < 18));
                    bool rain = y >= 7 && y <= 17 && (x == 19 || x == 20 || x == 43 || x == 44 || x == 53 || x == 54);
                    mask[x, y] = cloudPuff || lightning || rain;
                    break;
                case IconShape.Cactus:
                    bool cactusStem = ((x - 32f) / 7.5f) * ((x - 32f) / 7.5f) + ((y - 31f) / 27f) * ((y - 31f) / 27f) < 1f;
                    bool leftArm = ((x - 21f) / 7.5f) * ((x - 21f) / 7.5f) + ((y - 31f) / 5.5f) * ((y - 31f) / 5.5f) < 1f ||
                                    ((x - 17f) / 5.5f) * ((x - 17f) / 5.5f) + ((y - 38f) / 9f) * ((y - 38f) / 9f) < 1f;
                    bool rightArm = ((x - 43f) / 7.5f) * ((x - 43f) / 7.5f) + ((y - 39f) / 5.5f) * ((y - 39f) / 5.5f) < 1f ||
                                     ((x - 47f) / 5.5f) * ((x - 47f) / 5.5f) + ((y - 31f) / 9f) * ((y - 31f) / 9f) < 1f;
                    mask[x, y] = cactusStem || leftArm || rightArm;
                    break;
                case IconShape.Shield: mask[x, y] = ny > -.74f && Mathf.Abs(nx) < .78f - Mathf.Max(0f, -ny) * .66f; break;
                case IconShape.Flower:
                    bool petal = (x - 32) * (x - 32) + (y - 32) * (y - 32) < 90 ||
                                 (x - 32) * (x - 32) + (y - 48) * (y - 48) < 100 ||
                                 (x - 32) * (x - 32) + (y - 16) * (y - 16) < 100 ||
                                 (x - 48) * (x - 48) + (y - 32) * (y - 32) < 100 ||
                                 (x - 16) * (x - 16) + (y - 32) * (y - 32) < 100;
                    mask[x, y] = petal;
                    break;
                default: mask[x, y] = Mathf.Abs(nx) + Mathf.Abs(ny) < .86f; break;
            }
        }
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            if (!mask[x, y]) { pixels[y * size + x] = clear; continue; }
            bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1 ||
                        !mask[Mathf.Max(0, x - 1), y] || !mask[Mathf.Min(size - 1, x + 1), y] ||
                        !mask[x, Mathf.Max(0, y - 1)] || !mask[x, Mathf.Min(size - 1, y + 1)];
            bool detail = shape == IconShape.Cactus && ((x == 30 && y % 13 == 7) || (x == 36 && y % 17 == 10)) ||
                          shape == IconShape.SpikeRock && (x == 26 && y == 18 || x == 39 && y == 12) ||
                          shape == IconShape.Bat && (x == 28 || x == 36) && y == 34;
            bool eye = shape == IconShape.Bat && (x == 28 || x == 36) && y == 34;
            bool shine = shape == IconShape.Diamond && x > 24 && y > 37 && x < 30 && y < 47;
            bool lightning = shape == IconShape.StormCloud && y >= 9 && y <= 32 &&
                             ((x >= 30 && x <= 39 && y >= 24) ||
                              (x >= 26 && x <= 35 && y >= 17 && y < 25) ||
                              (x >= 31 && x <= 39 && y >= 9 && y < 18));
            bool rain = shape == IconShape.StormCloud && y >= 7 && y <= 17 && (x == 19 || x == 20 || x == 43 || x == 44 || x == 53 || x == 54);
            Color lightningColor = new Color(1f, .86f, .3f, 1f);
            Color rainColor = new Color(.63f, .82f, 1f, 1f);
            pixels[y * size + x] = lightning ? (edge ? outline : lightningColor) : rain ? (edge ? outline : rainColor) :
                edge ? outline : eye || detail || shine ? Color.white : (x < 32 ? highlight : color);
        }
        texture.SetPixels(pixels); texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64f);
        spriteCache[key] = sprite;
        return sprite;
    }
}
