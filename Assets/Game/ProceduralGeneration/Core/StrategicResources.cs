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
            if(settings.Preset == MapLayoutPreset.FourFrontiers)
            {
                PlaceFourFrontiersResources(settings,layout,terrain,reserved,occupied,distances,resources,ref random);
                return;
            }
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

        public static int FourFrontiersBudget(MapGenerationSettingsData settings, int region, ProceduralResourceKind kind)
        {
            if(region<1||region>4)throw new ArgumentOutOfRangeException(nameof(region));
            int total=kind==ProceduralResourceKind.Tree?settings.TotalTreeCount-settings.StartingTreeCount
                :kind==ProceduralResourceKind.Gold?settings.TotalGoldCount-settings.StartingGoldCount
                :settings.TotalSheepCount-settings.StartingSheepCount;
            bool economy=region%2==1, first=region<=2;
            if(kind==ProceduralResourceKind.Gold)
                return economy?0:first?Math.Max(3,(total+1)/2):total-Math.Max(3,(total+1)/2);
            int roleTotal=economy?EconomyShare(total):total-EconomyShare(total);
            return first?(roleTotal+1)/2:roleTotal/2;
        }

        static void PlaceFourFrontiersResources(MapGenerationSettingsData settings, StrategicLayout layout, byte[] terrain,
            byte[] reserved, byte[] occupied, int[] distances, List<ResourcePlacement> resources, ref DeterministicRandom random)
        {
            int[] dx={-4,4,-4,4},dy={-4,-4,4,4};
            int placed=0;
            for(int i=0;i<4&&placed<3;i++)
            {
                int x=layout.Landmark.X+dx[i],y=layout.Landmark.Y+dy[i];
                if(layout.RegionIds[y*settings.Width+x]!=layout.Landmark.Region
                    || !CanPlace(x,y,1,terrain,reserved,occupied,settings.Width,settings.Height))continue;
                resources.Add(new ResourcePlacement {Kind=ProceduralResourceKind.Gold,X=x,Y=y,
                    VariantId=CountPlacements(resources,ProceduralResourceKind.Gold)%6,ClusterId=400});
                MarkOccupied(x,y,1,occupied,settings.Width,settings.Height);placed++;
            }
            for(int id=1;id<5;id++)
            foreach(ProceduralResourceKind kind in Enum.GetValues(typeof(ProceduralResourceKind)))
            {
                int budget=FourFrontiersBudget(settings,id,kind)-(kind==ProceduralResourceKind.Gold&&id==layout.Landmark.Region?placed:0);
                int minimum=kind==ProceduralResourceKind.Tree?settings.StartingTreeMaxDistance+1
                    :kind==ProceduralResourceKind.Gold?settings.StartingGoldMaxDistance+1:settings.StartingSheepMaxDistance+1;
                PlaceQuota(settings,kind,budget,kind==ProceduralResourceKind.Gold?1:0,minimum,int.MaxValue,false,
                    terrain,reserved,occupied,distances,resources,ref random,layout,id);
            }
        }

        public static MapValidationReport ValidateSnapshot(MapGenerationSettingsData settings, MapData map)
        {
            var report=ValidateStrategicMap(settings,map);
            if(!report.IsValid)return report;
            Validate(settings,map.Terrain,map.Reserved,map.Resources,map.Width,map.Height,map.StartX,map.StartY,report);
            foreach(ProceduralResourceKind kind in Enum.GetValues(typeof(ProceduralResourceKind)))
            {
                int starting=0;foreach(var r in map.Resources)if(r.IsStartingResource&&r.Kind==kind)starting++;
                int expected=kind==ProceduralResourceKind.Tree?settings.StartingTreeCount:kind==ProceduralResourceKind.Gold?settings.StartingGoldCount:settings.StartingSheepCount;
                if(starting!=expected)report.AddError("Starting quota mismatch.");
            }
            CountTerrain(map.Terrain,report);return report;
        }

        public static MapValidationReport ValidateStrategicMap(MapGenerationSettingsData settings, MapData map)
        {
            if(settings==null||map==null||map.Layout==null||settings.Width!=map.Width||settings.Height!=map.Height)
                throw new ArgumentException("Matching strategic map and settings required.");
            var report=new MapValidationReport();
            if(!StrategicLayout.ValidContract(map.Layout.Regions,map.Layout.Connections,map.Layout.Gateways,map.Layout.Landmark))
            {report.AddError("Invalid strategic topology contract.");return report;}
            if(map.Layout.Regions.Length!=(settings.Preset==MapLayoutPreset.FourFrontiers?5:3)
                || map.Layout.RegionIds.Length!=map.Width*map.Height || map.Layout.Routes.Length!=map.Width*map.Height)
            {report.AddError("Topology/preset size mismatch.");return report;}
            foreach(byte id in map.Layout.RegionIds)if(id>=map.Layout.Regions.Length)
            {report.AddError("Region ID out of bounds.");return report;}
            foreach(var r in map.Layout.Regions)if(r.X<0||r.Y<0||r.X>=map.Width||r.Y>=map.Height||r.Radius<=0)
            {report.AddError("Region geometry out of bounds.");return report;}
            foreach(var g in map.Layout.Gateways)if(g.X<0||g.Y<0||g.X>=map.Width||g.Y>=map.Height
                || !(g.X==0||g.Y==0||g.X==map.Width-1||g.Y==map.Height-1)
                || map.Layout.RegionIds[g.Y*map.Width+g.X]!=g.Region)
            {report.AddError("Gateway geometry/ownership mismatch.");return report;}
            if(settings.Preset==MapLayoutPreset.FourFrontiers)
            {
                for(int id=1;id<5;id++)
                {
                    var r=map.Layout.Regions[id];
                    if(r.X-r.Radius<2||r.Y-r.Radius<2||r.X+r.Radius>=map.Width-2||r.Y+r.Radius>=map.Height-2)
                        report.AddError("Outer region too close to border.");
                    for(int other=0;other<id;other++)
                    {
                        var b=map.Layout.Regions[other];long dx=r.X-b.X,dy=r.Y-b.Y,min=r.Radius+b.Radius+4L;
                        if(dx*dx+dy*dy<min*min)report.AddError("Region centers too close.");
                    }
                }
            }
            if(!report.IsValid)return report;
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
                if(r.Kind==ProceduralResourceKind.Tree&&layout.Regions[region].Role==RegionRole.Economy)trees++;
                if(r.Kind==ProceduralResourceKind.Sheep&&layout.Regions[region].Role==RegionRole.Economy)sheep++;
                if(r.Kind==ProceduralResourceKind.Gold)
                {
                    if(layout.Regions[region].Role!=RegionRole.GoldFrontier)report.AddError("Outer gold outside GoldFrontier.");
                    if(Math.Abs(r.X-landmark.X)<=5&&Math.Abs(r.Y-landmark.Y)<=5)gold++;
                }
            }
            if(trees!=EconomyShare(settings.TotalTreeCount-settings.StartingTreeCount)||sheep!=EconomyShare(settings.TotalSheepCount-settings.StartingSheepCount))
                report.AddError("Economy region resource budget mismatch.");
            if(gold<Math.Min(3,settings.TotalGoldCount-settings.StartingGoldCount))report.AddError("Landmark gold cluster quota missing.");
            if(settings.Preset==MapLayoutPreset.FourFrontiers)
            {
                for(int id=1;id<layout.Regions.Length;id++)
                {
                    var region=layout.Regions[id];
                    CheckPad(region.X,region.Y,2,"Region "+id);
                    if(wide[region.Y*w+region.X]<0)report.AddError("Region center isolated at required clearance.");
                    foreach(ProceduralResourceKind kind in Enum.GetValues(typeof(ProceduralResourceKind)))
                    {
                        int count=0;
                        foreach(var r in resources)if(!r.IsStartingResource&&r.Kind==kind&&layout.RegionIds[r.Y*w+r.X]==id)count++;
                        if(count!=FourFrontiersBudget(settings,id,kind))report.AddError("Region resource budget mismatch: "+id+" / "+kind);
                    }
                }
            }
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
