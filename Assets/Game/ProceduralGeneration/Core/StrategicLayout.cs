using Unity.Mathematics;

namespace RTS.ProceduralGeneration
{
    public enum RegionRole : byte { Start, Economy, GoldFrontier }
    [System.Serializable] public struct MapRegion { public int X, Y, Radius; public RegionRole Role; }
    [System.Serializable] public struct RegionConnection { public int From, To; }
    [System.Serializable] public struct MapGateway { public int X, Y, Region; }
    [System.Serializable] public struct MapLandmark { public int X, Y, Radius, Region; }

    public sealed class StrategicLayout
    {
        public readonly MapRegion[] Regions;
        public readonly RegionConnection[] Connections;
        public readonly MapGateway[] Gateways = new MapGateway[2];
        public MapLandmark Landmark;
        public readonly byte[] RegionIds, Routes;

        StrategicLayout(int cells, int regionCount = 3)
        {
            RegionIds = new byte[cells]; Routes = new byte[cells];
            Regions = new MapRegion[regionCount]; Connections = new RegionConnection[regionCount-1];
            for(int i=0;i<Connections.Length;i++) Connections[i]=new RegionConnection {From=0,To=i+1};
        }
        public static StrategicLayout FromSnapshot(MapRegion[] regions, RegionConnection[] connections, MapGateway[] gateways,
            MapLandmark landmark, byte[] regionIds, byte[] routes)
        {
            if (regions == null || (regions.Length != 3 && regions.Length != 5) || connections == null || connections.Length != regions.Length - 1 || gateways == null || gateways.Length != 2
                || regionIds == null || routes == null || routes.Length != regionIds.Length) throw new System.ArgumentException("Invalid strategic snapshot.");
            if(regions.Length==5 && !ValidContract(regions,connections,gateways,landmark))
                throw new System.ArgumentException("Invalid FourFrontiers topology contract.");
            foreach(byte id in regionIds)if(id>=regions.Length)throw new System.ArgumentException("Invalid region ID.");
            foreach(byte route in routes)if(route>1)throw new System.ArgumentException("Invalid route mask.");
            var result = new StrategicLayout(regionIds.Length, regions.Length) { Landmark = landmark };
            System.Array.Copy(regions, result.Regions, regions.Length); System.Array.Copy(connections, result.Connections, connections.Length); System.Array.Copy(gateways, result.Gateways, 2);
            System.Array.Copy(regionIds, result.RegionIds, regionIds.Length); System.Array.Copy(routes, result.Routes, routes.Length);
            return result;
        }

        public static bool ValidContract(MapRegion[] regions, RegionConnection[] connections, MapGateway[] gateways, MapLandmark landmark)
        {
            if(regions==null||connections==null||gateways==null||gateways.Length!=2
                || (regions.Length!=3&&regions.Length!=5)||connections.Length!=regions.Length-1)return false;
            for(int i=0;i<regions.Length;i++)if(regions[i].Role!=(i==0?RegionRole.Start:i%2==1?RegionRole.Economy:RegionRole.GoldFrontier))return false;
            var seen=new bool[regions.Length];
            foreach(var c in connections)
            {if(c.From!=0||c.To<=0||c.To>=regions.Length||seen[c.To])return false;seen[c.To]=true;}
            foreach(var g in gateways)if(g.Region<=0||g.Region>=regions.Length)return false;
            return gateways[0].Region!=gateways[1].Region
                && regions[gateways[0].Region].Role!=regions[gateways[1].Region].Role
                && landmark.Region>0&&landmark.Region<regions.Length&&regions[landmark.Region].Role==RegionRole.GoldFrontier;
        }

