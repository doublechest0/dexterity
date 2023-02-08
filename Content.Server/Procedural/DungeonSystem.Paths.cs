using System.Linq;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Paths;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    private List<DungeonPath> GetPaths(Dungeon dungeon, PathGen path, Random random)
    {
        switch (path)
        {
            case SimplePathGen simple:
                return GetPaths(simple, dungeon, random);
            default:
                throw new NotImplementedException();
        }
    }

    private List<DungeonPath> GetPaths(SimplePathGen gen, Dungeon dungeon, Random random)
    {
        var paths = new List<DungeonPath>();
        var rooms = dungeon.Rooms.ToList();
        var roomCenters = rooms.Select(GetRoomCenter).ToList();

        var currentRoom = random.Next(rooms.Count);
        var currentRoomCenter = roomCenters[currentRoom];

        while (rooms.Count > 0)
        {
            var closest = FindClosestPointTo(currentRoomCenter, roomCenters);
            roomCenters.Remove(closest);
            var newCorridor = CreateCorridor(currentRoomCenter, closest);
            currentRoomCenter = closest;
            paths.Add(new DungeonPath(string.Empty, string.Empty, newCorridor));
        }

        return paths;
    }

    private HashSet<Vector2i> CreateCorridor(Vector2i currentRoomCenter, Vector2i destination)
    {
        var corridor = new HashSet<Vector2i>();
        var position = currentRoomCenter;
        corridor.Add(currentRoomCenter);

        while (position.Y != destination.Y)
        {
            if (destination.Y > position.Y)
            {
                position += Vector2i.Up;
            }
            else if (destination.Y < position.Y)
            {
                position += Vector2i.Down;
            }
            corridor.Add(position);
        }

        while (position.X != destination.X)
        {
            if (destination.X > position.X)
            {
                position += Vector2i.Right;
            }
            else if(destination.X < position.X)
            {
                position += Vector2i.Left;
            }
            corridor.Add(position);
        }
        return corridor;
    }

    private Vector2i FindClosestPointTo(Vector2i currentRoomCenter, List<Vector2i> roomCenters)
    {
        var closest = Vector2i.Zero;
        var distance = float.MaxValue;
        var roomCenter = (Vector2) currentRoomCenter;

        foreach (var position in roomCenters)
        {
            var currentDistance = (position - roomCenter).Length;

            if (currentDistance < distance)
            {
                distance = currentDistance;
                closest = position;
            }
        }

        return closest;
    }
}
