using System;
using System.Collections.Generic;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public sealed class WorldCheatsMenu : Menu
    {
        public const int MenuId = 20, SpawnButtonId = 8;
        private static readonly IMenu page = new ListMenu(MenuId, new Rect(1503, 10, 20, 20), "WORLD CHEATS");
        private Button abundance, paradise, shopping, loot, rarity, chaos, freeze, status;
        private float nextLabelRefresh;
        private readonly List<Button> rows = new List<Button>();
        public WorldCheatsMenu() : base(page)
        {
            SetPrevMenuId(4);
            if (!UmbraMenu.characterCollected && !UmbraMenu.forceFullModMenu) return;
            SetActivatingButton(Utility.FindButtonById(4, SpawnButtonId));
            Heading("Stage generation");
            abundance = Multiplier("Stage Abundance", () => WorldCheats.StageAbundance, v => WorldCheats.StageAbundance = v,
                () => WorldCheats.StageMultiplier, v => WorldCheats.StageMultiplier = v);
            Note("Next stage: more chests, shrines and drones.");
            paradise = Multiplier("Chest Paradise", () => WorldCheats.ChestParadise, v => WorldCheats.ChestParadise = v,
                () => WorldCheats.ChestMultiplier, v => WorldCheats.ChestMultiplier = v);
            Note("Next stage: extra chests, up to 50. Placement may limit the total.");
            Heading("Shopping and chest loot");
            shopping = Toggle("Free Shopping", () => WorldCheats.FreeShopping, v => WorldCheats.FreeShopping = v);
            Note("Money purchases are free. Other payment types keep their cost.");
            loot = Multiplier("Loot Multiplier", () => WorldCheats.LootMultiplier, v => WorldCheats.LootMultiplier = v,
                () => WorldCheats.DropMultiplier, v => WorldCheats.DropMultiplier = v);
            Note("Unopened chests: multiply their drops, up to 20 per chest.");
            rarity = new Button(new MulButton(this, rows.Count + 1, "Rarity Control : Natural", CycleRarity, CycleRarity, PreviousRarity)); rows.Add(rarity);
            Note("Item chests use the chosen rarity. Equipment stays unchanged.");
            Heading("Combat");
            chaos = new Button(new TogglableMulButton(this, rows.Count + 1, "PROC CHAOS : OFF", "PROC CHAOS : ON",
                ToggleChaos, ToggleChaos, () => ChangeLuck(1), () => ChangeLuck(-1))); rows.Add(chaos);
            Note("Boosts your luck-based item procs; higher values give more rerolls.");
            freeze = Toggle("Freeze Enemies", () => WorldCheats.FreezeEnemies, v => WorldCheats.FreezeEnemies = v);
            Note("Stops hostile movement and attacks. Toggle OFF to let them resume.");
            Heading("Session");
            rows.Add(new Button(new NormalButton(this, rows.Count + 1, "TURN OFF ALL WORLD CHEATS", () => { WorldCheats.Reset(); RefreshLabels(); })));
            status = new Button(new TextButton(this, rows.Count + 1, WorldCheats.Status)); rows.Add(status);
            AddButtons(rows); RefreshLabels();
        }
        private void Heading(string text) => rows.Add(new Button(new DividerButton(this, rows.Count + 1, text, new Color32(113, 226, 207, 255), true)));
        private void Note(string text) => rows.Add(new Button(new TextButton(this, rows.Count + 1, text)));
        private Button Toggle(string name, Func<bool> get, Action<bool> set)
        {
            void Change() { if (!WorldCheats.RequireHost()) { RefreshLabels(); return; } set(!get()); WorldCheats.SettingsChanged(); RefreshLabels(); }
            var row = new Button(new TogglableButton(this, rows.Count + 1, name + " : OFF", name + " : ON", Change, Change, get()));
            rows.Add(row); return row;
        }
        private Button Multiplier(string name, Func<bool> enabled, Action<bool> setEnabled, Func<int> value, Action<int> setValue)
        {
            void Toggle() { if (!WorldCheats.RequireHost()) { RefreshLabels(); return; } setEnabled(!enabled()); WorldCheats.SettingsChanged(); RefreshLabels(); }
            void Adjust(int step) { if (!WorldCheats.RequireHost()) return; setValue(Mathf.Clamp(value() + step, 2, 5)); WorldCheats.SettingsChanged(); RefreshLabels(); }
            var row = new Button(new TogglableMulButton(this, rows.Count + 1, name + " : OFF", name + " : ON", Toggle, Toggle, () => Adjust(1), () => Adjust(-1)));
            rows.Add(row); return row;
        }
        private void CycleRarity() => ChangeRarity(1);
        private void PreviousRarity() => ChangeRarity(-1);
        private void ChangeRarity(int step)
        {
            if (!WorldCheats.RequireHost()) return;
            int count = Enum.GetValues(typeof(LootRarity)).Length;
            WorldCheats.Rarity = (LootRarity)(((int)WorldCheats.Rarity + step + count) % count);
            WorldCheats.SettingsChanged(); RefreshLabels();
        }
        private void ToggleChaos()
        {
            if (!WorldCheats.RequireHost()) { RefreshLabels(); return; }
            WorldCheats.ProcChaos = !WorldCheats.ProcChaos; WorldCheats.SettingsChanged(); RefreshLabels();
        }
        private void ChangeLuck(int step)
        {
            if (!WorldCheats.RequireHost()) return;
            WorldCheats.LuckBonus = Mathf.Clamp(WorldCheats.LuckBonus + step * 5, 5, 20);
            WorldCheats.SettingsChanged(); RefreshLabels();
        }
        private static void Label(Button row, string name, bool enabled, string suffix = "")
        {
            row.SetOffText(name + " (OFF)" + suffix); row.SetOnText(name + " (ON)" + suffix); row.SetEnabled(enabled);
        }
        private void RefreshLabels()
        {
            if (abundance == null) return;
            Label(abundance, "Stage Abundance", WorldCheats.StageAbundance, " : x" + WorldCheats.StageMultiplier);
            Label(paradise, "Chest Paradise", WorldCheats.ChestParadise, " : x" + WorldCheats.ChestMultiplier);
            Label(shopping, "Free Shopping", WorldCheats.FreeShopping);
            Label(loot, "Loot Multiplier", WorldCheats.LootMultiplier, " : x" + WorldCheats.DropMultiplier);
            rarity.SetText("Rarity Control : " + WorldCheats.Rarity);
            Label(chaos, "Proc Chaos", WorldCheats.ProcChaos, " : +" + WorldCheats.LuckBonus + " luck");
            Label(freeze, "Freeze Enemies", WorldCheats.FreezeEnemies);
            status.SetText(WorldCheats.IsHost ? WorldCheats.Status : "Host of an active run required.");
        }
        public override void Draw()
        {
            if (!IsEnabled()) return;
            if (Time.unscaledTime >= nextLabelRefresh) { nextLabelRefresh = Time.unscaledTime + .1f; RefreshLabels(); }
            base.Draw();
        }
        public override void Reset() { WorldCheats.Reset(); RefreshLabels(); base.Reset(); }
    }
}
