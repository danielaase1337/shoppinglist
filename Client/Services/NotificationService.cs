namespace BlazorApp.Client.Services
{
    public class NotificationService : INotificationService
    {
        public event Action<ToastMessage>? OnToast;

        public void Success(string message) => Notify(message, ToastType.Success, 3000);
        public void Error(string message) => Notify(message, ToastType.Error, 5000);
        public void Warning(string message) => Notify(message, ToastType.Warning, 4000);
        public void Info(string message) => Notify(message, ToastType.Info, 3000);

        private void Notify(string message, ToastType type, int timeoutMs)
        {
            OnToast?.Invoke(new ToastMessage
            {
                Message = message,
                Type = type,
                TimeoutMs = timeoutMs
            });
        }
    }
}
