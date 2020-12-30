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
        private static int AllyTurrets = 12;
        private static int EnemyTurrets = 12;

        private static List<WardPosition> WardPositions = null;

        private static List<WardPosition> FilteredWardPositions = new List<WardPosition>();
        private const int WardCastDistance = 600;

        private static readonly Items.Item
            ControlWard = new Items.Item(2055, 550f);

        private static readonly Items.Item
            TrinketN = new Items.Item(3340, 600f);

        private static readonly Items.Item
            FrostFang = new Items.Item(3851, 600f);

        private static readonly Items.Item
            ShardOfTrueIce = new Items.Item(3853, 600f);

        private static float _lastWardPlaced = Game.Time;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            _mainMenu = new Menu("Auto Ward", "autoward", true);

            _mainMenu.AddItem(new MenuItem("autoward.on", "Auto Ward").SetValue(true));
            _mainMenu.AddItem(new MenuItem("autoward.popularity", "Min. popularity").SetValue(new Slider(300, 0, 600)));
            _mainMenu.AddItem(new MenuItem("autoward.score", "Min. score").SetValue(new Slider(50, 0, 600)));
            _mainMenu.AddItem(
                new MenuItem("autoward.on_key", "Put ward in best spot").SetValue(new KeyBind('X', KeyBindType.Press)));
            _mainMenu.AddItem(new MenuItem("autoward.num_wards", "Wards to display").SetValue(new Slider(20, 0, 40)));
            _mainMenu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += GameOnOnUpdate;
        }

        private static bool CanCastWards()
        {
            return ShardOfTrueIce.IsReady() || FrostFang.IsReady() || TrinketN.IsReady() || ControlWard.IsReady();
        }

        private static void CastWard(Vector2 position)
        {
            if (TrinketN.IsReady())
            {
                TrinketN.Cast(position);
            }
            else if (ShardOfTrueIce.IsReady())
            {
                ShardOfTrueIce.Cast(position);
            }
            else if (FrostFang.IsReady())
            {
                FrostFang.Cast(position);
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
                                 minion.Name == "SightWard" || minion.Name == "YellowTrinket" ||
                                 minion.Name == "VisionWard")
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
            var numWards = _mainMenu.Item("autoward.num_wards").GetValue<Slider>().Value;

            if (autoWardKey)
            {
                minPopularity = 0;
                minScore = 0;
            }

            var allyTurrets = ObjectManager
                .Get<Obj_AI_Turret>().Count(turret => turret.Team == HeroManager.Player.Team) / 2 * 2;
            var enemyTurrets = ObjectManager
                .Get<Obj_AI_Turret>().Count(turret => turret.Team != HeroManager.Player.Team) / 2 * 2;

            if (allyTurrets != AllyTurrets || enemyTurrets != EnemyTurrets || WardPositions == null)
            {
                EnemyTurrets = enemyTurrets;
                AllyTurrets = allyTurrets;

                var currentWards = WardPositionReader.Read(HeroManager.Player.Team, AllyTurrets, EnemyTurrets);
                if (currentWards.Count != 0)
                {
                    WardPositions = currentWards;
                    Console.WriteLine($"Loading {currentWards.Count} wards.");
                }
                else
                {
                    Console.WriteLine($"Not found wards for {AllyTurrets}:{EnemyTurrets}");
                }
            }

            FilteredWardPositions = WardPositions
                .Where(wardPosition =>
                    !otherWards.Any(otherWard => otherWard.Position.Distance(wardPosition.position.To3D()) < 1600))
                .OrderByDescending(wardPosition => wardPosition.popularity)
                .Take(numWards)
                .ToList();

            if (!CanCastWards()) return;
            if (OwnWards().Count >= 3) return;

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

        public static void DrawText(string msg, Vector2 position, System.Drawing.Color color, int weight = 0)
        {
            var wts = Drawing.WorldToScreen(position.To3D2());
            Drawing.DrawText(wts[0] - msg.Length * 4, wts[1] + weight + 20, color, msg);
        }

        public static bool IsOnScreen(Vector2 pos)
        {
            return pos.X > 0 && pos.X <= Drawing.Width && pos.Y > 0 && pos.Y <= Drawing.Height;
        }

        public static void DrawCross(Vector2 position, System.Drawing.Color color, int size = 25)
        {
            var point1 = Drawing.WorldToScreen(new Vector2
            {
                X = position.X + size,
                Y = position.Y + size
            }.To3D2());
            var point2 = Drawing.WorldToScreen(new Vector2
            {
                X = position.X - size,
                Y = position.Y - size
            }.To3D2());
            var point3 = Drawing.WorldToScreen(new Vector2
            {
                X = position.X + size,
                Y = position.Y - size
            }.To3D2());
            var point4 = Drawing.WorldToScreen(new Vector2
            {
                X = position.X - size,
                Y = position.Y + size
            }.To3D2());

            if (IsOnScreen(point1) && IsOnScreen(point2))
            {
                Drawing.DrawLine(point1, point2, 5, color);
            }

            if (IsOnScreen(point3) && IsOnScreen(point4))
            {
                Drawing.DrawLine(point3, point4, 5, color);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (OwnWards().Count >= 3) return;
            if (!CanCastWards()) return;

            var minPopularity = _mainMenu.Item("autoward.popularity").GetValue<Slider>().Value;
            var minScore = _mainMenu.Item("autoward.score").GetValue<Slider>().Value;
            var autoWardKey = _mainMenu.Item("autoward.on_key").GetValue<KeyBind>().Active;

            foreach (var ward in FilteredWardPositions)
            {
                var textColor = System.Drawing.Color.LightYellow;
                var boxColor = System.Drawing.Color.Yellow;

                if ((ward.score > minScore && ward.popularity > minPopularity) || autoWardKey)
                {
                    textColor = System.Drawing.Color.LightGreen;
                    boxColor = System.Drawing.Color.Green;
                }

                DrawText($"S: {ward.score} P: {ward.popularity}",
                    ward.position, textColor);
                DrawCross(ward.position, boxColor);
            }
        }
    }
}