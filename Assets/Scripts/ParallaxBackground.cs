using UnityEngine;

/// <summary>Creates layered, looping silhouettes that scroll at different rates.</summary>
[RequireComponent(typeof(Camera))]
public sealed class ParallaxBackground : MonoBehaviour
{
    [Min(0f)] public float worldScrollSpeed = 3.5f;
    private Transform[][] tiles;
    private float[] widths;
    private Transform[] stars;
    private float starFieldWidth;
    private Camera targetCamera;
    private Material groundMaterial;
    private float groundTextureOffset;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        // A storybook twilight palette keeps the original night-run setting,
        // while lifting the dark values so the cartoon artwork reads clearly.
        targetCamera.backgroundColor = new Color(.105f, .145f, .255f);
        BuildLayers();
        BuildCelestials();
        StyleGround();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.RunState.Playing) return;
        float currentWorldSpeed = worldScrollSpeed * PlayerDash.GameSpeedMultiplier;
        if (groundMaterial != null)
        {
            groundTextureOffset = Mathf.Repeat(groundTextureOffset + currentWorldSpeed * Time.deltaTime, 1f);
            groundMaterial.mainTextureOffset = new Vector2(groundTextureOffset, 0f);
        }
        float starSpeed = currentWorldSpeed * .1f;
        float leftStarEdge = targetCamera.transform.position.x - targetCamera.orthographicSize * targetCamera.aspect - .15f;
        for (int i = 0; i < stars.Length; i++)
        {
            Transform star = stars[i];
            star.position += Vector3.left * starSpeed * Time.deltaTime;
            if (star.position.x < leftStarEdge)
                star.position += Vector3.right * (starFieldWidth + .3f);
        }
        for (int layer = 0; layer < tiles.Length; layer++)
        {
            float speed = currentWorldSpeed * (layer == 0 ? .12f : layer == 1 ? .28f : .48f);
            for (int i = 0; i < tiles[layer].Length; i++)
            {
                Transform tile = tiles[layer][i];
                tile.position += Vector3.left * speed * Time.deltaTime;
                float leftEdge = targetCamera.transform.position.x - targetCamera.orthographicSize * targetCamera.aspect - widths[layer] * .5f;
                if (tile.position.x < leftEdge - widths[layer] * .5f)
                    tile.position += Vector3.right * widths[layer] * tiles[layer].Length;
            }
        }
    }

    private void BuildLayers()
    {
        tiles = new Transform[3][];
        widths = new float[3];
        Color[] colors = { new Color(.39f, .45f, .69f), new Color(.25f, .39f, .53f), new Color(.16f, .28f, .42f) };
        int[] baseHeights = { 42, 29, 19 };
        // Smooth gradients and a high resolution ridge give the layered hills
        // a soft storybook silhouette instead of a blocky pixel edge.
        float tileWidth = 20f;
        widths[0] = widths[1] = widths[2] = tileWidth;
        for (int layer = 0; layer < 3; layer++)
        {
            Sprite sprite = MakeHills(colors[layer], baseHeights[layer], layer);
            tiles[layer] = new Transform[3];
            for (int i = 0; i < tiles[layer].Length; i++)
            {
                GameObject go = new GameObject($"Parallax Layer {layer + 1} Tile {i + 1}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(targetCamera.transform.position.x + (i - 1) * tileWidth, targetCamera.transform.position.y, 5f + layer);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = -10 + layer;
                tiles[layer][i] = go.transform;
            }
        }
    }

    private static Sprite MakeHills(Color color, int averageHeight, int seed)
    {
        const int width = 512, height = 256;
        const float ppu = 25.6f;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        int ridge = Mathf.RoundToInt(averageHeight * (height / 160f));
        for (int x = 0; x < width; x++)
        {
            // A periodic, multi-frequency ridge makes the tile edges line up
            // while giving each layer broad peaks and smaller shoulders.
            float phase = (x / (float)width) * Mathf.PI * 2f + seed * .91f;
            float profile = Mathf.Sin(phase) * .56f + Mathf.Sin(phase * 2f + 1.1f) * .28f + Mathf.Sin(phase * 3f + 2.3f) * .16f;
            int top = Mathf.Clamp(Mathf.RoundToInt(ridge + profile * ridge * .46f), 3, height / 2);
            int snowDepth = top > ridge * 1.22f ? Mathf.Clamp(Mathf.RoundToInt((top - ridge * 1.16f) * .4f), 2, 11) : 0;
            for (int y = 0; y < top; y++)
            {
                float depth = (float)y / Mathf.Max(1, top);
                Color gradient = Color.Lerp(color * .55f, color, .35f + depth * .57f);
                if (y == top - 1)
                    gradient = Color.Lerp(color, new Color(.82f, .87f, 1f), seed == 0 ? .48f : seed == 1 ? .34f : .22f);
                else if (snowDepth > 0 && y >= top - snowDepth)
                    gradient = Color.Lerp(new Color(.61f, .73f, .9f), new Color(.91f, .93f, 1f), (float)(y - (top - snowDepth)) / snowDepth);
                else if (y == top - 2 && (x + seed * 7) % 43 < 5)
                    gradient = Color.Lerp(gradient, new Color(.95f, .78f, .76f), .22f);
                else if (y < top * .3f && (x + seed * 13) % 31 < 2)
                    gradient *= .76f;
                pixels[y * width + x] = gradient;
            }
        }
        texture.SetPixels(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), ppu);
    }

    private void BuildCelestials()
    {
        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        GameObject moon = new GameObject("Silver Moon");
        moon.transform.SetParent(transform, false);
        moon.transform.localPosition = new Vector3(targetCamera.transform.position.x + halfWidth * .58f,
            targetCamera.transform.position.y + targetCamera.orthographicSize * .53f, 8f);
        SpriteRenderer moonRenderer = moon.AddComponent<SpriteRenderer>();
        moonRenderer.sprite = MakeMoon();
        moonRenderer.sortingOrder = -20;

        Random.State previousRandomState = Random.state;
        Random.InitState(41);
        Sprite starSprite = MakeStar();
        stars = new Transform[26];
        starFieldWidth = halfWidth * 2f;
        for (int i = 0; i < stars.Length; i++)
        {
            float x = Random.Range(-halfWidth, halfWidth);
            float y = Random.Range(-.2f, targetCamera.orthographicSize * .88f);
            GameObject star = new GameObject($"Sky Sparkle {i + 1}");
            star.transform.SetParent(transform, false);
            star.transform.localPosition = new Vector3(targetCamera.transform.position.x + x,
                targetCamera.transform.position.y + y, 8.5f);
            float size = Random.Range(.65f, 1.35f);
            star.transform.localScale = new Vector3(size, size, 1f);
            SpriteRenderer renderer = star.AddComponent<SpriteRenderer>();
            renderer.sprite = starSprite;
            renderer.color = new Color(1f, .84f, .68f, Random.Range(.45f, .9f));
            renderer.sortingOrder = -19;
            stars[i] = star.transform;
        }
        Random.state = previousRandomState;
    }

    private static Sprite MakeMoon()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - 63.5f) / 54f;
            float dy = (y - 64f) / 54f;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float edgeAlpha = Mathf.Clamp01((1.015f - distance) * 24f);
            if (edgeAlpha <= 0f) { pixels[y * size + x] = new Color(0, 0, 0, 0); continue; }

            // Cool silver-blue shading and subtle craters replace the bright,
            // warm glow that made the old moon look like a sun.
            float shade = Mathf.Clamp01((1f - dx * .45f - dy * .22f) * .5f);
            Color moon = Color.Lerp(new Color(.43f, .56f, .73f), new Color(.91f, .94f, 1f), shade);
            float craterA = Mathf.Sqrt((x - 45f) * (x - 45f) + (y - 81f) * (y - 81f));
            float craterB = Mathf.Sqrt((x - 76f) * (x - 76f) + (y - 47f) * (y - 47f));
            float craterC = Mathf.Sqrt((x - 81f) * (x - 81f) + (y - 81f) * (y - 81f));
            if (craterA < 9f || craterB < 6f || craterC < 4f) moon = Color.Lerp(moon, new Color(.48f, .6f, .76f), .38f);
            pixels[y * size + x] = new Color(moon.r, moon.g, moon.b, edgeAlpha);
        }
        texture.SetPixels(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64f);
    }

    private static Sprite MakeStar()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = Mathf.Abs((x + .5f - size * .5f) / (size * .5f));
            float ny = Mathf.Abs((y + .5f - size * .5f) / (size * .5f));
            float beam = Mathf.Min(nx, ny);
            float extent = Mathf.Max(nx, ny);
            float cross = Mathf.Clamp01((.11f - beam) * 14f) * Mathf.Clamp01((.91f - extent) * 8f);
            float center = Mathf.Clamp01((.27f - Mathf.Sqrt(nx * nx + ny * ny)) * 7f);
            float glow = Mathf.Clamp01((.98f - Mathf.Sqrt(nx * nx + ny * ny)) * 1.4f) * .18f;
            float alpha = Mathf.Max(cross, Mathf.Max(center, glow));
            pixels[y * size + x] = new Color(1f, .87f, .72f, alpha);
        }
        texture.SetPixels(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64f);
    }

    private void StyleGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null) return;
        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        BoxCollider2D collider = ground.GetComponent<BoxCollider2D>();
        if (renderer == null) return;

        Vector3 oldScale = ground.transform.localScale;
        float width = collider != null ? collider.size.x * oldScale.x : 20f;
        float height = collider != null ? collider.size.y * oldScale.y : 1f;
        renderer.sprite = MakeGround();
        renderer.color = Color.white;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(width, height);
        groundMaterial = renderer.material;
        groundTextureOffset = 0f;
        groundMaterial.mainTextureOffset = Vector2.zero;
        ground.transform.localScale = Vector3.one;
        if (collider != null)
        {
            collider.size = new Vector2(width, height);
            collider.offset = Vector2.zero;
        }
    }

    private static Sprite MakeGround()
    {
        const int width = 256, height = 96;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float phase = x / (float)width * Mathf.PI * 2f;
            int grassLine = 78 + Mathf.RoundToInt(Mathf.Sin(phase) * 2f + Mathf.Sin(phase * 2f + .6f) * 1.2f);
            float depth = y / (float)height;
            Color value;
            if (y >= grassLine)
            {
                float grassShade = Mathf.Clamp01((y - grassLine) / 12f);
                value = Color.Lerp(new Color(.62f, .82f, .49f), new Color(.38f, .68f, .4f), grassShade);
                float glint = Mathf.Sin(phase * 7f + .8f) * .5f + .5f;
                if (y == grassLine && glint > .83f) value = Color.Lerp(value, new Color(.84f, .91f, .61f), .7f);
            }
            else
            {
                float shade = .7f + depth * .26f + Mathf.Sin(phase * 3f) * .025f;
                value = new Color(.33f, .34f, .48f) * shade;
                bool pebble = y < 56 && (Mathf.Sin(phase * 8f + y * .4f) > .985f);
                if (pebble) value = new Color(.49f, .47f, .6f);
                if (y == grassLine - 1) value = new Color(.29f, .47f, .42f);
            }
            pixels[y * width + x] = value;
        }
        texture.SetPixels(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), height, 0, SpriteMeshType.FullRect);
    }

    private void OnDestroy()
    {
        if (groundMaterial != null) Destroy(groundMaterial);
    }
}
