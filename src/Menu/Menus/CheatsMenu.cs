using System;
using System.Collections.Generic;
using UnityEngine;

namespace UmbraMenu.Menus
{
    public sealed class CheatsMenu : Menu
    {
        public const int MenuId = 20, MainButtonId = 9;
        private static readonly IMenu page = new ListMenu(MenuId, new Rect(1503, 10, 20, 20), "CHEATS");
        private Button abundance, paradise, shopping, loot, rarity, chaos, freeze, status;
        private float nextLabelRefresh;
        private readonly List<Button> rows = new List<Button>();
        public CheatsMenu() : base(page)
        {
            SetPrevMenuId(0);
            if (!UmbraMenu.characterCollected && !UmbraMenu.forceFullModMenu) return;
            SetActivatingButton(Utility.FindButtonById(0, MainButtonId));
            Heading("Resources");
            Legacy("Give Money", () => ((Player)UmbraMenu.playerMenu).giveMoney, () => Player.moneyToGive);
            Legacy("Give Lunar Coins", () => ((Player)UmbraMenu.playerMenu).giveCoins, () => Player.coinsToGive);
            Legacy("Give Experience", () => ((Player)UmbraMenu.playerMenu).giveExperience, () => Player.xpToGive);
            NewMultiplier("Gold Multiplier", () => PlayerCheats.GoldMultiplier, v => PlayerCheats.GoldMultiplier = v,
                () => PlayerCheats.GoldFactor, v => PlayerCheats.GoldFactor = (int)v, 2, 10, 1);
            Note("Multiplies collected gold. Give Money uses its exact chosen amount.");
            Heading("Survival");
            LegacyToggle("God Mode", () => ((Player)UmbraMenu.playerMenu).toggleGod, () => Player.GodToggle);
            NewToggle("Unlimited Shields", () => PlayerCheats.UnlimitedShields, v => PlayerCheats.UnlimitedShields = v);
            Note("Refills shields; grants a shield if your character has none.");
            NewToggle("No Fall Damage", () => PlayerCheats.NoFallDamage, v => PlayerCheats.NoFallDamage = v);
            Heading("Movement");
            LegacyToggle("Flight", () => ((Movement)UmbraMenu.movementMenu).toggleFlight, () => Movement.flightToggle);
            NewMultiplier("Speed Multiplier", () => PlayerCheats.SpeedMultiplier, v => PlayerCheats.SpeedMultiplier = v,
                () => PlayerCheats.SpeedFactor, v => PlayerCheats.SpeedFactor = v, 1.5f, 10, .5f);
            NewToggle("Unlimited Jumps", () => PlayerCheats.UnlimitedJumps, v => PlayerCheats.UnlimitedJumps = v);
            Heading("Combat and abilities");
            LegacyToggle("Aimbot", () => ((Player)UmbraMenu.playerMenu).toggleAimbot, () => Player.AimBotToggle);
            LegacyToggle("Infinite Skills", () => ((Player)UmbraMenu.playerMenu).toggleSkillCD, () => Player.SkillToggle);
            NewToggle("One-Hit Kills", () => PlayerCheats.OneHitKills, v => PlayerCheats.OneHitKills = v);
            Note("Your damaging hits kill hostile targets. Allies are excluded.");
            NewMultiplier("Fast Attack Speed", () => PlayerCheats.FastAttackSpeed, v => PlayerCheats.FastAttackSpeed = v,
                () => PlayerCheats.AttackFactor, v => PlayerCheats.AttackFactor = v, 2, 20, 1);
            NewToggle("Max Critical", () => PlayerCheats.MaxCritical, v => PlayerCheats.MaxCritical = v);
            chaos = new Button(new TogglableMulButton(this, rows.Count + 1, "PROC CHAOS : OFF", "PROC CHAOS : ON",
                ToggleChaos, ToggleChaos, () => ChangeLuck(1), () => ChangeLuck(-1))); rows.Add(chaos);
            Note("Boosts your luck-based item procs; higher values give more rerolls.");
            freeze = Toggle("Freeze Enemies", () => WorldCheats.FreezeEnemies, v => WorldCheats.FreezeEnemies = v);
            Note("Stops hostile movement and attacks. Toggle OFF to let them resume.");
            Heading("Stages and teleporter");
            abundance = Multiplier("Stage Abundance", () => WorldCheats.StageAbundance, v => WorldCheats.StageAbundance = v,
                () => WorldCheats.StageMultiplier, v => WorldCheats.StageMultiplier = v);
            Note("Next stage: more chests, shrines and drones.");
            paradise = Multiplier("Chest Paradise", () => WorldCheats.ChestParadise, v => WorldCheats.ChestParadise = v,
                () => WorldCheats.ChestMultiplier, v => WorldCheats.ChestMultiplier = v);
            Note("Next stage: extra chests, up to 50. Placement may limit the total.");
            rows.Add(new Button(new NormalButton(this, rows.Count + 1, "INSTANT TELEPORTER CHARGING",
                () => { if (WorldCheats.RequireHost()) ((Teleporter)UmbraMenu.teleporterMenu).InstaTeleporter(); })));
            Heading("Shopping and chest loot");
            shopping = Toggle("Free Shopping", () => WorldCheats.FreeShopping, v => WorldCheats.FreeShopping = v);
            Note("Money purchases are free. Other payment types keep their cost.");
            loot = Multiplier("Loot Multiplier", () => WorldCheats.LootMultiplier, v => WorldCheats.LootMultiplier = v,
                () => WorldCheats.DropMultiplier, v => WorldCheats.DropMultiplier = v);
            Note("Unopened chests: multiply their drops, up to 20 per chest.");
            rarity = new Button(new MulButton(this, rows.Count + 1, "Rarity Control : Natural", CycleRarity, CycleRarity, PreviousRarity)); rows.Add(rarity);
            Note("Item chests use the chosen rarity. Equipment stays unchanged.");
            Heading("Session");
            rows.Add(new Button(new NormalButton(this, rows.Count + 1, "TURN OFF ALL CHEATS", () => { StopAll(); RefreshLabels(); })));
            status = new Button(new TextButton(this, rows.Count + 1, WorldCheats.Status)); rows.Add(status);
            AddButtons(rows); RefreshLabels();
        }
        private readonly System.Collections.Generic.List<Action> updateLabels = new System.Collections.Generic.List<Action>();
        private void Legacy(string name, Func<Button> source, Func<ulong> value)
        {
            var row = new Button(new MulButton(this, rows.Count + 1, name, () => source()?.Activate(),
                () => { source()?.GetIncreaseAction()?.Invoke(); RefreshLabels(); },
                () => { source()?.GetDecreaseAction()?.Invoke(); RefreshLabels(); }));
            rows.Add(row); updateLabels.Add(() => row.SetText(name + " : " + value()));
        }
        private void LegacyToggle(string name, Func<Button> source, Func<bool> enabled)
        {
            void Change() { source()?.Activate(); RefreshLabels(); }
            var row = new Button(new TogglableButton(this, rows.Count + 1, name + " (OFF)", name + " (ON)", Change, Change));
            rows.Add(row); updateLabels.Add(() => Label(row, name, enabled()));
        }
        private void NewToggle(string name, Func<bool> get, Action<bool> set)
        {
            void Change() { if (!WorldCheats.RequireHost()) { RefreshLabels(); return; } set(!get()); PlayerCheats.SettingsChanged(); RefreshLabels(); }
            var row = new Button(new TogglableButton(this, rows.Count + 1, name + " (OFF)", name + " (ON)", Change, Change));
            rows.Add(row); updateLabels.Add(() => Label(row, name, get()));
        }
        private void NewMultiplier(string name, Func<bool> enabled, Action<bool> setEnabled, Func<float> value, Action<float> setValue,
            float minimum, float maximum, float step)
        {
            void Change() { if (!WorldCheats.RequireHost()) { RefreshLabels(); return; } setEnabled(!enabled()); PlayerCheats.SettingsChanged(); RefreshLabels(); }
            void Adjust(float direction) { if (!WorldCheats.RequireHost()) return; setValue(Mathf.Clamp(value() + step * direction, minimum, maximum)); PlayerCheats.SettingsChanged(); RefreshLabels(); }
            var row = new Button(new TogglableMulButton(this, rows.Count + 1, name + " (OFF)", name + " (ON)", Change, Change, () => Adjust(1), () => Adjust(-1)));
            rows.Add(row); updateLabels.Add(() => Label(row, name, enabled(), " : x" + value().ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)));
        }
        public static void StopAll()
        {
            if (UmbraMenu.characterCollected && (Player.GodToggle || (UmbraMenu.playerMenu as Player)?.toggleGod?.IsEnabled() == true)) Player.DisabledGodMode();
            var player = UmbraMenu.playerMenu as Player;
            player?.toggleGod?.SetEnabled(false); player?.toggleAimbot?.SetEnabled(false); player?.toggleSkillCD?.SetEnabled(false);
            (UmbraMenu.movementMenu as Movement)?.toggleFlight?.SetEnabled(false);
            Player.GodToggle = Player.AimBotToggle = Player.SkillToggle = false;
            Movement.flightToggle = false;
            PlayerCheats.Reset(); WorldCheats.Reset();
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
            foreach (Action update in updateLabels) update();
            Label(abundance, "Stage Abundance", WorldCheats.StageAbundance, " : x" + WorldCheats.StageMultiplier);
            Label(paradise, "Chest Paradise", WorldCheats.ChestParadise, " : x" + WorldCheats.ChestMultiplier);
            Label(shopping, "Free Shopping", WorldCheats.FreeShopping);
            Label(loot, "Loot Multiplier", WorldCheats.LootMultiplier, " : x" + WorldCheats.DropMultiplier);
            rarity.SetText("Rarity Control : " + WorldCheats.Rarity);
            Label(chaos, "Proc Chaos", WorldCheats.ProcChaos, " : +" + WorldCheats.LuckBonus + " luck");
            Label(freeze, "Freeze Enemies", WorldCheats.FreezeEnemies);
            status.SetText(WorldCheats.IsHost ? WorldCheats.Status + " " + PlayerCheats.Status : "New cheats and world controls require the host.");
        }
        public void RefreshControls() { if (Time.unscaledTime < nextLabelRefresh) return; nextLabelRefresh = Time.unscaledTime + .1f; RefreshLabels(); }
        public override void Draw()
        {
            if (!IsEnabled()) return;
            if (Time.unscaledTime >= nextLabelRefresh) { nextLabelRefresh = Time.unscaledTime + .1f; RefreshLabels(); }
            base.Draw();
        }
        public override void Reset() { StopAll(); RefreshLabels(); base.Reset(); }
    }
}
