namespace BlazorApp.Client.Services
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastMessage
    {
        public string Message { get; init; } = string.Empty;
        public ToastType Type { get; init; }
        public int TimeoutMs { get; init; }
        public Guid Id { get; } = Guid.NewGuid();
    }

    public interface INotificationService
    {
        event Action<ToastMessage> OnToast;

        void Success(string message);
        void Error(string message);
        void Warning(string message);
        void Info(string message);
    }
}
