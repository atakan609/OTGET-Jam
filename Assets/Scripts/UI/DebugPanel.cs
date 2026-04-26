using System.Collections.Generic;
using UnityEngine;
using Managers;
using Gameplay;

/// <summary>
/// Oyun içi Debug paneli. Yalnızca Editor veya Development Build'de görünür.
/// Sahneye boş bir GameObject ekle → bu scripti sürükle → çalıştır.
/// Kısayol: Tab tuşuyla paneli aç/kapat.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private float addMoneyAmount = 50f;
    [SerializeField] private float goldenDropValue = 5f;
    [SerializeField] private float stunDuration = 1f;

    private bool _isOpen = false;

    // Panel boyutu ve pozisyonu
    private Rect _windowRect = new Rect(10f, 10f, 260f, 600f);
    private bool _isDragging = false;
    private Vector2 _dragOffset;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            _isOpen = !_isOpen;
    }

    private void OnGUI()
    {
        if (!_isOpen) return;

        // Panel arka planı
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(_windowRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(_windowRect);
        DrawHeader();
        DrawButtons();
        GUILayout.EndArea();

        HandleDrag();
    }

    // ── Layout ──────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan },
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Label("⚡ DEBUG PANEL  [TAB]", titleStyle);
        GUILayout.Space(4f);
        DrawSeparator();
    }

    private void DrawButtons()
    {
        GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        // ─── PARA ───────────────────────────────────────────────────────────
        GUILayout.Label("── PARA ──", sectionStyle);
        if (GUILayout.Button($"+ {CurrencyManager.FormatWater(addMoneyAmount)} (Depoya)"))
            Debug_AddMoney(addMoneyAmount);
        if (GUILayout.Button($"+ {CurrencyManager.FormatWater(addMoneyAmount * 10f)} (Depoya)"))
            Debug_AddMoney(addMoneyAmount * 10f);
        DrawSeparator();

        // ─── YILDIRIM ───────────────────────────────────────────────────────
        GUILayout.Label("── YILDIRIM / STUN ──", sectionStyle);
        if (GUILayout.Button("⚡ Kafama Yıldırım Düşür"))
            Debug_LightningOnPlayer();
        if (GUILayout.Button($"😵 Karakteri Stun Et ({stunDuration}s)"))
            Debug_StunPlayer();
        DrawSeparator();

        // ─── BULUT ──────────────────────────────────────────────────────────
        GUILayout.Label("── BULUT ──", sectionStyle);
        if (GUILayout.Button("🌩 Storm Cloud Çağır"))
            Debug_SpawnStormCloud();
        if (GUILayout.Button("☁ Normal Bulut Çağır"))
            Debug_SpawnCloud();
        DrawSeparator();

        // ─── DAMLALAR ───────────────────────────────────────────────────────
        GUILayout.Label("── DAMLALAR ──", sectionStyle);
        if (GUILayout.Button($"⭐ Golden Drop Düşür (x{goldenDropValue})"))
            Debug_SpawnGoldenDrop();
        if (GUILayout.Button("💧 Normal Drop Düşür"))
            Debug_SpawnNormalDrop();
        DrawSeparator();

        // ─── KOVA ───────────────────────────────────────────────────
        GUILayout.Label("── KOVA ──", sectionStyle);
        if (GUILayout.Button("🪣 Kovayı Doldur"))
            Debug_FillBucket();
        if (GUILayout.Button("🪣 Kovayı Boşalt"))
            Debug_EmptyBucket();
        DrawSeparator();

        // ─── UPGRADE ───────────────────────────────────────────
        GUILayout.Label("── UPGRADE ──", sectionStyle);
        if (Managers.UpgradeManager.Instance != null && Managers.UpgradeManager.Instance.TreeDatas != null)
        {
            for (int i = 0; i < Managers.UpgradeManager.Instance.TreeDatas.Count; i++)
            {
                var tree = Managers.UpgradeManager.Instance.TreeDatas[i];
                string treeName = (tree != null && tree.rootNode != null) ? tree.rootNode.upgradeData.upgradeName : $"Tree {i}";
                if (GUILayout.Button($"⬆️ {treeName} Ağacını Ful (MAX)"))
                    Debug_MaxTreeUpgrades(i);
            }
        }
        else
        {
            GUILayout.Label("UpgradeManager bekleniyor...", sectionStyle);
        }
    }

    private void DrawSeparator()
    {
        GUIStyle sepStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { textColor = new Color(1f, 1f, 1f, 0.3f) }
        };
        GUILayout.Label("─────────────────────────", sepStyle);
    }

    private void HandleDrag()
    {
        Event e = Event.current;
        Rect titleBar = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, 24f);

        if (e.type == EventType.MouseDown && titleBar.Contains(e.mousePosition))
        {
            _isDragging = true;
            _dragOffset = e.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
        }
        if (e.type == EventType.MouseUp)   _isDragging = false;
        if (_isDragging && e.type == EventType.MouseDrag)
        {
            _windowRect.x = e.mousePosition.x - _dragOffset.x;
            _windowRect.y = e.mousePosition.y - _dragOffset.y;
        }
    }

    // ── Debug Actions ────────────────────────────────────────────────────────

    private void Debug_AddMoney(float amount)
    {
        var depot = FindObjectOfType<DepotController>();
        if (depot != null)
        {
            depot.AddWater(amount);
            Log($"+ {amount} mL depoya eklendi.");
        }
        else Log("DepotController bulunamadı!");
    }

    private void Debug_LightningOnPlayer()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) { Log("PlayerController bulunamadı!"); return; }

        if (LightningManager.Instance != null)
        {
            // Oyuncunun tam konumunu kullan
            LightningManager.Instance.SpawnImmediateLightning(
                player.transform.position.x,
                player.transform.position.y + 5f  // Biraz yukarıdan başla
            );
            Log("⚡ Oyuncuya yıldırım çaktırıldı!");
        }
        else Log("LightningManager bulunamadı!");
    }

    private void Debug_StunPlayer()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.ApplyLightningStun(stunDuration);
            Log($"😵 Oyuncu {stunDuration}s stun'landı.");
        }
        else Log("PlayerController bulunamadı!");
    }

    private void Debug_SpawnStormCloud()
    {
        if (CloudManager.Instance == null) { Log("CloudManager bulunamadı!"); return; }
        CloudManager.Instance.SpawnDebugStormCloud();
        Log("🌩 Storm Cloud çağrıldı!");
    }

    private void Debug_SpawnCloud()
    {
        if (CloudManager.Instance == null) { Log("CloudManager bulunamadı!"); return; }
        CloudManager.Instance.SpawnDebugCloud();
        Log("☁ Normal Bulut çağrıldı!");
    }

    private void Debug_SpawnGoldenDrop()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) { Log("PlayerController bulunamadı!"); return; }

        Vector3 pos = player.transform.position + new Vector3(0f, 5f, 0f);
        SpawnRaindropAt(pos, goldenDropValue, isGolden: true);
        Log($"⭐ Golden Drop düşürüldü (value={goldenDropValue})");
    }

    private void Debug_SpawnNormalDrop()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) { Log("PlayerController bulunamadı!"); return; }

        Vector3 pos = player.transform.position + new Vector3(0f, 5f, 0f);
        SpawnRaindropAt(pos, 1f, isGolden: false);
        Log("💧 Normal Drop düşürüldü.");
    }

    private void Debug_FillBucket()
    {
        var bucket = FindObjectOfType<BucketController>();
        if (bucket != null)
        {
            bucket.TryAddWater(bucket.MaxCapacity);
            Log("🪣 Kova dolduruldu.");
        }
        else Log("BucketController bulunamadı!");
    }

    private void Debug_EmptyBucket()
    {
        var bucket = FindObjectOfType<BucketController>();
        if (bucket != null)
        {
            bucket.DrainWater(bucket.MaxCapacity);
            Log("🪣 Kova boşaltıldı.");
        }
        else Log("BucketController bulunamadı!");
    }

    private void Debug_MaxTreeUpgrades(int index)
    {
        if (Managers.UpgradeManager.Instance != null)
        {
            Managers.UpgradeManager.Instance.Debug_MaxTreeUpgrades(index);
            Log($"⬆️ {index}. Ağaç max seviyeye çekildi.");
        }
        else Log("UpgradeManager bulunamadı!");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SpawnRaindropAt(Vector3 pos, float value, bool isGolden)
    {
        // Sahnedeki aktif bir Cloud'dan prefab referansını çalmak yerine,
        // direkt Resources klasöründen veya seri alandan yüklüyoruz.
        // Eğer prefab referansı yoksa Cloud'dan bir tane buluyoruz.
        GameObject prefab = isGolden ? _goldenDropPrefab : _normalDropPrefab;

        if (prefab == null)
        {
            // Fallback: Cloud'dan prefab çal
            var cloud = FindObjectOfType<Cloud>();
            if (cloud != null)
            {
                var fieldName = isGolden ? "goldenRaindropPrefab" : "raindropPrefab";
                var field = typeof(Cloud).GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prefab = field?.GetValue(cloud) as GameObject;
            }
        }

        if (prefab == null) { Log("Raindrop prefabı bulunamadı! Cloud sahnesinde olduğundan emin ol."); return; }

        var obj = Instantiate(prefab, pos, Quaternion.identity);
        var raindrop = obj.GetComponent<Raindrop>();
        if (raindrop != null)
        {
            raindrop.dropValue = value;
            raindrop.isGolden  = isGolden;
            raindrop.ApplySize();
        }
    }

    private static void Log(string msg) => Debug.Log($"[DebugPanel] {msg}");

#endif

    // ── Inspector referansları (Opsiyonel – doldurmak zorunda değilsin) ───────

    [Header("Raindrop Prefab Refs (opsiyonel, boş bırakılabilir)")]
    [SerializeField] private GameObject _normalDropPrefab;
    [SerializeField] private GameObject _goldenDropPrefab;
}
