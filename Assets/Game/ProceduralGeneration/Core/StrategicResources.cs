using System;
using System.Collections.Generic;

namespace RTS.ProceduralGeneration
{
    public sealed partial class ProceduralMapGenerator
    {
        static int EconomyShare(int count) => (count * 3 + 3) / 4;
        static void PlaceStrategicResources(MapGenerationSettingsData settings, StrategicLayout layout, byte[] terrain,
            byte[] reserved, byte[] occupied, int[] distances, List<ResourcePlacement> resources, ref DeterministicRandom random)
        {
            // Three distinct 3x3 gold footprints surround the protected 5x5 building pad.
            int[] dx = { -4, 4, -4, 4 }, dy = { -4, -4, 4, 4 };
            int landmarkGold = Math.Min(3, settings.TotalGoldCount - settings.StartingGoldCount);
            int placed = 0;
            for (int i = 0; i < 4 && placed < landmarkGold; i++)
            {
                int x = layout.Landmark.X + dx[i], y = layout.Landmark.Y + dy[i];
                if (!CanPlace(x,y,1,terrain,reserved,occupied,settings.Width,settings.Height)) continue;
                resources.Add(new ResourcePlacement {Kind=ProceduralResourceKind.Gold,X=x,Y=y,
                    VariantId=CountPlacements(resources,ProceduralResourceKind.Gold)%6,ClusterId=400});
                MarkOccupied(x,y,1,occupied,settings.Width,settings.Height);placed++;
            }
            int trees=settings.TotalTreeCount-settings.StartingTreeCount, sheep=settings.TotalSheepCount-settings.StartingSheepCount;
            PlaceQuota(settings,ProceduralResourceKind.Tree,EconomyShare(trees),0,settings.StartingTreeMaxDistance+1,int.MaxValue,false,
                terrain,reserved,occupied,distances,resources,ref random,layout,1);
            PlaceQuota(settings,ProceduralResourceKind.Tree,trees-EconomyShare(trees),0,settings.StartingTreeMaxDistance+1,int.MaxValue,false,
                terrain,reserved,occupied,distances,resources,ref random,layout,2);
            PlaceQuota(settings,ProceduralResourceKind.Gold,settings.TotalGoldCount-settings.StartingGoldCount-landmarkGold,1,settings.StartingGoldMaxDistance+1,int.MaxValue,false,
                terrain,reserved,occupied,distances,resources,ref random,layout,2);
            PlaceQuota(settings,ProceduralResourceKind.Sheep,EconomyShare(sheep),0,settings.StartingSheepMaxDistance+1,int.MaxValue,false,
                terrain,reserved,occupied,distances,resources,ref random,layout,1);
            PlaceQuota(settings,ProceduralResourceKind.Sheep,sheep-EconomyShare(sheep),0,settings.StartingSheepMaxDistance+1,int.MaxValue,false,
                terrain,reserved,occupied,distances,resources,ref random,layout,2);
        }