        public static StrategicLayout Create(MapGenerationSettingsData settings, int attempt, byte[] terrain, byte[] reserved, MapValidationReport report)
        {
            int w=settings.Width,h=settings.Height,cx=w/2,cy=h/2;
            bool four = settings.Preset == MapLayoutPreset.FourFrontiers;
            var result = new StrategicLayout(w*h, four ? 5 : 3);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                if(x<2||y<2||x>=w-2||y>=h-2)terrain[y*w+x]=(byte)TerrainCell.Water;
            var random = new DeterministicRandom(DeterministicRandom.DeriveSeed(settings.Seed,0x5245474Eu,attempt));
            int rotation=random.NextInt(0,4);
            result.Regions[0]=new MapRegion {X=cx,Y=cy,Radius=settings.StartClearRadius,Role=RegionRole.Start};
            int[] dx={1,0,-1,0},dy={0,1,0,-1};
            for(int i=1;i<result.Regions.Length;i++)
            {
                int direction=(rotation+i-1)%4;
                int offset=(dx[direction]!=0?w:h)/2-settings.RegionRadius-4;
                int jitter=random.NextInt(-2,3);
                int x=cx+dx[direction]*offset+dy[direction]*jitter;
                int y=cy+dy[direction]*offset+dx[direction]*jitter;
                result.Regions[i]=new MapRegion {X=x,Y=y,Radius=settings.RegionRadius,Role=four ? (i%2==1 ? RegionRole.Economy : RegionRole.GoldFrontier) : (RegionRole)i};
                if(i<=2) result.Gateways[i-1]=new MapGateway {X=dx[direction]>0?w-1:dx[direction]<0?0:x,Y=dy[direction]>0?h-1:dy[direction]<0?0:y,Region=i};
            }
            result.Landmark=new MapLandmark {X=result.Regions[2].X,Y=result.Regions[2].Y,Radius=2,Region=2};
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                int nearest=0,best=int.MaxValue;
                for(int i=0;i<result.Regions.Length;i++)
                {
                    var region=result.Regions[i];int distance=(x-region.X)*(x-region.X)+(y-region.Y)*(y-region.Y);
                    if(distance<best){nearest=i;best=distance;}
                    if(distance<=region.Radius*region.Radius) Paint(x,y,i==0,false);
                }
                result.RegionIds[y*w+x]=(byte)nearest;
            }
            // shortcut: L routes define the blockout; S6 verifies actual collider/navigation mappings.
            foreach(var connection in result.Connections)
            {
                var from=result.Regions[connection.From];var to=result.Regions[connection.To];
                Route(from.X,from.Y,to.X,to.Y);
                foreach(var gateway in result.Gateways)
                    if(gateway.Region==connection.To) Route(to.X,to.Y,gateway.X,gateway.Y);
            }
            for(int y=cy-settings.StartClearRadius;y<=cy+settings.StartClearRadius;y++)
                for(int x=cx-settings.StartClearRadius;x<=cx+settings.StartClearRadius;x++)Paint(x,y,true,false);
            for(int y=result.Landmark.Y-2;y<=result.Landmark.Y+2;y++)
                for(int x=result.Landmark.X-2;x<=result.Landmark.X+2;x++)Paint(x,y,true,false);
            if(four) foreach(var region in result.Regions)
                for(int y=region.Y-2;y<=region.Y+2;y++)
                    for(int x=region.X-2;x<=region.X+2;x++)Paint(x,y,true,false);
            return result;

            void Paint(int x,int y,bool protect,bool route)
            {
                if(x<0||y<0||x>=w||y>=h)return;
                int index=y*w+x;
                if(terrain[index]!=(byte)TerrainCell.Grass)report.Repairs++;
                terrain[index]=(byte)TerrainCell.Grass;
                if(protect)reserved[index]=1;
                if(route)result.Routes[index]=1;
            }
            void Route(int x,int y,int tx,int ty)
            {
                while(true)
                {
                    for(int oy=-settings.RequiredCorridorHalfWidth;oy<=settings.RequiredCorridorHalfWidth;oy++)
                        for(int ox=-settings.RequiredCorridorHalfWidth;ox<=settings.RequiredCorridorHalfWidth;ox++)Paint(x+ox,y+oy,true,true);
                    if(x==tx&&y==ty)break;
                    if(x!=tx)x+=x<tx?1:-1;else y+=y<ty?1:-1;
                }
            }
        }
    }
}
