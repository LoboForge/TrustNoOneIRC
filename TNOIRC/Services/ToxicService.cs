using System.Diagnostics;

public class ToxicService
{
    private Process? _toxicProcess;

    public event Action<string>? OnOutput;

    public void Start(string args = "")
    {
        var psi = new ProcessStartInfo
        {
            FileName = "torsocks", // or "toxic" directly
            Arguments = $"./toxic/build/toxic {args}",
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _toxicProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _toxicProcess.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                OnOutput?.Invoke(e.Data);
        };
        _toxicProcess.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                OnOutput?.Invoke($"[ERR] {e.Data}");
        };

        _toxicProcess.Start();
        _toxicProcess.BeginOutputReadLine();
        _toxicProcess.BeginErrorReadLine();
    }

    public void SendCommand(string cmd)
    {
        if (_toxicProcess?.StandardInput.BaseStream.CanWrite == true)
        {
            _toxicProcess.StandardInput.WriteLine(cmd);
            _toxicProcess.StandardInput.Flush();
        }
    }

    public void Stop()
    {
        _toxicProcess?.Kill();
        _toxicProcess?.Dispose();
    }
}