        public static MapValidationReport ValidateStrategicMap(MapGenerationSettingsData settings, MapData map)
        {
            if(settings==null||map==null||map.Layout==null||settings.Width!=map.Width||settings.Height!=map.Height)
                throw new ArgumentException("Matching strategic map and settings required.");
            var report=new MapValidationReport();
            ValidateStrategic(settings,map.Layout,map.Terrain,map.Reserved,map.Resources,report);return report;
        }
        static void ValidateStrategic(MapGenerationSettingsData settings, StrategicLayout layout, byte[] terrain,
            byte[] reserved, List<ResourcePlacement> resources, MapValidationReport report)
        {
            int w=settings.Width,h=settings.Height,cx=w/2,cy=h/2;
            var blocked=new byte[w*h];
            var footprints=new byte[w*h];
            foreach(var resource in resources)
            {
                int radius=resource.Kind==ProceduralResourceKind.Gold?1:0;
                for(int y=resource.Y-radius;y<=resource.Y+radius;y++)for(int x=resource.X-radius;x<=resource.X+radius;x++)
                    if(!InBounds(x,y,w,h)||terrain[y*w+x]==0||reserved[y*w+x]!=0||footprints[y*w+x]!=0)
                        report.AddError("Resource footprint overlaps water, reserved space or another resource.");
                MarkOccupied(resource.X,resource.Y,radius,footprints,w,h);
            }
            foreach(var r in resources)if(r.Kind!=ProceduralResourceKind.Sheep)MarkOccupied(r.X,r.Y,r.Kind==ProceduralResourceKind.Gold?1:0,blocked,w,h);
            var clearance=new byte[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                bool free=true;
                for(int oy=-1;oy<=1&&free;oy++)for(int ox=-1;ox<=1;ox++)
                {int nx=x+ox,ny=y+oy;if(!InBounds(nx,ny,w,h))continue;if(terrain[ny*w+nx]==0||blocked[ny*w+nx]!=0){free=false;break;}}
                clearance[y*w+x]=(byte)(free?TerrainCell.Grass:TerrainCell.Water);
            }
            var travel=BuildDistances(terrain,w,h,cx,cy,blocked);
            var wide=BuildDistances(clearance,w,h,cx,cy,null);
            foreach(var gate in layout.Gateways)
                if(wide[gate.Y*w+gate.X]<0)report.AddError("Gateway has no connected 3x3 clearance route.");
                else report.ReachableGateways++;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if((x==0||y==0||x==w-1||y==h-1)&&travel[y*w+x]>=0)
            {
                bool registered=false;
                foreach(var gate in layout.Gateways)
                    if(Math.Abs(x-gate.X)+Math.Abs(y-gate.Y)<=settings.RequiredCorridorHalfWidth)registered=true;
                if(!registered)report.AddError("Unregistered reachable boundary entrance.");
            }
            var landmark=layout.Landmark;
            CheckPad(cx,cy,settings.StartClearRadius,"Start");
            CheckPad(landmark.X,landmark.Y,2,"Landmark");
            if(wide[landmark.Y*w+landmark.X]<0)report.AddError("Landmark is isolated at required clearance.");
            int trees=0,sheep=0,gold=0;
            foreach(var r in resources)
            {
                if(!HasReachableInteractionCell(r,travel,terrain,w,h))report.AddError("Resource has no final path to deposit.");
                int distance=int.MaxValue;
                int interaction=r.Kind==ProceduralResourceKind.Gold?2:r.Kind==ProceduralResourceKind.Tree?1:0;
                for(int oy=-interaction;oy<=interaction;oy++)for(int ox=-interaction;ox<=interaction;ox++)
                    if(InBounds(r.X+ox,r.Y+oy,w,h)&&travel[(r.Y+oy)*w+r.X+ox]>=0)
                        distance=Math.Min(distance,travel[(r.Y+oy)*w+r.X+ox]);
                if(distance!=int.MaxValue)report.MaxResourcePathDistance=Math.Max(report.MaxResourcePathDistance,distance);
                if(r.IsStartingResource)continue;
                int region=layout.RegionIds[r.Y*w+r.X];
                if(r.Kind==ProceduralResourceKind.Tree&&region==1)trees++;
                if(r.Kind==ProceduralResourceKind.Sheep&&region==1)sheep++;
                if(r.Kind==ProceduralResourceKind.Gold)
                {
                    if(region!=2)report.AddError("Outer gold outside GoldFrontier.");
                    if(Math.Abs(r.X-landmark.X)<=5&&Math.Abs(r.Y-landmark.Y)<=5)gold++;
                }
            }
            if(trees!=EconomyShare(settings.TotalTreeCount-settings.StartingTreeCount)||sheep!=EconomyShare(settings.TotalSheepCount-settings.StartingSheepCount))
                report.AddError("Economy region resource budget mismatch.");
            if(gold<Math.Min(3,settings.TotalGoldCount-settings.StartingGoldCount))report.AddError("Landmark gold cluster quota missing.");
            // Exclude the 5x5 proposed building and require a reachable exit ring.
            CheckExit(cx,cy,"Start");CheckExit(landmark.X,landmark.Y,"Landmark");

            void CheckPad(int x,int y,int radius,string name)
            {for(int py=y-radius;py<=y+radius;py++)for(int px=x-radius;px<=x+radius;px++)
                if(!InBounds(px,py,w,h)||terrain[py*w+px]==0||blocked[py*w+px]!=0||reserved[py*w+px]==0){report.AddError(name+" building pad obstructed.");return;}}
            void CheckExit(int x,int y,string name)
            {
                var mask=(byte[])blocked.Clone();MarkOccupied(x,y,2,mask,w,h);
                int sx=cx+3,sy=cy;var exits=BuildDistances(terrain,w,h,sx,sy,mask);
                bool reachable=false;
                for(int oy=-3;oy<=3;oy++)for(int ox=-3;ox<=3;ox++)if(Math.Abs(ox)==3||Math.Abs(oy)==3)
                    if(InBounds(x+ox,y+oy,w,h)&&exits[(y+oy)*w+x+ox]>=0)reachable=true;
                if(!reachable)report.AddError(name+" building exit ring isolated.");
            }
        }
    }
}
