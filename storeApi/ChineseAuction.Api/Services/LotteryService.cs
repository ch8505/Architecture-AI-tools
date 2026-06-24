using ChineseAuction.Api.Dtos;
using ChineseAuction.Api.Models;
using ChineseAuction.Api.Repositories;
using System.Globalization;

namespace ChineseAuction.Api.Services
{
    public class LotteryService : ILotteryService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IGiftRepository _giftRepo;
        private readonly ILotteryRepository _lotteryRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<LotteryService> _logger;
        private readonly KafkaProducerService _kafkaProducerService;
        private readonly string _reportsPath;

        public LotteryService(
            IOrderRepository orderRepo,
            IGiftRepository giftRepo,
            ILotteryRepository lotteryRepo,
            IUserRepository userRepo,
            ILogger<LotteryService> logger,
            KafkaProducerService kafkaProducerService,
            IWebHostEnvironment env)
        {
            _orderRepo = orderRepo;
            _giftRepo = giftRepo;
            _lotteryRepo = lotteryRepo;
            _userRepo = userRepo;
            _logger = logger;
            _kafkaProducerService = kafkaProducerService;

            // Reports folder in app root
            _reportsPath = Path.Combine(env.ContentRootPath, "Reports");
            if (!Directory.Exists(_reportsPath)) Directory.CreateDirectory(_reportsPath);
        }

        /// <summary>
        /// ���� ���� ���� ���� ��� - ����� �� ������ �������. �� ��� ������� ����� null.
        /// ���� �� �-Winner ���� � ����� ����� ����� Winners.csv
        /// </summary>
        public async Task<WinnerResultDto?> DrawForGiftAsync(int giftId)
        {
            // 1. ��� �� �� ������� ������� �� ����� ���
            var orders = await _orderRepo.GetByGiftIdAsync(giftId);
            var confirmed = orders.Where(o => o.Status == Status.IsConfirmed).ToList();

            // 2. ��� ���� ������� ��� ����� ���� ����� ���
            var ticketsByUser = new Dictionary<int, int>(); // userId -> ticket count
            string giftName = string.Empty;

            foreach (var o in confirmed)
            {
                foreach (var oi in o.OrderItems.Where(i => i.GiftId == giftId))
                {
                    giftName = oi.Gift?.Name ?? giftName;
                    if (ticketsByUser.ContainsKey(o.UserId)) ticketsByUser[o.UserId] += oi.Quantity;
                    else ticketsByUser[o.UserId] = oi.Quantity;
                }
            }

            var totalTickets = ticketsByUser.Values.Sum();
            if (totalTickets == 0) return null;

            // 3. ����� ���� ��� ���� (���� �������)
            var rng = new Random();
            var pick = rng.Next(1, totalTickets + 1); // [1..totalTickets]
            var cumulative = 0;
            int winnerUserId = 0;
            foreach (var kv in ticketsByUser)
            {
                cumulative += kv.Value;
                if (pick <= cumulative)
                {
                    winnerUserId = kv.Key;
                    break;
                }
            }

            // 4. ��� ���� �����
            var user = await _userRepo.GetByIdAsync(winnerUserId);
            if (user == null)
            {
                _logger.LogWarning("Winner user {Id} not found in DB", winnerUserId);
                return null;
            }

            // 5. ����� ���� ����
            var winnerEntity = new Winner
            {
                GiftId = giftId,
                UserId = winnerUserId
            };

            var savedWinner = await _lotteryRepo.SaveWinnerAsync(winnerEntity);

            // 6. ���� DTO ������ ����� (Winners.csv)
            var result = new WinnerResultDto
            {
                GiftId = giftId,
                GiftName = giftName,
                WinnerUserId = winnerUserId,
                WinnerName = string.IsNullOrWhiteSpace(user.Name) ? user.Email : $"{user.Name} ",
                WinnerEmail = user.Email,
                TotalTickets = totalTickets,
                DrawDate = DateTime.UtcNow
            };

            AppendWinnerReport(result);

            await _kafkaProducerService.SendLotteryEventAsync(result);

            return result;
        }

        /// <summary>
        /// ���� ����� ��� ���� ������� ����� ������� (����� �� ����� ������),
        /// ���� �� �� ������ ����� ��� ������ (Revenue.csv).
        /// </summary>
        public async Task<IEnumerable<WinnerResultDto>> DrawAllAsync()
        {
            var results = new List<WinnerResultDto>();

            var gifts = await _giftRepo.GetAllAsync();

            foreach (var g in gifts)
            {
                var res = await DrawForGiftAsync(g.Id);
                if (res != null) results.Add(res);
            }

            var totalRevenue = await _lotteryRepo.GetTotalRevenueAsync();
            AppendRevenueReport(totalRevenue);

            return results;
        }

        // Append one line to Winners.csv (GiftId,GiftName,WinnerUserId,WinnerName,WinnerEmail,TotalTickets,DrawDate)
        private void AppendWinnerReport(WinnerResultDto result)
        {
            try
            {
                var file = Path.Combine(_reportsPath, "Winners.csv");
                var exists = File.Exists(file);
                using var sw = new StreamWriter(file, append: true);
                if (!exists)
                {
                    sw.WriteLine("GiftId,GiftName,WinnerUserId,WinnerName,WinnerEmail,TotalTickets,DrawDate");
                }

                // escape commas in fields
                static string esc(string? s) => (s ?? string.Empty).Replace(",", " ");
                sw.WriteLine($"{result.GiftId},{esc(result.GiftName)},{result.WinnerUserId},{esc(result.WinnerName)},{esc(result.WinnerEmail)},{result.TotalTickets},{result.DrawDate.ToString("o", CultureInfo.InvariantCulture)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append winner report");
            }
        }

        // Append or create Revenue.csv with timestamp and total revenue
        private void AppendRevenueReport(decimal totalRevenue)
        {
            try
            {
                var file = Path.Combine(_reportsPath, "Revenue.csv");
                var exists = File.Exists(file);
                using var sw = new StreamWriter(file, append: true);
                if (!exists)
                {
                    sw.WriteLine("GeneratedAt,TotalRevenue");
                }

                sw.WriteLine($"{DateTime.UtcNow:o},{totalRevenue}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append revenue report");
            }
        }
    }
}