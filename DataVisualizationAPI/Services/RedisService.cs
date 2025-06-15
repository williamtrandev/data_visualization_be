using StackExchange.Redis;
using System.Text.Json;

namespace DataVisualizationAPI.Services
{
    public interface IRedisService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<T?> GetAsync<T>(string key);
        Task<bool> DeleteAsync(string key);
    }

    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly string _instanceName;

        public RedisService(IConfiguration configuration)
        {
            _instanceName = configuration["Redis:InstanceName"] ?? "";
            
            var muxer = ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    EndPoints = { { configuration["Redis:Host"], int.Parse(configuration["Redis:Port"]) } },
                    User = "default",
                    Password = configuration["Redis:Password"]
                }
            );
            
            _redis = muxer;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var serializedValue = JsonSerializer.Serialize(value);
                await db.StringSetAsync(_instanceName + key, serializedValue, expiry);
            }
            catch (RedisConnectionException ex)
            {
                // Log the error
                Console.WriteLine($"Redis connection error: {ex.Message}");
                throw;
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var db = _redis.GetDatabase();
                var value = await db.StringGetAsync(_instanceName + key);
                
                if (value.IsNull)
                    return default;

                return JsonSerializer.Deserialize<T>(value);
            }
            catch (RedisConnectionException ex)
            {
                // Log the error
                Console.WriteLine($"Redis connection error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyDeleteAsync(_instanceName + key);
            }
            catch (RedisConnectionException ex)
            {
                // Log the error
                Console.WriteLine($"Redis connection error: {ex.Message}");
                throw;
            }
        }
    }
} 