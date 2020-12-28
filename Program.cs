namespace AutoWard
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SharpDX;

    static class Program
    {
        private static Menu _mainMenu;
        private static int CurrentlyLoadedMinute = 0;

        private static List<WardPosition> WardPositions =
            WardPositionReader.Read(HeroManager.Player.Team, CurrentlyLoadedMinute);

        private static List<WardPosition> FilteredWardPositions = new List<WardPosition>();
        private const int WardCastDistance = 600;

        private static readonly Items.Item
            ControlWard = new Items.Item(2055, 550f);

        private static readonly Items.Item
            TrinketN = new Items.Item(3340, 600f);

        private static readonly Items.Item
            FrostFang = new Items.Item(3851, 600f);

        private static float _lastWardPlaced = Game.Time;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            _mainMenu = new Menu("Auto Ward", "autoward", true);

            _mainMenu.AddItem(new MenuItem("autoward.on", "Auto Ward").SetValue(true));
            _mainMenu.AddItem(new MenuItem("autoward.popularity", "Min. popularity").SetValue(new Slider(40, 0, 300)));
            _mainMenu.AddItem(new MenuItem("autoward.score", "Min. score").SetValue(new Slider(40, 0, 600)));
            _mainMenu.AddItem(
                new MenuItem("autoward.on_key", "Put ward in best spot").SetValue(new KeyBind('X', KeyBindType.Press)));
            _mainMenu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += GameOnOnUpdate;
        }

        private static bool CanCastWards()
        {
            return FrostFang.IsReady() || TrinketN.IsReady() || ControlWard.IsReady();
        }

        private static void CastWard(Vector2 position)
        {
            if (FrostFang.IsReady())
            {
                FrostFang.Cast(position);
            }
            else if (TrinketN.IsReady())
            {
                TrinketN.Cast(position);
            }
            else if (ControlWard.IsReady())
            {
                ControlWard.Cast(position);
            }

            _lastWardPlaced = Game.Time;
        }

        private static List<Obj_AI_Minion> OwnWards()
        {
            return OtherWardPositions()
                .Where(wardPosition => wardPosition.Owner.NetworkId == HeroManager.Player.NetworkId)
                .ToList();
        }

        private static List<Obj_AI_Minion> OtherWardPositions()
        {
            return ObjectManager.Get<Obj_AI_Minion>()
                .Where(minion => minion.Name == "JammerDevice" || minion.Name == "BlueTrinket" ||
                                 minion.Name == "SightWard" || minion.Name == "YellowTrinket")
                .Where(minion => minion.Team == HeroManager.Player.Team)
                .ToList();
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            var autoWardKey = _mainMenu.Item("autoward.on_key").GetValue<KeyBind>().Active;
            var autoWard = _mainMenu.Item("autoward.on").GetValue<bool>();

            if (!autoWardKey && !autoWard) return;
            if (Game.Time - _lastWardPlaced < 1) return;

            var otherWards = OtherWardPositions();

            var minPopularity = _mainMenu.Item("autoward.popularity").GetValue<Slider>().Value;
            var minScore = _mainMenu.Item("autoward.score").GetValue<Slider>().Value;

            if (autoWardKey)
            {
                minPopularity = 0;
                minScore = 0;
            }

            FilteredWardPositions = WardPositions
                .Where(wardPosition =>
                    !otherWards.Any(otherWard => otherWard.Position.Distance(wardPosition.position.To3D()) < 1200))
                .ToList();

            if (!CanCastWards()) return;
            if (OwnWards().Count >= 3) return;

            if (Game.Time / 60 > CurrentlyLoadedMinute + 5)
            {
                CurrentlyLoadedMinute += 5;
                var currentMinuteWards = WardPositionReader.Read(HeroManager.Player.Team, CurrentlyLoadedMinute);
                if (currentMinuteWards.Count != 0)
                {
                    WardPositions = currentMinuteWards;
                    Console.WriteLine($"Loading {currentMinuteWards.Count} wards.");
                }
                else
                {
                    Console.WriteLine($"Not found wards for minute {CurrentlyLoadedMinute}");
                }
            }

            foreach (var wardPosition in FilteredWardPositions
                .Where(wardPosition =>
                    wardPosition.position.Distance(HeroManager.Player.ServerPosition) < WardCastDistance)
                .Where(wardPosition =>
                    wardPosition.popularity > minPopularity && wardPosition.score > minScore))
            {
                CastWard(wardPosition.position);
                break;
            }
        }

        public static void DrawText(string msg, Vector3 position, System.Drawing.Color color, int weight = 0)
        {
            var wts = Drawing.WorldToScreen(position);
            Drawing.DrawText(wts[0] - msg.Length * 4, wts[1] + weight + 20, color, msg);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (OwnWards().Count >= 3) return;
            if (!CanCastWards()) return;

            foreach (var wardPosition in FilteredWardPositions)
            {
                Render.Circle.DrawCircle(wardPosition.position.To3D(), 50, System.Drawing.Color.Green, 5);
                DrawText($"S: {wardPosition.score} P: {wardPosition.popularity}",
                    wardPosition.position.To3D(), System.Drawing.Color.LightGreen);
            }
        }
    }
}