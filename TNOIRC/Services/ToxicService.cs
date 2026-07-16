using System.Diagnostics;

public class ToxicService
{
    private Process? _toxicProcess;
    private bool _started;

    public event Action<string>? OnOutput;

    public void Start(string args = "")
    {
        if (_started && _toxicProcess is { HasExited: false })
            return;

        var psi = new ProcessStartInfo
        {
            FileName = "torsocks",
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

        try
        {
            _toxicProcess.Start();
            _toxicProcess.BeginOutputReadLine();
            _toxicProcess.BeginErrorReadLine();
            _started = true;
        }
        catch (Exception ex)
        {
            OnOutput?.Invoke($"[ERR] Failed to start toxic: {ex.Message}");
        }
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
        if (_toxicProcess is { HasExited: false })
            _toxicProcess.Kill();

        _toxicProcess?.Dispose();
        _toxicProcess = null;
        _started = false;
    }
}
