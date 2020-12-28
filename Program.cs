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
        private static readonly List<WardPosition> WardPositions = WardPositionReader.Read(HeroManager.Player.Team);
        private const int WardCastDistance = 900;

        private static readonly Items.Item
            VisionWard = new Items.Item(2055, 550f);

        private static readonly Items.Item
            TrinketN = new Items.Item(3340, 600f);

        private static readonly Items.Item
            SightStone = new Items.Item(3851, 600f);

        private static float _lastWardPlaced = Game.Time;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            _mainMenu = new Menu("Auto Ward", "autoward", true);

            _mainMenu.AddItem(new MenuItem("autoward.on", "Auto Ward").SetValue(true));
            _mainMenu.AddItem(new MenuItem("autoward.popularity", "Min. popularity").SetValue(new Slider(40, 0, 3000)));
            _mainMenu.AddItem(new MenuItem("autoward.score", "Min. score").SetValue(new Slider(40, 0, 600)));
            _mainMenu.AddItem(
                new MenuItem("autoward.on_key", "Put ward in best spot").SetValue(new KeyBind('X', KeyBindType.Press)));
            _mainMenu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += GameOnOnUpdate;
        }

        private static void CastWard(Vector2 position)
        {
            if (VisionWard.IsReady())
            {
                VisionWard.Cast(position);
            }
            else if (TrinketN.IsReady())
            {
                TrinketN.Cast(position);
            }
            else if (SightStone.IsReady())
            {
                SightStone.Cast(position);
            }

            _lastWardPlaced = Game.Time;
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            if (!_mainMenu.Item("autoward.on").GetValue<bool>() &&
                !_mainMenu.Item("autoward.on_key").GetValue<bool>()) return;
            if (Game.Time - _lastWardPlaced < 5) return;

            var minPopularity = _mainMenu.Item("autoward.popularity").GetValue<int>();
            var minScore = _mainMenu.Item("autoward.score").GetValue<int>();

            foreach (var wardPosition in WardPositions
                .Where(wardPosition =>
                    wardPosition.position.Distance(HeroManager.Player.ServerPosition) < WardCastDistance &&
                    wardPosition.popularity > minPopularity && wardPosition.score > minScore))
            {
                CastWard(wardPosition.position);
                break;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var wardPosition in WardPositions)
            {
                Render.Circle.DrawCircle(wardPosition.position.To3D(), 25, System.Drawing.Color.Green, 1);
            }
        }
    }
}