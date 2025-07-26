// Services/ToastService.cs
public class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void Show(string message, ToastLevel level = ToastLevel.Info)
    {
        OnShow?.Invoke(new ToastMessage(message, level));
    }
}

public record ToastMessage(string Message, ToastLevel Level);

public enum ToastLevel { Info, Warning, Error, Success }
