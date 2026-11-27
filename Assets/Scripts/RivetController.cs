using System;
using System.Collections.Generic;
using UnityEngine;

public class RivetController : MonoBehaviour
{
    [SerializeField] Belt belt;
    [SerializeField] BlueprintBoard board;
    [SerializeField] GameManager game;

    [Header("Placement")]
    [SerializeField] Vector3 offsetFromGrabPoint = new Vector3(0f, 1.5f, 0f);

    [Header("Tray display")]
    [SerializeField] ChipView trayIconPrefab;
    [SerializeField] Transform trayAnchor;
    [SerializeField] float trayIconSpacing = 1f;

    [Header("Effects")]
    [SerializeField] float pickupFlyTime = 0.2f;
    [SerializeField] float pickupArc = 0.6f;
    [SerializeField] float builtRise = 0.5f;
    [SerializeField] float builtFadeTime = 0.4f;

    public event Action PickedUp;
    public event Action Built;
    public event Action Fumbled;
    public event Action BurntDestroyed;

    readonly List<ChipData> tray = new();
    readonly List<ChipView> trayIcons = new();

    void Start()
    {
        transform.position = belt.GrabPoint + offsetFromGrabPoint;
    }

    void Update()
    {
        if (game.IsPlaying && !game.IsPaused && Time.frameCount > game.StartFrame && OneButton.Pressed) Grab();
    }

    void Grab()
    {
        int slot = belt.SlotAtGrab();
        if (!belt.TryPeek(slot, out ChipData chip))
        {
            Fumble();
            return;
        }

        if (chip.kind == ChipKind.Burnt)
        {
            belt.Discard(slot);
            BurntDestroyed?.Invoke();
            if (tray.Count > 0) Fumble();
            else PickedUp?.Invoke();
            return;
        }

        if (!board.Accepts(tray, chip))
        {
            Fumble();
            return;
        }

        Vector3 from = belt.ChipPosition(slot);
        tray.Add(belt.Take(slot));
        RefreshTray();
        trayIcons[trayIcons.Count - 1].FlyFrom(from, pickupFlyTime, pickupArc);

        int done = board.FindCompleted(tray);
        if (done >= 0)
        {
            game.BlueprintBuilt(board.Complete(done));
            tray.Clear();
            foreach (var icon in trayIcons) icon.Vanish(Vector3.up * builtRise, builtFadeTime, pickupFlyTime);
            trayIcons.Clear();
            board.Redraw(tray);
            Built?.Invoke();
        }
        else
        {
            PickedUp?.Invoke();
        }
    }

    void Fumble()
    {
        var from = new List<Vector3>();
        foreach (var icon in trayIcons) from.Add(icon.transform.position);
        bool fit = belt.DropAfterGrab(tray, from);
        tray.Clear();
        RefreshTray();
        game.BreakStreak();
        Fumbled?.Invoke();
        if (!fit) game.GameOver();
    }

    void RefreshTray()
    {
        foreach (var icon in trayIcons) Destroy(icon.gameObject);
        trayIcons.Clear();

        for (int k = 0; k < tray.Count; k++)
        {
            ChipView icon = Instantiate(trayIconPrefab, trayAnchor);
            icon.transform.localPosition = new Vector3(k * trayIconSpacing, 0f, 0f);
            icon.Set(tray[k]);
            trayIcons.Add(icon);
        }

        board.Redraw(tray);
    }
}