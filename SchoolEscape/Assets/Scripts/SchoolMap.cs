using System;
using System.Collections.Generic;
using UnityEngine;

namespace SchoolEscape
{
    [Serializable] public sealed class MapData
    {
        public string title;
        public int schemaVersion, spawnFloor, spawnX, spawnY;
        public float tileSize, walkSpeed, runSpeed, playerRadius;
        public float stairEntryHalfWidth,stairEntryHalfDepth,stairExitRadius,stairCooldown;
        public FloorData[] floors;
        public FloorData[] roomMaps = Array.Empty<FloorData>();
        public FloorData[] outdoorMaps = Array.Empty<FloorData>();
        public EscapeGateData escapeGate;
        public IEnumerable<FloorData> AllSpaces()
        {
            foreach(var f in floors??Array.Empty<FloorData>())yield return f;
            foreach(var f in roomMaps??Array.Empty<FloorData>())yield return f;
            foreach(var f in outdoorMaps??Array.Empty<FloorData>())yield return f;
        }
        public FloorData FindSpace(string id)
        {foreach(var f in AllSpaces())if(f.id==id)return f;return null;}
        public static MapData Load() => JsonUtility.FromJson<MapData>(Resources.Load<TextAsset>("SchoolMap").text);
        // Unity's forward-facing camera uses +Z north. The browser renderer
        // independently maps north to -Z because its camera faces local -Z.
        public Vector3 World(float x, float y) => new Vector3((x - 24) * tileSize, 0, (y - 19) * tileSize);
        public Vector2 Grid(Vector3 p) => new Vector2(p.x / tileSize + 24, p.z / tileSize + 19);
    }
    [Serializable] public sealed class FloorData
    {
        public int number, width, height;
        public string id,label,kind,building,returnSpaceId,exitLabel;
        public bool isRoom,isOutdoor,noExit,doubleHeight;
        public int sourceFloor,spawnX,spawnY,exitX,exitY,returnX,returnY;
        public string subtitle;
        public string[] rows;
        public RoomData[] rooms;
        public StairData[] stairs;
        public PortalData[] portals = Array.Empty<PortalData>();
        public FixtureData[] fixtures = Array.Empty<FixtureData>();
        public InteriorZoneData[] interiorZones = Array.Empty<InteriorZoneData>();
        public LandmarkData[] landmarks, barriers;
        public bool Open(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && rows[y][x] == '.';
        public bool DoorCell(int x, int y) => (isRoom&&!noExit&&x==exitX&&y==exitY)||Array.Exists(rooms??Array.Empty<RoomData>(),r=>r.doorX==x&&r.doorY==y)||Array.Exists(portals??Array.Empty<PortalData>(),p=>p.doorX==x&&p.doorY==y);
        public RoomData InteriorAt(float x,float y) => Array.Find(rooms,r=>r.interiorOpen&&x>=r.x0-.5f&&x<=r.x1+.5f&&y>=r.y0-.5f&&y<=r.y1+.5f);
        public bool LowInteriorWall(int x,int y) => isRoom||InteriorAt(x,y)!=null;
        // QA navigation tests the same authored wall/fixture clearances as the
        // controller's physical colliders. Sizes remain in metres in JSON.
        public bool Clear(float x,float y,float tile,float radius)
        {
            float r=radius/tile;
            for(int j=Mathf.FloorToInt(y-r-.5f);j<=Mathf.CeilToInt(y+r+.5f);j++)
                for(int i=Mathf.FloorToInt(x-r-.5f);i<=Mathf.CeilToInt(x+r+.5f);i++)
                    if(!Open(i,j)&&CircleRect(x,y,r,i-.5f,j-.5f,i+.5f,j+.5f))return false;
            foreach(var v in fixtures??Array.Empty<FixtureData>())
                if(v.solid&&v.elevation<1.68f&&CircleRect(x,y,r,v.x-v.width/tile*.5f,v.y-v.depth/tile*.5f,v.x+v.width/tile*.5f,v.y+v.depth/tile*.5f))return false;
            foreach(var portal in portals??Array.Empty<PortalData>())
            {
                Vector2 d=portal.Outward,c=new Vector2(portal.doorX,portal.doorY)+d*.51f;
                float a=(d.y!=0?1.02f:.14f)/tile*.5f,b=(d.y!=0?.14f:1.02f)/tile*.5f;
                if(CircleRect(x,y,r,c.x-a,c.y-b,c.x+a,c.y+b))return false;
            }
            return true;
        }
        static bool CircleRect(float x,float y,float r,float x0,float y0,float x1,float y1)
        {
            float dx=x-Mathf.Clamp(x,x0,x1),dy=y-Mathf.Clamp(y,y0,y1);
            return dx*dx+dy*dy<r*r-.000001f;
        }
    }
    [Serializable] public sealed class PortalData
    {
        public string id,label,facing,targetId;
        public int doorX,doorY,arrivalX,arrivalY;
        public Vector2 Outward=>facing=="north"?Vector2.up:facing=="south"?Vector2.down:facing=="west"?Vector2.left:Vector2.right;
    }
    [Serializable] public sealed class EscapeGateData
    {
        public string spaceId,label;
        public float x,y,range;
    }
    [Serializable] public sealed class RoomData
    {
        public string label, facing, interiorKind, interiorId;
        public bool interiorOpen;
        public int x0, y0, x1, y1, doorX, doorY;
    }
    [Serializable] public sealed class FixtureData
    {
        public string id,room,kind,label,color,shape;
        public float x,y,width,depth,height,elevation;
        public bool solid;
    }
    [Serializable] public sealed class InteriorZoneData
    {
        public string room,label;
        public int x,y;
    }
    [Serializable] public sealed class StairData
    {
        public string id, label, targetId;
        public int x, y, target, arrivalX, arrivalY;
    }
    [Serializable] public sealed class LandmarkData
    {
        public string label;
        public int x, y;
    }
}
