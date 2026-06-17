public interface IPooledRuntimeObject
{
    void OnSpawnedFromPool(bool isReused);
    void OnReturnedToPool();
}
