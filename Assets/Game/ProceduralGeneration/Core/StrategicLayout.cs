using Unity.Mathematics;

namespace RTS.ProceduralGeneration
{
    public enum RegionRole : byte { Start, Economy, GoldFrontier }
    public struct MapRegion { public int X, Y, Radius; public RegionRole Role; }
    public struct RegionConnection { public int From, To; }
    public struct MapGateway { public int X, Y, Region; }
    public struct MapLandmark { public int X, Y, Radius, Region; }

    public sealed class StrategicLayout
    {
        public readonly MapRegion[] Regions = new MapRegion[3];
        public readonly RegionConnection[] Connections = { new RegionConnection { From=0, To=1 }, new RegionConnection { From=0, To=2 } };
        public readonly MapGateway[] Gateways = new MapGateway[2];
        public MapLandmark Landmark;
        public readonly byte[] RegionIds, Routes;

        StrategicLayout(int cells) { RegionIds = new byte[cells]; Routes = new byte[cells]; }

        public static StrategicLayout Create(MapGenerationSettingsData settings, int attempt, byte[] terrain, byte[] reserved, MapValidationReport report)
        {
            int w=settings.Width,h=settings.Height,cx=w/2,cy=h/2;
            var result = new StrategicLayout(w*h);
            var random = new DeterministicRandom(DeterministicRandom.DeriveSeed(settings.Seed,0x5245474Eu,attempt));
            int rotation=random.NextInt(0,4);
            result.Regions[0]=new MapRegion {X=cx,Y=cy,Radius=settings.StartClearRadius,Role=RegionRole.Start};
            int[] dx={1,0,-1,0},dy={0,1,0,-1};
            for(int i=1;i<3;i++)
            {
                int direction=(rotation+i-1)%4;
                int offset=(dx[direction]!=0?w:h)/2-settings.RegionRadius-4;
                int jitter=random.NextInt(-2,3);
                int x=cx+dx[direction]*offset+dy[direction]*jitter;
                int y=cy+dy[direction]*offset+dx[direction]*jitter;
                result.Regions[i]=new MapRegion {X=x,Y=y,Radius=settings.RegionRadius,Role=(RegionRole)i};
                result.Gateways[i-1]=new MapGateway {X=dx[direction]>0?w-1:dx[direction]<0?0:x,Y=dy[direction]>0?h-1:dy[direction]<0?0:y,Region=i};
            }
            result.Landmark=new MapLandmark {X=result.Regions[2].X,Y=result.Regions[2].Y,Radius=2,Region=2};
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                int nearest=0,best=int.MaxValue;
                for(int i=0;i<3;i++)
                {
                    var region=result.Regions[i];int distance=(x-region.X)*(x-region.X)+(y-region.Y)*(y-region.Y);
                    if(distance<best){nearest=i;best=distance;}
                    if(distance<=region.Radius*region.Radius) Paint(x,y,i==0,false);
                }
                result.RegionIds[y*w+x]=(byte)nearest;
            }
            // shortcut: protected L routes are a blockout; validate final topology and wave paths in S5/S6.
            foreach(var connection in result.Connections)
            {
                var from=result.Regions[connection.From];var to=result.Regions[connection.To];
                Route(from.X,from.Y,to.X,to.Y);
                var gateway=result.Gateways[connection.To-1];Route(to.X,to.Y,gateway.X,gateway.Y);
            }
            for(int y=cy-settings.StartClearRadius;y<=cy+settings.StartClearRadius;y++)
                for(int x=cx-settings.StartClearRadius;x<=cx+settings.StartClearRadius;x++)Paint(x,y,true,false);
            for(int y=result.Landmark.Y-2;y<=result.Landmark.Y+2;y++)
                for(int x=result.Landmark.X-2;x<=result.Landmark.X+2;x++)Paint(x,y,true,false);
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
