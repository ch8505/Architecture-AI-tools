using AutoMapper;
using ChineseAuction.Api.Dtos;
using ChineseAuction.Api.Models;
using ChineseAuction.Api.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System;
using System.Collections.Generic;

namespace ChineseAuction.Api.Services
{
    public class GiftService : IGiftService
    {
        private readonly IGiftRepository _repo;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;

        public GiftService(IGiftRepository repo, IMapper mapper, IDistributedCache cache, IConfiguration configuration)
        {
            _repo = repo;
            _mapper = mapper;
            _cache = cache;
            _configuration = configuration;
        }

        /// <summary>Invalidate the AllGifts cache entry</summary>
        private async Task InvalidateCacheAsync()
        {
            try
            {
                await _cache.RemoveAsync("AllGifts");
            }
            catch (Exception ex)
            {
                // Log the exception but don't let Redis failure crash the business logic
                System.Diagnostics.Debug.WriteLine($"Cache invalidation failed: {ex.Message}");
            }
        }

        /// <summary>Get all gifts for buyers (with caching)</summary>
        public async Task<IEnumerable<GiftDetailDto>> GetAllForBuyersAsync()
        {
            const string cacheKey = "AllGifts";

            // Try read from cache
            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var cachedResult = JsonSerializer.Deserialize<IEnumerable<GiftDetailDto>>(cached, options);
                if (cachedResult != null)
                {
                    System.Diagnostics.Debug.WriteLine("Fetching all gifts from the cache (cache hit)");
                    return cachedResult;
                }
                    
            }

            // Cache miss — read from repo and populate cache
            var gifts = await _repo.GetAllAsync();
            var result = _mapper.Map<IEnumerable<GiftDetailDto>>(gifts);
            //הדפסה ל console באם ניגש למאגר הנתונים ולא מה cache
            System.Diagnostics.Debug.WriteLine("Fetching all gifts from the database (cache miss)");

            var ttlInMinutes = _configuration.GetValue<int?>("CacheSettings:DefaultTTLInMinutes") ?? 5;
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttlInMinutes)
            };

            var serializeOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var payload = JsonSerializer.Serialize(result, serializeOptions);
            await _cache.SetStringAsync(cacheKey, payload, cacheOptions);

            return result;
        }

        /// <summary>Get all gifts sorted by price</summary>
        public async Task<IEnumerable<GiftDetailDto>> GetAllSortedByPriceAsync(bool ascending)
        {
            var gifts = await _repo.GetAllSortedByPriceAsync(ascending);
            return _mapper.Map<IEnumerable<GiftDetailDto>>(gifts);
        }

        /// <summary>Get all gifts sorted by category</summary>
        public async Task<IEnumerable<GiftDetailDto>> GetAllSortedByCategoryAsync()
        {
            var gifts = await _repo.GetAllSortedByCategoryAsync();
            return _mapper.Map<IEnumerable<GiftDetailDto>>(gifts);
        }

        /// <summary>Get all gifts for admin</summary>
        public async Task<IEnumerable<GiftAdminDto>> GetAllForAdminAsync()
        {
            var gifts = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<GiftAdminDto>>(gifts);
        }

        /// <summary>Get gift by id</summary>
        public async Task<GiftDetailDto?> GetByIdAsync(int id)
        {
            var gift = await _repo.GetByIdAsync(id);
            return _mapper.Map<GiftDetailDto>(gift);
        }

        /// <summary>Search gifts</summary>
        public async Task<IEnumerable<GiftDto>> SearchAsync(string? name, string? donor, int? minPurchasers)
        {
            var gifts = await _repo.SearchGiftsInternalAsync(name, donor, minPurchasers);
            return _mapper.Map<IEnumerable<GiftDto>>(gifts);
        }

        ///// <summary>Create gift (commented)</summary>
        //public async Task<int> CreateAsync(GiftCreateUpdateDto dto)
        //{
        //    var gift = _mapper.Map<Gift>(dto);
        //    return await _repo.CreateAsync(gift);
        //}

        /// <summary>Add gift to donor</summary>
        public async Task<int> AddToDonorAsync(int donorId, GiftCreateUpdateDto dto, string? imagePath)
        {
            if (!await _repo.DonorExistsAsync(donorId))
                throw new KeyNotFoundException("Donor not found");

            var gift = _mapper.Map<Gift>(dto);

            gift.DonorId = donorId;
            gift.ImageUrl = imagePath; // stored in wwwroot

            gift.CategoryId = dto.CategoryId ?? throw new ArgumentException("CategoryId is required", nameof(dto.CategoryId));
            gift.Category = null;

            var giftId = await _repo.CreateAsync(gift);

            // Invalidate cache after successful creation
            await InvalidateCacheAsync();

            return giftId;
        }

        /// <summary>Update gift</summary>
        public async Task<bool> UpdateAsync(int id, GiftCreateUpdateDto dto, string? imagePath)
        {
            var existing = await _repo.GetByIdTrackedAsync(id);
            if (existing == null) return false;

            _mapper.Map(dto, existing);

            if (dto.CategoryId.HasValue)
            {
                existing.CategoryId = dto.CategoryId.Value;
                existing.Category = null;
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                existing.ImageUrl = imagePath;
            }

            await _repo.SaveChangesAsync();

            // Invalidate cache after successful update
            await InvalidateCacheAsync();

            return true;
        }

        /// <summary>Delete gift</summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _repo.DeleteAsync(id);

            // Invalidate cache after successful deletion
            if (result)
            {
                await InvalidateCacheAsync();
            }

            return result;
        }
    }
}
