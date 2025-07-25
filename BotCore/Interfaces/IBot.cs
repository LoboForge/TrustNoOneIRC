namespace BotCore.Interfaces;

public interface IBot
{
    string Name { get; }

    void OnStart();
    void OnPM() { }
    void OnJoin() { }
    void OnSelfJoin() { }
    void OnChannelMessage() { }
    void OnTick() { }
    void OnStop();

}
