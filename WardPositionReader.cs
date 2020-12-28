using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using SharpDX;

namespace AutoWard
{
    public class WardPosition
    {
        public Vector2 position;
        public float popularity;
        public float score;
    }

    public static class WardPositionReader
    {
        public static List<WardPosition> Read(GameObjectTeam team, int minute = 0)
        {
            var teamString = "Chaos";
            if (team == GameObjectTeam.Order)
            {
                teamString = "Order";
            }

            var output = new List<WardPosition>();
            var content = Resource1.ResourceManager.GetString($"wards_{teamString}_All_{minute}", Resource1.Culture);
            if (content == null) return output;

            output.AddRange(content.Split('\n')
                .Skip(1)
                .Select(line => line.Split(','))
                .Select(columns =>
                    new WardPosition
                    {
                        position = new Vector2
                        {
                            X = float.Parse(columns[0]),
                            Y = float.Parse(columns[1])
                        },
                        popularity = float.Parse(columns[4]),
                        score = float.Parse(columns[5])
                    }));

            return output;
        }
    }
}