namespace UnityCacheCleaner.Extensions
{
    public static class ControlExtensions
    {
        public static async Task InvokeAsync(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                await Task.Run(() => control.Invoke(action));
            }
            else
            {
                action();
            }
        }
    }
}
