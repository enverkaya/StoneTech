namespace StoneTech.Services
{
    public interface IActivityLogService
    {
        // Sistemde gerçekleştirilen bir işlemi işlem geçmişine kaydeder
        Task LogAsync(
            string module,
            string action,
            int? recordId,
            string description);
    }
}