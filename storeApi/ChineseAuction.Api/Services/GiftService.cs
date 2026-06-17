using AutoMapper;
using ChineseAuction.Api.Dtos;
using ChineseAuction.Api.Models;
using ChineseAuction.Api.Repositories;

namespace ChineseAuction.Api.Services
{
    public class GiftService : IGiftService
    {
        private readonly IGiftRepository _repo;
        private readonly IMapper _mapper;

        public GiftService(IGiftRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        /// <summary>שליפת כל המתנות לרוכשים (מידע מפורט )</summary>
        public async Task<IEnumerable<GiftDetailDto>> GetAllForBuyersAsync()
        {
            var gifts = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<GiftDetailDto>>(gifts);
        }

        /// <summary>מיון מתנות לפי מחיר כרטיס</summary>
        public async Task<IEnumerable<GiftDetailDto>> GetAllSortedByPriceAsync(bool ascending)
        {
            var gifts = await _repo.GetAllSortedByPriceAsync(ascending);
            return _mapper.Map<IEnumerable<GiftDetailDto>>(gifts);
        }

        /// <summary>מיון מתנות לפי שם הקטגוריה</summary>
        public async Task<IEnumerable<GiftDetailDto>> GetAllSortedByCategoryAsync()
        {
            var gifts = await _repo.GetAllSortedByCategoryAsync();
            return _mapper.Map<IEnumerable<GiftDetailDto>>(gifts);
        }

        /// <summary>צפייה מנהלית מורחבת כולל נתוני מכירות ותורמים</summary>
        public async Task<IEnumerable<GiftAdminDto>> GetAllForAdminAsync()
        {
            var gifts = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<GiftAdminDto>>(gifts);
        }

        /// <summary>שליפת פרטי מתנה מלאים לפי מזהה</summary>
        public async Task<GiftDetailDto?> GetByIdAsync(int id)
        {
            var gift = await _repo.GetByIdAsync(id);
            return _mapper.Map<GiftDetailDto>(gift);
        }

        /// <summary>חיפוש דינמי במתנות לפי שם, תורם או מינימום רכישות</summary>
        public async Task<IEnumerable<GiftDto>> SearchAsync(string? name, string? donor, int? minPurchasers)
        {
            var gifts = await _repo.SearchGiftsInternalAsync(name, donor, minPurchasers);
            return _mapper.Map<IEnumerable<GiftDto>>(gifts);
        }

        ///// <summary>יצירת מתנה חדשה</summary>
        //public async Task<int> CreateAsync(GiftCreateUpdateDto dto)
        //{
        //    var gift = _mapper.Map<Gift>(dto);
        //    return await _repo.CreateAsync(gift);
        //}

        /// <summary>יצירת מתנה חדשה לתורם קיים</summary>
        public async Task<int> AddToDonorAsync(int donorId, GiftCreateUpdateDto dto, string? imagePath)
        {

            if (!await _repo.DonorExistsAsync(donorId))
                throw new KeyNotFoundException("התורם המבוקש לא נמצא במערכת");

            var gift = _mapper.Map<Gift>(dto);

            gift.DonorId = donorId;
            gift.ImageUrl = imagePath; // השמת הנתיב שנשמר ב-wwwroot

            // פתרון שגיאת Identity: משתמשים ב-ID בלבד ולא באובייקט מלא
            gift.CategoryId = (int)dto.CategoryId;
            gift.Category = null;

            return await _repo.CreateAsync(gift);
        }

        /// <summary>עדכון פרטי מתנה קיימת כולל תמונה</summary>
        public async Task<bool> UpdateAsync(int id, GiftCreateUpdateDto dto, string? imagePath)
        {
            //  שליפת המתנה הקיימת מהמסד
            var existing = await _repo.GetByIdTrackedAsync(id);
            if (existing == null) return false;

            //. מיפוי אוטומטי של שדות פשוטים (Name, TicketPrice, Description)
            // ה-Mapper ידלג על ImageUrl ו-Category כי הגדרנו לו Ignore ב-Profile
            _mapper.Map(dto, existing);

            // עדכון ה-ID של הקטגוריה וניתוק האובייקט (למניעת שגיאת Identity)
            if (dto.CategoryId.HasValue)
            {
                existing.CategoryId = dto.CategoryId.Value;
                existing.Category = null;
            }

            // עדכון התמונה - רק אם הועלתה תמונה חדשה
            if (!string.IsNullOrEmpty(imagePath))
            {
                existing.ImageUrl = imagePath;
            }

            // שמירה
            await _repo.SaveChangesAsync();
            return true;
        }


        /// <summary>מחיקת מתנה מהמערכת</summary>
        public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);
    }
}